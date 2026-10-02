using DotNetSseClient;
using Microsoft.AspNetCore.Components;

namespace AwesomeWeb.Pages;

public partial class HttpClientDemo
{
    [Inject(Key = "counter-stream")] private SseClient<int> SseClient { get; set; } = default!;

    private int _lastValue;
    private bool _isConnected;

    private async Task StartAsync()
    {
        if (_isConnected)
        {
            return;
        }

        _isConnected = true;
        StateHasChanged();

        try
        {
            await SseClient.StartAsync("/events", value =>
            {
                _lastValue = value;
                InvokeAsync(StateHasChanged);
            });
        }
        finally
        {
            _isConnected = false;
            await InvokeAsync(StateHasChanged);
        }
    }

}
