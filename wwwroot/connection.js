let _handler;
let _dotnet;
let _pingInterval;
let _pingUrl;

window.Connection = {
    Initialize: function (interop, pingUrl, pingIntervalMs) {
        _dotnet = interop;
        _pingUrl = pingUrl || null;

        // Called by browser online/offline events and directly on init
        _handler = function () {
            if (!navigator.onLine) {
                // Network adapter is offline — trust it immediately, no need to ping
                _dotnet.invokeMethodAsync("Connection.StatusChanged", false);
            } else if (_pingUrl) {
                // navigator.onLine is true but only means the NIC is up, not that
                // there's actual internet. Verify with a real HTTP request.
                Connection._doPing();
            } else {
                // No ping URL configured — fall back to navigator.onLine as before
                _dotnet.invokeMethodAsync("Connection.StatusChanged", true);
            }
        };

        window.addEventListener("online", _handler);
        window.addEventListener("offline", _handler);

        // Report initial connectivity state immediately on startup
        _handler();

        // Periodic ping to catch "connected to router but no internet" —
        // the browser online/offline events won't fire in that scenario
        if (_pingUrl && pingIntervalMs > 0) {
            _pingInterval = setInterval(() => Connection._doPing(), pingIntervalMs);
        }
    },

    _doPing: async function () {
        try {
            const response = await fetch(_pingUrl + '?_t=' + Date.now(), {
                method: 'HEAD',
                cache: 'no-store',
                signal: AbortSignal.timeout(5000)
            });
            _dotnet.invokeMethodAsync("Connection.StatusChanged", response.ok);
        } catch {
            _dotnet.invokeMethodAsync("Connection.StatusChanged", false);
        }
    },

    Dispose: function () {
        if (_handler) {
            window.removeEventListener("online", _handler);
            window.removeEventListener("offline", _handler);
            _handler = null;
        }
        if (_pingInterval) {
            clearInterval(_pingInterval);
            _pingInterval = null;
        }
        _dotnet = null;
    }
};
