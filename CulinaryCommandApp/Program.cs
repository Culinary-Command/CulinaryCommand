using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using CulinaryCommand.Data;
using CulinaryCommand.Services;
using CulinaryCommand.Inventory.Services;
using CulinaryCommand.PurchaseOrder.Services;
using CulinaryCommand.Components;
using CulinaryCommand.Inventory;
using CulinaryCommand.Inventory.Services.Interfaces;
using CulinaryCommandApp.AIDashboard.Services.Reporting;
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
builder.Services.AddSingleton<Client>(_ => new Client());
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

//
// =====================
// Application Services
// =====================

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserContextService, UserContextService>();

builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonCognitoIdentityProvider>();
builder.Services.AddScoped<CognitoProvisioningService>();

builder.Services.AddScoped<RecipeService>();
builder.Services.AddScoped<UnitService>();
builder.Services.AddScoped<IngredientService>();
builder.Services.AddScoped<IIngredientService, IngredientService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<LocationState>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IUnitService, UnitService>();
builder.Services.AddScoped<IInventoryTransactionService, InventoryTransactionService>();
builder.Services.AddScoped<IInventoryManagementService, InventoryManagementService>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<ITaskAssignmentService, TaskAssignmentService>();
builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
builder.Services.AddSingleton<EnumService>();
builder.Services.AddScoped<IVendorService, VendorService>();
builder.Services.AddScoped<LogoDevService>();
builder.Services.AddHttpClient();


builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost;

    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

var env = builder.Environment;

if (builder.Environment.IsDevelopment())
{
    var dp = Path.Combine(builder.Environment.ContentRootPath, ".dpkeys");
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(dp))
        .SetApplicationName("CulinaryCommand");
}

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
    // app.UseHttpsRedirection();
}

// Temporarily disable HTTPS redirect for development

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