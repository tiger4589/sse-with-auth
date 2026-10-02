using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.JSInterop;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace AwesomeWeb.Pages;

public partial class ShortLivedTokenDemo : IAsyncDisposable
{
    [Inject] private IAccessTokenProvider TokenProvider { get; set; } = default!;
    [Inject] private IConfiguration Configuration { get; set; } = default!;
    [Inject] private HttpClient Client { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private IJSObjectReference? _module;
    private DotNetObjectReference<ShortLivedTokenDemo>? _objRef;
    private int _lastValue;
    private string _error = string.Empty;
    private bool _isConnected;
    private const string ConnectionId = "short-lived-token";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        _module = await JS.InvokeAsync<IJSObjectReference>("import", "./js/sseShortLivedTokenClient.js");
    }

    private async Task StartAsync()
    {
        if (_module is null)
        {
            return;
        }

        var tokenResult = await TokenProvider.RequestAccessToken();
        if (!tokenResult.TryGetToken(out var token))
        {
            _error = "Could not retrieve an access token.";
            StateHasChanged();
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, "/request-slt");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

        using var response = await Client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            _error = $"Failed to request short-lived token: {(int)response.StatusCode}";
            StateHasChanged();
            return;
        }

        var shortLivedToken = await response.Content.ReadFromJsonAsync<string>();
        if (string.IsNullOrWhiteSpace(shortLivedToken))
        {
            _error = "The API returned an empty short-lived token.";
            StateHasChanged();
            return;
        }

        _objRef ??= DotNetObjectReference.Create(this);
        _error = string.Empty;

        await _module.InvokeVoidAsync(
            "connect",
            ConnectionId,
            $"{Configuration["ApiUrl"]}/events-slt",
            _objRef,
            nameof(HandleMessage),
            nameof(HandleError),
            shortLivedToken);

        _isConnected = true;
        StateHasChanged();
    }

    private async Task StopAsync()
    {
        if (_module is null)
        {
            return;
        }

        await _module.InvokeVoidAsync("disconnect", ConnectionId);
        _isConnected = false;
        StateHasChanged();
    }

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
        _isConnected = false;
        StateHasChanged();
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            await _module.InvokeVoidAsync("disconnect", ConnectionId);
            await _module.DisposeAsync();
        }

        _objRef?.Dispose();
    }
}
