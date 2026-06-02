using Microsoft.JSInterop;

namespace Blazor.ConnectionStatusDetector;

public class ConnectionStatusDetectorService : IConnectionStatusDetectorService, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly DotNetObjectReference<ConnectionStatusDetectorService> _selfRef;
    private readonly string? _pingUrl;
    private readonly int _pingIntervalMs;

    public event EventHandler<bool>? ConnectionStatusChanged;
    public bool IsOnline { get; private set; }

    /// <param name="runtime">The JS runtime.</param>
    /// <param name="pingUrl">
    ///   Optional URL to HTTP HEAD ping to verify real internet/server connectivity.
    ///   When set, a HEAD request is made here whenever navigator.onLine is true,
    ///   and periodically on the given interval. This catches the common case of
    ///   "connected to router but no internet" that navigator.onLine can't detect.
    ///   Recommended: point this at a cheap endpoint on your own API server.
    /// </param>
    /// <param name="pingIntervalMs">
    ///   How often to re-ping in milliseconds. Ignored if pingUrl is null. Default: 30s.
    /// </param>
    public ConnectionStatusDetectorService(IJSRuntime runtime, string? pingUrl = null, int pingIntervalMs = 30_000)
    {
        _jsRuntime = runtime;
        _pingUrl = pingUrl;
        _pingIntervalMs = pingIntervalMs;
        _selfRef = DotNetObjectReference.Create(this);
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("Connection.Initialize", _selfRef, _pingUrl, _pingIntervalMs);
        }
        catch (JSException)
        {
            // JS runtime not ready yet (e.g. during server-side prerender).
            // The component stays in default state until the next connectivity change.
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("Connection.Dispose");
        }
        catch (JSException) { }

        _selfRef.Dispose();
    }

    [JSInvokable("Connection.StatusChanged")]
    public void OnConnectionStatusChanged(bool isOnline)
    {
        if (IsOnline != isOnline)
        {
            IsOnline = isOnline;
            ConnectionStatusChanged?.Invoke(this, isOnline);
        }
    }
}
