# Peerchat

## The Database Query: BWGETPEERCHAT

After our maplist is downloaded by the client. The client sends us another request to `db.bwgame.com/query/` - this time the query payload is equal to `BWGETPEERCHAT`.
Searching our disassembly for how the response is parsed, it's simply 1 row by 1 col table containing the hostname and port like so:

| Hostname : Port          |
| ------------------------ |
| peerchat.bwgame.com:6667 |

*The tables don't have column headers, I just add those to make data representation easier to understand.*

`\0x2peerchat.bwgame.com:6667\0x3[rows]:1[columns]:1[totalcolumns]:1` is our final return string to this query, our Black & White client then resolves the hostname and
opens a TCP connection to the given hostname.

## What is Peerchat?

Before we examine the TCP connection stream anymore, it's important to know what Peerchat is first, we know it's GameSpy related due to our previous error message telling
us that the connection to GameSpy servers was lost.

The original Peerchat server was written by GameSpy and used to be hosted on `peerchat.gamespy.com` - until it was shutdown in 2014 making hundreds of games multiplayer
modes completely useless. It enabled a simple way for game developers to create lobby based game systems enabled with cd-key authentication and encryption.

PeerChat can however be described as **only a classical IRC server** which uses a very simple encryption - and *Black & White doesn't even use the encryption*. :relieved:

## Peerchat Handshake

If we go back to our TCP connection stream now and examine it, the client makes the first move by sending a simple `USRIP\r\n` command. Luckily
[aluigi](https://twitter.com/luigi_auriemma) has already reverse-engineered the Peerchat protocol and we can see the response we have to make.

`:s 302  :=+@0.0.0.0\r\n`

As soon as we send this back, the client begins sending regular IRC commands: `NICK BNW_536871013` `USER X14saFv19X|536871013 127.0.0.1 peerchat.bwgame.xyz :matt` - great.
Let's proxy them to a real IRC server now, I set a basic one up using unrealircd running on classical IRC mode since the game is from 2001 and modern IRC wasn't around.
And if we proxy the responses back to the client we get past the handshake phase into the current game list:

![Image of gamelist](/Writeups/3/gamelist.png)
![Image of peerchat](/Writeups/3/peerchat.png)
![Image of peerchat](/Writeups/3/mapselection.png)

## Few other notes

The multiplayer pretty much entirely works over the Peerchat/IRC protocol - you can create games which basically creates an IRC channel `#GSP!bandw!X14saFv19X` where the
name partially matches your encoded user credentials. Rooms are passworded, locked etc.. using default IRC modes and the creator is channel OP.

### A couple notes about the handshake:

`NICK BNW_536871013` - The argument is basically the UID we provide in login but with the bitwise operators `UID & 0x1FFFFFFF | 0x20000000` applied making the value
a much higher integer.
`USER X14saFv19X|536871013 127.0.0.1 peerchat.bwgame.xyz :matt` - The first argument is our users encoded IP address and their bit operated UID (let's call it GameSpy ID),
2nd arg: their hostname (always 127.0.0.1?), 3rd: the server name, 4th: b&w username. We'll look more into encoded IP addresses later.

### Current game list:

```
JOIN #bandw_updates
PART #bandw_updates
```

Whenever the `Refresh List` button is pressed the client sends a join, waits (more like hangs whilst it waits for a response) and then leaves the channel. This happens when
the client first logs in too. It is unclear right now what the game expects as a response but we will look into it later.

Logging multiple clients into the game works, but they can not see each other games in the list yet.

## Next

Not sure yet, will probably look into what the client expects from #bandw_updates.