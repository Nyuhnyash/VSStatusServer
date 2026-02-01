# Status Server Revised

Vintage Story mod that provides game server information via the popular [Server List Ping](https://wiki.vg/Server_List_Ping) protocol.

This is an optimized and extended fork of the original [Status Server](https://github.com/Nyuhnyash/VSStatusServer) mod by Nyuhnyash.

## Features

- **Server List Ping protocol** — compatible with Minecraft server list tools
- **Rate limiting** — per-IP request throttling for DDoS protection
- **Parallel client handling** — non-blocking request processing
- **Extension system** — modular architecture for custom status data
- **Configurable** — timeouts, connection limits, and more

## Installation

1. Download the latest release (`StatusServerRevised_vX.X.X.zip`)
2. Place it in your server's `Mods` folder
3. Start the server to generate the config file
4. Open TCP port specified in config (default: 25565)

## Configuration

Config file: `ModConfig/statusserverrevised.json`

```json
{
    "Port": 25565,
    "IconFile": "server-icon.png",
    "StartDelaySeconds": 10,
    "EnabledExtensions": ["world"],   
    "ClientTimeoutMs": 5000,
    "Backlog": 10,
    "MaxConcurrentConnections": 50,
    "EnableRateLimiting": true,
    "RateLimitWindowSeconds": 60,
    "RateLimitMaxRequests": 30
}
```

### Options

| Option | Default | Description |
|--------|---------|-------------|
| `Port` | 25565 | TCP port for the status server |
| `IconFile` | server-icon.png | Server icon path (64x64 PNG) |
| `StartDelaySeconds` | 10 | Delay before starting listener (prevents early request errors) |
| `EnabledExtensions` | ["world"] | Enabled status extensions |
| `ClientTimeoutMs` | 5000 | Client connection timeout |
| `Backlog` | 10 | Maximum pending connections |
| `MaxConcurrentConnections` | 50 | Maximum simultaneous connections |
| `EnableRateLimiting` | true | Enable per-IP rate limiting |
| `RateLimitWindowSeconds` | 60 | Rate limit time window |
| `RateLimitMaxRequests` | 30 | Max requests per IP per window |

## Build

```shell
# Set VINTAGE_STORY environment variable to your game directory
dotnet build -c Release
```

Output: `bin/Release/StatusServerRevised_v1.0.0.zip`

## Usage

Compatible with any Server List Ping client:
- [mcstatus](https://github.com/py-mine/mcstatus) (Python)
- [mcutil](https://github.com/mcstatus-io/mcutil) (Go)
- [More examples](https://wiki.vg/Server_List_Ping#Examples)

Online services: [mcsrvstat.us](https://mcsrvstat.us), [mcstatus.io](https://mcstatus.io)

### Example

```shell
$ mcstatus localhost:25565 status
version: v1.19.0 (protocol 2000)
description: "Vintage Story Server"
players: 1/16 ['RainYbit (3e4a67aa-c4f1-f5f7-dffd-37e2fad5f74d)']
```

### JSON Response

```json
{
    "version": {
        "protocol": 2000,
        "name": "1.19.0"
    },
    "players": {
        "max": 16,
        "online": 1,
        "sample": [
            {
                "name": "RainYbit",
                "id": "3e4a67aa-c4f1-f5f7-dffd-37e2fad5f74d"
            }
        ]
    },
    "description": {
        "text": "Vintage Story Server"
    },
    "favicon": "data:image/png;base64,<...>",
    "world": {
        "datetime": "2. May, Year 0, 17:31"
    }
}
```

## Credits

- Original mod by [Nyuhnyash](https://github.com/Nyuhnyash/VSStatusServer)
- Revised version by RainYbit
