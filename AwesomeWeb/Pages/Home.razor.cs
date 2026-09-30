using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.JSInterop;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AwesomeWeb.Pages;

public partial class Home
{
    [Inject] private IAccessTokenProvider TokenProvider { get; set; }
    [Inject] private IConfiguration Configuration { get; set; }
    [Inject] private HttpClient Client { get; set; }
    [Inject] private IJSRuntime JS { get; set; }

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

    private IJSObjectReference? sseModule;
    private IJSObjectReference? sseWithAuthModule;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            sseModule = await JS.InvokeAsync<IJSObjectReference>("import", "./js/sseClient.js");
            sseWithAuthModule = await JS.InvokeAsync<IJSObjectReference>("import", "./js/sseClientWithLibrary.js");
        }
    }

    private string _error = string.Empty;
    private readonly Dictionary<string, DotNetObjectReference<Home>> referenceCache = new();

    private async Task StartAuthenticatedEndpoint(string id)
    {
        if (sseWithAuthModule is null)
        {
            return;
        }

        var tokenResult = await TokenProvider.RequestAccessToken();
        string accessToken = "none";
        if (tokenResult.TryGetToken(out var token))
        {
            accessToken = token.Value;
        }

        var objRef = DotNetObjectReference.Create(this);
        referenceCache[id] = objRef;

        try
        {
            await sseWithAuthModule.InvokeVoidAsync("connect", id, "https://localhost:7238/events", objRef, "HandleMessage", "HandleError", accessToken);
        }
        catch
        {
            if (referenceCache.TryGetValue(id, out var reference))
            {
                reference.Dispose();
                referenceCache.Remove(id);
            }
        }
        finally
        {
            StateHasChanged();
        }
    }

    private async Task StartEndpoint(string id)
    {
        if (sseModule is null)
        {
            return;
        }

        var tokenResult = await TokenProvider.RequestAccessToken();
        string accessToken = "none";
        if (tokenResult.TryGetToken(out var token))
        {
            accessToken = token.Value;
        }

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var slt = await Client.GetFromJsonAsync<string>("/request-slt");

        var objRef = DotNetObjectReference.Create(this);
        referenceCache[id] = objRef;

        try
        {
            await sseModule.InvokeVoidAsync("connect", id, "https://localhost:7238/events-slt", objRef, "HandleMessage", "HandleError", slt);
        }
        catch
        {
            if (referenceCache.TryGetValue(id, out var reference))
            {
                reference.Dispose();
                referenceCache.Remove(id);
            }
        }
        finally
        {
            StateHasChanged();
        }
    }

    private int _lastValue = 0;

    [JSInvokable]
    public void HandleMessage(string id, string data)
    {
        if (int.TryParse(data, out var value))
        {
            _lastValue = value;
        }

        _error = string.Empty;
        StateHasChanged();
    }

    [JSInvokable]
    public void HandleError(string id, string message)
    {
        _error = message;
        StateHasChanged();
    }

    public async ValueTask DisposeAsync()
    {
        if (sseModule is not null)
        {
            await sseModule.InvokeVoidAsync("disconnect", 1);
        }

        foreach (var reference in referenceCache.Values)
        {
            reference.Dispose();
        }
    }
}