using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using CulinaryCommand.Data;
using CulinaryCommand.Services;
using CulinaryCommandApp.Inventory.Services;
using CulinaryCommand.PurchaseOrder.Services;
using CulinaryCommand.Components;
using CulinaryCommand.Inventory;
using CulinaryCommandApp.Inventory.Services.Interfaces;
using CulinaryCommandApp.AIDashboard.Services.Reporting;
using CulinaryCommandApp.Recipe.Services;
using CulinaryCommandApp.Recipe.Services.Interfaces;
using Google.GenAI;
using System;
using CulinaryCommand.Services.UserContextSpace;
using Amazon.CognitoIdentityProvider;
using Amazon.Extensions.NETCore.Setup;
using CulinaryCommandApp.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;
using CulinaryCommand.Vendor.Services;
using System.IO;
using Resend;
using CulinaryCommandApp.Inventory.Entities;
using Amazon.Runtime;
using CulinaryCommandApp.SmartTask.Services;
using CulinaryCommandApp.SmartTask.Services.Interfaces;




var builder = WebApplication.CreateBuilder(args);

//
// =====================
// UI
// =====================
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

//
// =====================
// Cognito Authentication (MUST be before Build)
// =====================
// ===== Cognito Auth (OIDC) =====
builder.Services
  .AddAuthentication(options =>
  {
      options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
      options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
  })
  .AddCookie()
  .AddOpenIdConnect(options =>
  {
      options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

      // ---- Read Cognito config (env/appsettings) ----
      var region =
          builder.Configuration["AWS:Region"]
          ?? builder.Configuration["AWS_REGION"]
          ?? builder.Configuration["Authentication:Cognito:Region"]; // optional

      var userPoolId = builder.Configuration["Authentication:Cognito:UserPoolId"];
      var clientId = builder.Configuration["Authentication:Cognito:ClientId"];

      // client secret can come from either config or a raw env var
      var clientSecret =
          Environment.GetEnvironmentVariable("COGNITO_CLIENT_SECRET")
          ?? builder.Configuration["Authentication:Cognito:ClientSecret"];

      // Fail fast if missing (prevents weird half-working deploys)
      if (string.IsNullOrWhiteSpace(region))
          throw new InvalidOperationException("Missing config: AWS:Region (or AWS_REGION).");
      if (string.IsNullOrWhiteSpace(userPoolId))
          throw new InvalidOperationException("Missing config: Authentication:Cognito:UserPoolId");
      if (string.IsNullOrWhiteSpace(clientId))
          throw new InvalidOperationException("Missing config: Authentication:Cognito:ClientId");
      if (string.IsNullOrWhiteSpace(clientSecret))
          throw new InvalidOperationException("Missing config: Authentication:Cognito:ClientSecret (or COGNITO_CLIENT_SECRET).");

      options.Authority = $"https://cognito-idp.{region}.amazonaws.com/{userPoolId}";
      options.MetadataAddress = $"{options.Authority}/.well-known/openid-configuration";

      options.ClientId = clientId;
      options.ClientSecret = clientSecret;

      options.ResponseType = OpenIdConnectResponseType.Code;
      options.SaveTokens = true;

      // Use config if present, else default
      options.CallbackPath =
          builder.Configuration["Authentication:Cognito:CallbackPath"] ?? "/signin-oidc";

      options.SignedOutCallbackPath =
          builder.Configuration["Authentication:Cognito:SignedOutCallbackPath"] ?? "/signout-callback-oidc";

      options.RequireHttpsMetadata = true;

      options.Scope.Clear();
      options.Scope.Add("openid");
      options.Scope.Add("email");
      options.Scope.Add("profile");

      options.TokenValidationParameters.NameClaimType = "cognito:username";
      options.TokenValidationParameters.RoleClaimType = "cognito:groups";

      // Secure cookies only work over HTTPS; use SameAsRequest in dev (HTTP).
      var securePolicy = builder.Environment.IsDevelopment()
          ? CookieSecurePolicy.SameAsRequest
          : CookieSecurePolicy.Always;
      options.CorrelationCookie.SecurePolicy = securePolicy;
      options.NonceCookie.SecurePolicy = securePolicy;

      options.Events.OnRedirectToIdentityProvider = ctx =>
        {
            // Forces correct scheme/host behind nginx
            ctx.ProtocolMessage.RedirectUri = $"{ctx.Request.Scheme}://{ctx.Request.Host}{options.CallbackPath}";
            return Task.CompletedTask;
        };

  });

builder.Services.AddAuthorization();

//
// =====================
// AI Services
// =====================
var googleApiKey = builder.Configuration["Google:ApiKey"]
    ?? Environment.GetEnvironmentVariable("GOOGLE_API_KEY")
    ?? throw new InvalidOperationException("Missing config: Google:ApiKey (or GOOGLE_API_KEY env var).");
builder.Services.AddSingleton<Client>(new Client(apiKey: googleApiKey));
builder.Services.AddScoped<AIReportingService>();

//
// =====================
// Database
// =====================
var conn = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(conn))
{
    throw new InvalidOperationException(
        "Missing connection string 'DefaultConnection'");
}

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseMySql(
        conn,
        new MySqlServerVersion(new Version(8, 0, 36)),
        mySqlOpts => mySqlOpts.EnableRetryOnFailure()
    )
);

// Register a DbContextFactory so services that may be invoked from Blazor
// circuits (where the scoped DbContext can be hit concurrently) can spin up
// their own short-lived DbContexts instead of sharing the circuit-scoped one.
builder.Services.AddDbContextFactory<AppDbContext>(opt =>
    opt.UseMySql(
        conn,
        new MySqlServerVersion(new Version(8, 0, 36)),
        mySqlOpts => mySqlOpts.EnableRetryOnFailure()
    ),
    lifetime: ServiceLifetime.Scoped
);

//
// =====================
// Application Services
// =====================

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserContextService, UserContextService>();

// Register AWS options once (Profile/Region from config if provided)
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonCognitoIdentityProvider>();
builder.Services.AddScoped<CognitoProvisioningService>();

builder.Services.AddScoped<IRecipeService, RecipeService>();
builder.Services.AddScoped<UnitService>();
builder.Services.AddScoped<IngredientService>();
builder.Services.AddScoped<StorageLocationService>();
builder.Services.AddScoped<IIngredientService, IngredientService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<LocationState>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IUnitService, UnitService>();
builder.Services.AddScoped<IInventoryTransactionService, InventoryTransactionService>();
builder.Services.AddScoped<IInventoryManagementService, InventoryManagementService>();

builder.Services.AddOptions();  // Start of Email Setup
builder.Services.AddHttpClient<ResendClient>();
builder.Services.Configure<ResendClientOptions>(o =>
{
    o.ApiToken = builder.Configuration["Email:ResendApiToken"]
        ?? throw new InvalidOperationException("Email:ResendApiToken is not set.");
});
builder.Services.AddTransient<IResend, ResendClient>();
builder.Services.AddScoped<IEmailSender, EmailSender>();    // End of Email Setup
builder.Services.AddScoped<ITaskAssignmentService, TaskAssignmentService>();
builder.Services.AddScoped<ITaskLibraryService, TaskLibraryService>();

// SmartTask (Lambda orchestrator integration)
var smartTaskAwsRegion = Amazon.RegionEndpoint.GetBySystemName(
    builder.Configuration["SmartTask:AwsRegion"] ?? "us-east-2");

var smartTaskEnabled = builder.Configuration.GetValue("SmartTask:Enabled", false);

var smartTaskLambdaFunctionUrlEndpoint = builder.Configuration["SmartTask:LambdaFunctionUrlEndpoint"];

if (smartTaskEnabled)
{
    if (string.IsNullOrWhiteSpace(smartTaskLambdaFunctionUrlEndpoint))
        throw new InvalidOperationException("SmartTask is enabled but SmartTask:LambdaFunctionUrlEndpoint is not configured.");

    // Resolve credentials via a factory so each outbound Lambda request gets
    // a fresh AWSCredentials instance. For local SSO this means the handler
    // re-reads the SSO cache (and silently refreshes when the SDK supports it),
    // avoiding stale-token 403s after `aws sso login`.
    Func<AWSCredentials> smartTaskCredentialsFactory = () =>
    {
        var profileName = builder.Configuration["AWS:Profile"]
            ?? Environment.GetEnvironmentVariable("AWS_PROFILE");

        if (!string.IsNullOrWhiteSpace(profileName))
        {
            var chain = new Amazon.Runtime.CredentialManagement.CredentialProfileStoreChain();
            if (chain.TryGetAWSCredentials(profileName, out var profileCredentials) && profileCredentials is not null)
                return profileCredentials;

            throw new InvalidOperationException(
                $"Could not load AWS credentials for profile '{profileName}'. " +
                $"If this is an SSO profile, run: aws sso login --profile {profileName}.");
        }

        // Fall back to the SDK's default chain (env vars, default profile, ECS, EC2 IMDS).
        var fallback = FallbackCredentialsFactory.GetCredentials();
        if (fallback is null)
            throw new InvalidOperationException(
                "No AWS credentials found. Configure AWS:Profile, AWS_PROFILE, or AWS_ACCESS_KEY_ID/AWS_SECRET_ACCESS_KEY.");

        return fallback;
    };

    builder.Services.AddTransient(_ => new SigV4SigningHandler(
        smartTaskCredentialsFactory,
        smartTaskAwsRegion));

    builder.Services
        .AddHttpClient<ISmartTaskOrchestratorClient, SmartTaskLambdaClient>(httpClient =>
        {
            httpClient.BaseAddress = new Uri(smartTaskLambdaFunctionUrlEndpoint);
        })
        .AddHttpMessageHandler<SigV4SigningHandler>();

    builder.Services.AddScoped<ISmartTaskService, SmartTaskService>();
}
else
{
    // SmartTask disabled; register no-op services so pages can render.
    builder.Services.AddScoped<ISmartTaskService, DisabledSmartTaskService>();
}

builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
builder.Services.AddSingleton<EnumService>();
builder.Services.AddScoped<IVendorService, VendorService>();
builder.Services.AddScoped<LogoDevService>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddSingleton<ITaskNotificationService, TaskNotificationService>();
builder.Services.AddHttpClient();

if (builder.Environment.IsDevelopment())
{
    var dp = Path.Combine(builder.Environment.ContentRootPath, ".dpkeys");
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(dp))
        .SetApplicationName("CulinaryCommand");
}
else
{
    builder.Services.AddDataProtection()
        .PersistKeysToAWSSystemsManager("/culinarycommand/prod/DataProtection")
        .SetApplicationName("CulinaryCommand");
}

builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost;

    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

//
// =====================
// Build App
// =====================
var app = builder.Build();

app.UseForwardedHeaders();

// Determine whether the app should only run migrations and exit
var migrateOnly = (Environment.GetEnvironmentVariable("MIGRATE_ONLY")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
                  || (args != null && args.Any(a => a.Equals("--migrate-only", StringComparison.OrdinalIgnoreCase)));

//
// =====================
// Apply pending EF Core migrations at startup
// =====================
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Startup] Migration warning: {ex.Message}");
    }
}

//
// =====================
// Middleware
// =====================
if (migrateOnly)
{
    Console.WriteLine("[Startup] MIGRATE_ONLY set; exiting after applying migrations.");
    return;
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();
  
//
// =====================
// Routes
// =====================
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGet("/health", () => "OK");

var cognitoDomain = builder.Configuration["Authentication:Cognito:Domain"];
var clientId = builder.Configuration["Authentication:Cognito:ClientId"];
var callbackPath = builder.Configuration["Authentication:Cognito:CallbackPath"] ?? "/signin-oidc";
var logoutCallbackPath = builder.Configuration["Authentication:Cognito:SignedOutCallbackPath"] ?? "/signout-callback-oidc";

app.MapGet("/login", async (HttpContext ctx) =>
{
    await ctx.ChallengeAsync(
        OpenIdConnectDefaults.AuthenticationScheme,
        new AuthenticationProperties { RedirectUri = "/post-login" }
    );
});

app.MapGet("/logout", async (HttpContext ctx, IConfiguration config) =>
{
    // Clear local cookie
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    var domain = config["Authentication:Cognito:Domain"]!.TrimEnd('/');
    var clientId = config["Authentication:Cognito:ClientId"]!;

    var postLogout = $"{ctx.Request.Scheme}://{ctx.Request.Host}/"; // must match allowed sign-out URL

    var url = $"{domain}/logout" +
              $"?client_id={Uri.EscapeDataString(clientId)}" +
              $"&logout_uri={Uri.EscapeDataString(postLogout)}";

    return Results.Redirect(url);
});



app.Run();
