using System.Collections.Generic;
using ExchangeService.Api;
using ExchangeService.Api.Configuration;
using ExchangeService.Api.Services;
using ExchangeService.Tests.TestUtilities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExchangeService.Tests.IntegrationTests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly TestHttpMessageHandler _handler;
    private readonly IDictionary<string, string?> _inMemoryConfig;

    public TestWebApplicationFactory(TestHttpMessageHandler handler, IDictionary<string, string?>? configOverrides = null)
    {
        _handler = handler;
        _inMemoryConfig = configOverrides ?? new Dictionary<string, string?>();
        if (!_inMemoryConfig.ContainsKey("ExchangeRateApi:BaseUrl"))
        {
            _inMemoryConfig["ExchangeRateApi:BaseUrl"] = "https://fake/"; // ensures HttpClient BaseAddress is set
        }
    }

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((ctx, cfg) =>
        {
            cfg.AddInMemoryCollection(_inMemoryConfig);
        });
        builder.ConfigureServices(services =>
        {
            services.AddHttpClient<ExchangeRateApiClient>("ExchangeRateApi")
                .ConfigureHttpMessageHandlerBuilder(b => { b.PrimaryHandler = _handler; });

            services.PostConfigure<JwtOptions>(JwtOptions.SectionName, o =>
            {
                o.Issuer = "https://test-issuer";
                o.Audience = "exchange-api";
                o.SecretKey = "insecure-test-secret-key-please-replace";
                o.RequireHttpsMetadata = false;
                o.ValidateIssuer = false;
                o.ValidateAudience = false;
            });
        });
    }
}
