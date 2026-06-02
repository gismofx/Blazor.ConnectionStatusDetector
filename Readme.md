# Blazor.ConnectionStatusDetector

A Blazor service and component for detecting client-side connection status changes (online/offline).

## Features

- `ConnectionStatusDetectorService` — exposes an `IsOnline` property and a `ConnectionStatusChanged` event
- `Connection` component — renders different fragments based on connection status
- Optional HTTP ping to verify **actual** internet/server connectivity, not just whether the network adapter is up (`navigator.onLine` alone cannot detect "connected to router but no internet")
- Periodic re-ping to catch connectivity loss between browser events
- Targets **net8.0** and **net10.0**

## Installation

```powershell
dotnet add package Blazor.ConnectionStatusDetector
```

## Setup

### 1. Add the script to `index.html`

```html
<script src="_content/Blazor.ConnectionStatusDetector/connection.js"></script>
```

### 2. Register the service in `Program.cs`

Basic (uses `navigator.onLine` only):

```csharp
builder.Services.AddSingleton<IConnectionStatusDetectorService, ConnectionStatusDetectorService>();
```

Recommended — with HTTP ping to verify real connectivity:

```csharp
builder.Services.AddSingleton<IConnectionStatusDetectorService>(sp =>
    new ConnectionStatusDetectorService(
        sp.GetRequiredService<IJSRuntime>(),
        pingUrl: "https://your-api.com/api/health", // any cheap endpoint you control
        pingIntervalMs: 30_000));                    // re-ping every 30s
```

> **Why ping?** `navigator.onLine` only reflects whether the network adapter is connected — it returns `true` even when the machine is connected to a router with no internet access. Providing a `pingUrl` catches this case by making a real HTTP HEAD request to verify connectivity.

### 3. Import the namespace in `_Imports.razor`

```csharp
@using Blazor.ConnectionStatusDetector
```

## Usage

### Connection component

Renders different content based on connection status:

```html
<Connection>
    <Online>
        <span style="color: green">Online</span>
    </Online>
    <Offline>
        <span style="color: red">Offline</span>
    </Offline>
</Connection>
```

### Service directly

Inject `IConnectionStatusDetectorService` to query or subscribe to status changes:

```csharp
@inject IConnectionStatusDetectorService ConnectionStatus
@implements IDisposable

<p>Status: @(ConnectionStatus.IsOnline ? "Online" : "Offline")</p>

@code {
    private EventHandler<bool> _handler = default!;

    protected override void OnInitialized()
    {
        _handler = (_, isOnline) => InvokeAsync(StateHasChanged);
        ConnectionStatus.ConnectionStatusChanged += _handler;
    }

    public void Dispose() => ConnectionStatus.ConnectionStatusChanged -= _handler;
}
```

## How it works

1. On startup, the JS layer reads `navigator.onLine` and reports initial state immediately
2. Browser `online`/`offline` events trigger fast status updates
3. When `navigator.onLine` is true and a `pingUrl` is configured, an HTTP HEAD request is made to verify real connectivity
4. A periodic timer re-pings on the configured interval to catch cases where connectivity is lost without a browser event firing

## Parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `pingUrl` | `string?` | `null` | URL to HTTP HEAD ping for real connectivity verification |
| `pingIntervalMs` | `int` | `30000` | Ping interval in milliseconds (ignored if `pingUrl` is null) |
