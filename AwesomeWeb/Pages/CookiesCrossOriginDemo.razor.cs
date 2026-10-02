using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AwesomeWeb.Pages;

public partial class CookiesCrossOriginDemo : IAsyncDisposable
{
    [Inject] private IConfiguration Configuration { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private IJSObjectReference? _module;
    private DotNetObjectReference<CookiesCrossOriginDemo>? _objRef;
    private int _lastValue;
    private string _error = string.Empty;
    private bool _isConnected;
    private const string ConnectionId = "cookies-cross-origin";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        _module = await JS.InvokeAsync<IJSObjectReference>("import", "./js/sseCookieCrossOriginClient.js");
    }

    private async Task StartAsync()
    {
        if (_module is null)
        {
            return;
        }

        try
        {
            _objRef ??= DotNetObjectReference.Create(this);
            var apiUrl = Configuration["ApiUrl"]!;

            await _module.InvokeVoidAsync("initializeCookie", apiUrl);
            await _module.InvokeVoidAsync(
                "connect",
                ConnectionId,
                $"{apiUrl}/events-cookie-cross",
                _objRef,
                nameof(HandleMessage),
                nameof(HandleError));

            _error = string.Empty;
            _isConnected = true;
        }
        catch (Exception ex)
        {
            _error = ex.Message;
            _isConnected = false;
        }

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
