using AwesomeWeb;
using DotNetSseClient;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var authority = builder.Configuration["Authority"];
var clientId = builder.Configuration["Audience"];
var apiUrl = builder.Configuration["ApiUrl"];

builder.Services.AddLogging();

builder.Services.AddSseClient<int>("counter-stream", apiUrl)
    .WithBearerToken(async (provider, _) =>
    {
        var accessTokenProvider = provider.GetRequiredService<IAccessTokenProvider>();
        var tokenResult = await accessTokenProvider.RequestAccessToken();
        if (tokenResult.TryGetToken(out var token))
        {
            return token.Value;
        }

        return string.Empty;
    });

builder.Services.AddScoped<HttpClient>(_ => new HttpClient { BaseAddress = new Uri(apiUrl!) });
builder.Services.AddOidcAuthentication(options =>
{
    builder.Configuration.Bind("Local", options.ProviderOptions);
    options.ProviderOptions.Authority = authority;
    options.ProviderOptions.ClientId = clientId;
    options.ProviderOptions.ResponseType = "code";
    options.ProviderOptions.DefaultScopes.Add("profile");
});

await builder.Build().RunAsync();
