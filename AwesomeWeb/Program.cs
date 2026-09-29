using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using AwesomeWeb;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var authority = builder.Configuration["Authority"];
var clientId = builder.Configuration["Audience"];
var apiUrl = builder.Configuration["ApiUrl"];

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
