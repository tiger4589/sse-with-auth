using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace AwesomeWeb.Pages;

public partial class Home
{
    [Inject] private IAccessTokenProvider TokenProvider { get; set; }
    [Inject] private IConfiguration Configuration { get; set; }
    [Inject] private HttpClient Client { get; set; }

    public async Task CallApi()
    {
        var tokenResult = await TokenProvider.RequestAccessToken();
        string accessToken = "none";
        if (tokenResult.TryGetToken(out var token))
        {
            accessToken = token.Value;
        }

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        await Client.GetAsync("/events");
    }
}