namespace CulinaryCommandApp.AIDashboard.Services.Reporting
{
    using Google.GenAI;
    using Google.GenAI.Types;
    using System;
    using System.Threading.Tasks;
    using System.Linq;
    using System.Text;
    using System.Globalization;
    using System.Text.Json.Serialization;
    using System.Text.Json;
    using CulinaryCommandApp.AIDashboard.Services.DTOs;


    public class AIReportingService
    {
        private readonly Client _client;

        public AIReportingService(Client client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<string> AnalyzeCsvAsync(string? csvPath = null)
        {
            const string PayloadSchema = @" You are an expert restaurant data analyst.
                                            Analyze the CSV I give you and return ONLY a JSON object
                                            that exactly follows this SCHEMA:
                                            {
                                                ""title"": string,
                                                ""summary"": string,
                                                ""metrics"": [ { ""label"": string, ""value"": string, ""unit"": string|null } ],
                                                ""sections"": [ { ""heading"": string, ""body"": string } ],
                                                ""anomalies"": [ { ""row"": string, ""reason"": string } ],
                                                ""recommendations"": [ string ],
                                                ""generatedAt"": ""ISO-8601 datetime string"",
                                                ""confidence"": ""number"" (0.0 to 1.0)
                                            }
                                            Constraints:
                                            - Return only JSON (no surrounding text, no markdown fences).
                                            - For long text blocks, try to be concise in the ""body"" fields.
                                            - Use ISO-8601 for generatedAt.
                                            - Provide a numeric confidence between 0 and 1.

                                            Analysis requirements:
                                            - Use business/user friendly tone.
                                            - If there are anomalies, prompt the user to investigate.
                                            - Include top seller focus analysis.
                                            - Provide quick action items if appropriate.
                                            Now analyze the CSV content below and return the JSON.
                                            ";


            if (string.IsNullOrWhiteSpace(csvPath))
            {
                Console.WriteLine("CSV path not provided.");
                return "CSV path not provided.";
            }

            if (!System.IO.File.Exists(csvPath))
            {
                Console.WriteLine($"CSV not found at: {csvPath}");
                return $"CSV not found at: {csvPath}";
            }

            var lines = System.IO.File.ReadAllLines(csvPath).ToList();

            if (lines.Count <= 1)
            {
                Console.WriteLine("CSV empty or only header.");
                return "CSV empty or only header.";
            }

            var header = lines[0];
            var rows = lines.Skip(1).ToList();

            /**** Build payload to send to Gemini model****/
            var geminiPayload = new StringBuilder();

            geminiPayload.AppendLine(PayloadSchema);
            geminiPayload.AppendLine(header);
            foreach (var r in rows) geminiPayload.AppendLine(r);

            Console.WriteLine(geminiPayload);

            /***** Make API call to Gemini with payload ******/
            var response = await _client.Models.GenerateContentAsync(
                model: "gemini-3-flash-preview",
                contents: geminiPayload.ToString()
            );

            Console.WriteLine(response.Candidates[0].Content.Parts[0].Text);

            var AIAnalysisResponse = response?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            /*** Deserialize ***/

            if (!string.IsNullOrWhiteSpace(AIAnalysisResponse))
            {
                try
                {
                    var options = new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                    };

                    var result = System.Text.Json.JsonSerializer.Deserialize<AIAnalysisResultDTO>(AIAnalysisResponse, options);

                    if (result != null)
                    {
                        return System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    }
                }
                catch (JsonException)
                {
                    // just catch the exception and move on
                }
            }

            return AIAnalysisResponse ?? "(no analysis returned)";
        }

        /// Counts SmartTask runs created on the given UTC day. Used by the AI dashboard
        /// observability tile so operators can see how many planning sessions ran today.
        public async Task<int> CountRunsForDateAsync(DateOnly day, CulinaryCommand.Data.AppDbContext dbContext)
        {
            var dayStartUtc = new DateTime(day, TimeOnly.MinValue, DateTimeKind.Utc);
            var dayEndUtc = dayStartUtc.AddDays(1);
            return await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(
                dbContext.SmartTaskRuns,
                run => run.CreatedAt >= dayStartUtc && run.CreatedAt < dayEndUtc);
        }
    }
}