using System.Net.Http.Headers;
using System.Text;
using Amazon;
using Amazon.Lambda;
using Amazon.Runtime;
using Amazon.Runtime.Internal;
using Amazon.Runtime.Internal.Auth;
using Amazon.Runtime.Internal.Util;

namespace CulinaryCommandApp.SmartTask.Services
{
    public sealed class SigV4SigningHandler : DelegatingHandler
    {
        private const string LambdaServiceName = "lambda";
        private const string SecurityTokenHeader = "x-amz-security-token";

        private readonly Func<AWSCredentials> _credentialsFactory;
        private readonly RegionEndpoint _awsRegion;

        public SigV4SigningHandler(Func<AWSCredentials> credentialsFactory, RegionEndpoint awsRegion)
        {
            _credentialsFactory = credentialsFactory;
            _awsRegion = awsRegion;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage outboundRequest,
            CancellationToken cancellationToken)
        {
            // Resolve fresh immutable credentials per request so SSO refreshes
            // (and any rotated session tokens) are picked up automatically.
            var awsCredentials = _credentialsFactory();
            var immutableCredentials = await awsCredentials.GetCredentialsAsync();

            // Buffer the body so the payload hash matches what is actually sent.
            var bodyPayload = string.Empty;
            if (outboundRequest.Content != null)
            {
                bodyPayload = await outboundRequest.Content.ReadAsStringAsync(cancellationToken);

                // Replace with a buffered StringContent so signing and transport see identical bytes.
                var bufferedContent = new StringContent(
                    bodyPayload,
                    Encoding.UTF8,
                    outboundRequest.Content.Headers.ContentType?.MediaType ?? "application/json");

                outboundRequest.Content = bufferedContent;
            }

            var requestUri = outboundRequest.RequestUri
                ?? throw new InvalidOperationException("RequestUri is required for SigV4 signing.");

            var awsRequestForSigning = new DefaultRequest(new EmptyRequest(), LambdaServiceName)
            {
                HttpMethod = outboundRequest.Method.Method,
                Endpoint = new Uri(requestUri.GetLeftPart(UriPartial.Authority)),
                ResourcePath = requestUri.AbsolutePath,
                Content = Encoding.UTF8.GetBytes(bodyPayload)
            };

            // Include query string params in canonical request.
            if (!string.IsNullOrWhiteSpace(requestUri.Query))
            {
                var query = requestUri.Query.TrimStart('?');
                foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    var kvp = pair.Split('=', 2);
                    var name = Uri.UnescapeDataString(kvp[0]);
                    var value = kvp.Length == 2 ? Uri.UnescapeDataString(kvp[1]) : string.Empty;
                    awsRequestForSigning.Parameters.Add(name, value);
                }
            }

            // Required canonical headers.
            awsRequestForSigning.Headers["host"] = requestUri.Host;

            if (outboundRequest.Content?.Headers.ContentType is { } contentType)
                awsRequestForSigning.Headers["content-type"] = contentType.ToString();

            // Critical for temporary credentials (SSO / STS / role chaining):
            // x-amz-security-token MUST be in the canonical signed headers, otherwise
            // AWS responds with 403 "The security token included in the request is invalid."
            if (!string.IsNullOrEmpty(immutableCredentials.Token))
            {
                awsRequestForSigning.Headers[SecurityTokenHeader] = immutableCredentials.Token;
            }

            var awsV4Signer = new AWS4Signer();
            var lambdaConfig = new AmazonLambdaConfig { RegionEndpoint = _awsRegion };

            // Use SignRequest (access key + secret key) so the x-amz-security-token
            // header we already placed on the request is part of the canonical
            // signed-headers list. Then apply the Authorization header from the
            // result ourselves (SignRequest does not set it).
            var signingResult = awsV4Signer.SignRequest(
                awsRequestForSigning,
                lambdaConfig,
                new RequestMetrics(),
                immutableCredentials.AccessKey,
                immutableCredentials.SecretKey);

            awsRequestForSigning.Headers["Authorization"] = signingResult.ForAuthorizationHeader;

            // Apply signed headers to the outbound HttpRequestMessage.
            foreach (var signedHeader in awsRequestForSigning.Headers)
            {
                if (string.Equals(signedHeader.Key, "host", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (signedHeader.Key.StartsWith("content-", StringComparison.OrdinalIgnoreCase))
                {
                    outboundRequest.Content ??= new StringContent(string.Empty);
                    outboundRequest.Content.Headers.Remove(signedHeader.Key);
                    outboundRequest.Content.Headers.TryAddWithoutValidation(signedHeader.Key, signedHeader.Value);
                    continue;
                }

                outboundRequest.Headers.Remove(signedHeader.Key);
                outboundRequest.Headers.TryAddWithoutValidation(signedHeader.Key, signedHeader.Value);
            }

            outboundRequest.Content ??= new StringContent(string.Empty);
            outboundRequest.Content.Headers.ContentType ??= new MediaTypeHeaderValue("application/json");

            return await base.SendAsync(outboundRequest, cancellationToken);
        }

        private sealed class EmptyRequest : AmazonWebServiceRequest { }
    }
}
