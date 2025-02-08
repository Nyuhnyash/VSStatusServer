# Status Server
Vintage Story mod that provides game server information 
via popular [Server List Ping](https://wiki.vg/Server_List_Ping) protocol.

## Configure
1. Run the server with mod installed for the first time to generate default config file.
2. Open TCP port specified in mod config (You can't share the same port with Vintage Story game server).
3. <kbd>(Optional)</kbd> Add server icon (64x64, PNG only) path relative to VintagestoryServer executable (or use absolute path).

**Default config file**
```json
{
    "Port": 25565,
    "IconFile": "server-icon.png",
    "EnabledExtensions": [ "world" ]
}
```

## Build
- Set `VINTAGE_STORY` environment variable to your game directory.
- Get ready-to-publish mod as zip archive
```shell
dotnet build -c Release
```

## Usage example
There are plenty of libraries implementing the protocol: 
[mcstatus](https://github.com/py-mine/mcstatus), 
[mcutil](https://github.com/mcstatus-io/mcutil),
etc.
[More examples](https://wiki.vg/Server_List_Ping#Examples).

Ready-to-use services:
https://mcsrvstat.us, 
https://mcstatus.io,
Telegram bot [@msmpbot](https://t.me/msmpbot).

### Demonstration

#### Ping result
```shell
user@pc:~$ mcstatus localhost:25565 ping
2.0397999905981123ms
```

#### Status result
```shell
user@pc:~$ mcstatus localhost:25565 status
version: v1.18.0-pre.6 (protocol 2000)
description: "Vintage Story Server"
players: 1/16 ['Nyuhnyash (3e4a67aa-c4f1-f5f7-dffd-37e2fad5f74d)']
```

Operate raw json to access non-standard extensions data.

**Raw json**
```json5
{
    "version": {
        "protocol": 2000,
        "name": "1.18.0-pre.6"
    },
    "players": {
        "max": 16,
        "online": 1,
        "sample": [
            {
                "name": "Nyuhnyash",
                "id": "3e4a67aa-c4f1-f5f7-dffd-37e2fad5f74d"
            }
        ]
    },
    "description": {
        "text": "Vintage Story Server"
    },
    "favicon": "data:image/png;base64,<...>",
// Non-standard extension
    "world": {
        "datetime": "2. May, Year 0, 17:31"
    }
}
```
