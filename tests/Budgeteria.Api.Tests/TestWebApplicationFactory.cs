using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Budgeteria.Api.Data;
using Budgeteria.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Budgeteria.Api.Tests;

/// <summary>No-op email service so tests never hit real SMTP.</summary>
public class NoOpEmailService : IEmailService
{
    public Task SendInviteAsync(string toEmail, string toName, string inviterName, string planName, string token)
        => Task.CompletedTask;
}

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string TestSigningKey = "test-secret-key-that-is-long-enough-for-hmac-sha256!!";
    private const string TestIssuer = "https://test.auth0.com/";
    private const string TestAudience = "https://api.budgeteria.app";
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Auth0:Domain", "test.auth0.com");
        builder.UseSetting("Auth0:Audience", TestAudience);

        builder.ConfigureServices(services =>
        {
            // Replace PostgreSQL with InMemory
            var dbDescriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<BudgeteriaDbContext>))
                .ToList();
            foreach (var d in dbDescriptors) services.Remove(d);

            // Replace real EmailService with no-op so tests don't hit SMTP
            var emailDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IEmailService));
            if (emailDescriptor is not null) services.Remove(emailDescriptor);
            services.AddScoped<IEmailService, NoOpEmailService>();

            services.AddDbContext<BudgeteriaDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            // Completely override JWT Bearer to avoid OIDC discovery calls
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSigningKey));

                options.Authority = null;
                options.RequireHttpsMetadata = false;
                options.MetadataAddress = string.Empty;
                options.ConfigurationManager = null;
                options.Configuration = new OpenIdConnectConfiguration { Issuer = TestIssuer };
                options.Configuration.SigningKeys.Add(signingKey);
                options.BackchannelHttpHandler = new HttpClientHandler();
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = TestIssuer,
                    ValidAudience = TestAudience,
                    IssuerSigningKey = signingKey,
                    NameClaimType = "name"
                };
            });
        });
    }

    public HttpClient CreateAuthenticatedClient(string auth0Sub, string email = "test@test.com", string name = "Test User")
    {
        var client = CreateClient();
        var token = GenerateTestToken(auth0Sub, email, name);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static string GenerateTestToken(string sub, string email, string name)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, sub),
            new Claim(ClaimTypes.Email, email),
            new Claim("name", name),
            new Claim("email", email)
        };
        var token = new JwtSecurityToken(TestIssuer, TestAudience, claims,
            expires: DateTime.UtcNow.AddHours(1), signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public BudgeteriaDbContext GetDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<BudgeteriaDbContext>();
    }
}
