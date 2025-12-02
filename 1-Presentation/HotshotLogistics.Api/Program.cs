// <copyright file="Program.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using HotshotLogistics.Application;
using HotshotLogistics.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using System.Text.Json;
using Azure.Identity;
using HotshotLogistics.Api;
using Microsoft.Graph;
using HotshotLogistics.Application.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using HotshotLogistics.Domain.DTOs;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// AZURE APP CONFIGURATION + KEY VAULT INTEGRATION
// ============================================================
// Access using colon separator (normalized from double underscore in env vars)
var appConfigEndpoint = builder.Configuration["AppConfiguration:Endpoint"];
var keyVaultUri = builder.Configuration["KeyVault:VaultUri"];
var managedIdentityClientId = builder.Configuration["Azure:ManagedIdentityClientId"];
var isAppConfigConnected = false;

// Create credential for Azure services
DefaultAzureCredential credential;
if (!string.IsNullOrEmpty(managedIdentityClientId))
{
    // Use specific managed identity in production (Container Apps)
    credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
    {
        ManagedIdentityClientId = managedIdentityClientId
    });
}
else
{
    // Use default credential chain for local development
    credential = new DefaultAzureCredential();
}

// Connect to Azure App Configuration if endpoint is configured
if (!string.IsNullOrEmpty(appConfigEndpoint) && Uri.TryCreate(appConfigEndpoint, UriKind.Absolute, out _))
{
    try
    {
        builder.Configuration.AddAzureAppConfiguration(options =>
        {
            options.Connect(new Uri(appConfigEndpoint), credential)
                // Load all configuration values
                .Select(KeyFilter.Any)
                // Load environment-specific values (e.g., "Production:")
                .Select(KeyFilter.Any, builder.Environment.EnvironmentName)
                // Enable Key Vault references
                .ConfigureKeyVault(kv =>
                {
                    kv.SetCredential(credential);
                })
                // Enable dynamic configuration refresh
                .ConfigureRefresh(refresh =>
                {
                    refresh.Register("Sentinel", refreshAll: true)
                        .SetRefreshInterval(TimeSpan.FromMinutes(5));
                });
        });

        // Register the middleware services required by app.UseAzureAppConfiguration()
        builder.Services.AddAzureAppConfiguration();
        isAppConfigConnected = true;
        
        Console.WriteLine($"✅ Connected to Azure App Configuration: {appConfigEndpoint}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Failed to connect to Azure App Configuration: {ex.Message}");
        // Continue without App Config (will use env vars)
    }
}
else if (!builder.Environment.IsDevelopment())
{
    Console.WriteLine("⚠️ Azure App Configuration not configured. Using environment variables and appsettings.json");
}

// Configure settings
builder.Services.Configure<GoogleMapsSettings>(builder.Configuration.GetSection("Mapping:GoogleMaps"));
builder.Services.Configure<AzureMapsSettings>(builder.Configuration.GetSection("Mapping:AzureMaps"));
builder.Services.Configure<SendGridSettings>(builder.Configuration.GetSection("Communication:SendGrid"));

// Add services to the container.
builder.Services.AddHotshotRepositories();
builder.Services.AddApplicationServices();
builder.Services.AddMappingServices();
builder.Services.AddCommunicationServices();

// ============================================================
// AUTHENTICATION CONFIGURATION
// ============================================================
var azureAdB2cSection = builder.Configuration.GetSection("AzureAdB2C");
var azureAdB2cInstance = azureAdB2cSection["Instance"];
var azureAdB2cDomain = azureAdB2cSection["Domain"];
var azureAdB2cClientId = azureAdB2cSection["ClientId"];
// TenantId is often optional if Domain is provided, but good to check
var azureAdB2cTenantId = azureAdB2cSection["TenantId"];

// Fix: Ensure Instance is a valid absolute URI (it might be just the instance name like "Palfery")
if (!string.IsNullOrEmpty(azureAdB2cInstance) && !Uri.TryCreate(azureAdB2cInstance, UriKind.Absolute, out _))
{
    // Assume it's the instance name and construct the b2clogin.com URL
    var fixedInstance = $"https://{azureAdB2cInstance}.b2clogin.com";
    Console.WriteLine($"⚠️ Correcting AzureAdB2C:Instance from '{azureAdB2cInstance}' to '{fixedInstance}'");
    azureAdB2cInstance = fixedInstance;
    // Update the configuration value so MicrosoftIdentityWebApi uses the correct one
    builder.Configuration["AzureAdB2C:Instance"] = azureAdB2cInstance;
}

var isAzureAdB2cConfigured = !string.IsNullOrEmpty(azureAdB2cInstance) 
    && Uri.TryCreate(azureAdB2cInstance, UriKind.Absolute, out _)
    && !string.IsNullOrEmpty(azureAdB2cDomain)
    && !string.IsNullOrEmpty(azureAdB2cClientId);

if (builder.Environment.IsDevelopment())
{
    // Use test authentication handler ONLY for local development
    builder.Services.AddAuthentication("Test")
        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
}
else if (isAzureAdB2cConfigured)
{
    Console.WriteLine("Configuring Azure AD B2C with:");
    Console.WriteLine($"  Instance: {azureAdB2cInstance}");
    Console.WriteLine($"  Domain: {azureAdB2cDomain}");
    Console.WriteLine($"  ClientId: {azureAdB2cClientId}");
    Console.WriteLine($"  TenantId: {azureAdB2cTenantId}");
    
    // Production with proper Azure AD B2C configuration
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(azureAdB2cSection);
}
else
{
    // FAIL SAFE: Warn but do not crash. Allow app to start for health checks.
    Console.WriteLine("⚠️ WARNING: Azure AD B2C is not configured for production. Authentication will not work.");
    Console.WriteLine($"  - AzureAdB2C:Instance (Current: '{azureAdB2cInstance}')");
    Console.WriteLine($"  - AzureAdB2C:ClientId (Current: '{azureAdB2cClientId}')");
    
    // Register a dummy authentication scheme to prevent startup errors if services expect auth
    builder.Services.AddAuthentication("Broken")
        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Broken", options => { });
}

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Ignore null values to reduce payload size
        options.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;

        // Handle circular references gracefully
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hotshot Logistics API",
        Version = "v1",
        Description = "API for Hotshot Logistics platform - manage jobs, drivers, customers, billing, and real-time tracking",
        Contact = new OpenApiContact
        {
            Name = "Hotshot Logistics",
            Email = "support@hotshotlogistics.com"
        }
    });

    // Add JWT Bearer authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Include XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Add HTTP client factory for external API calls
builder.Services.AddHttpClient();

// Add distributed cache (using in-memory for development)
builder.Services.AddDistributedMemoryCache();

// Register GraphServiceClient
builder.Services.AddScoped(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var clientId = config["Azure:ManagedIdentityClientId"];
    
    var options = new DefaultAzureCredentialOptions
    {
        ExcludeSharedTokenCacheCredential = true,
        ExcludeAzureCliCredential = true,
        ExcludeEnvironmentCredential = true,
        ExcludeManagedIdentityCredential = false,
        ExcludeVisualStudioCredential = true,
        ExcludeInteractiveBrowserCredential = true
    };
    
    if (!string.IsNullOrEmpty(clientId))
    {
        options.ManagedIdentityClientId = clientId;
    }
    
    var credential = new DefaultAzureCredential(options);
    return new GraphServiceClient(credential);
});

// Add authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Manager", policy => policy.RequireRole("Manager"));
    options.AddPolicy("Driver", policy => policy.RequireRole("Driver"));
    options.AddPolicy("Customer", policy => policy.RequireRole("Customer"));
    options.AddPolicy("ManagerOrAdmin", policy => policy.RequireRole("Admin", "Manager"));
    options.AddPolicy("ManagerOrDriver", policy => policy.RequireRole("Manager", "Driver"));
    options.AddPolicy("OwnResource", policy => policy.RequireAuthenticatedUser()); // Placeholder for resource-based auth
    options.AddPolicy("CustomerResource", policy => policy.RequireAuthenticatedUser()); // Placeholder for resource-based auth
});

// Configure SignalR
builder.Services.AddSignalR();

var app = builder.Build();

// Enable Azure App Configuration refresh middleware (only if connected and services registered)
if (isAppConfigConnected)
{
    app.UseAzureAppConfiguration();
}

// Configure the HTTP request pipeline.
// Enable Swagger JSON endpoint in all environments
app.UseSwagger();

// Only enable Swagger UI in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hotshot Logistics API v1");
        c.RoutePrefix = "swagger";
    });
}

// Configure CORS policy - must be after UseHttpsRedirection but before UseAuthentication
app.UseHttpsRedirection();

// Handle CORS origins from either array or string format (for environment variables)
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (allowedOrigins == null || allowedOrigins.Length == 0)
{
    // Try to get as a single string (common with environment variables)
    var originsString = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string>();
    if (!string.IsNullOrWhiteSpace(originsString))
    {
        // Split by comma and trim whitespace
        allowedOrigins = originsString
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(o => o.Trim())
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .ToArray();
    }
    else
    {
        allowedOrigins = Array.Empty<string>();
    }
}

app.UseCors(policy =>
{
    policy.WithOrigins(allowedOrigins)
          .AllowAnyMethod()
          .AllowAnyHeader()
          .AllowCredentials();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
