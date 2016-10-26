# login.bwgame.com

## Getting Started

Starting on the login server made sense, it's the first process of the game client when you want to initiate an online session. After pressing Multiplayer you're greeted with this dialog:

![Image of login dialog](/Writeups/1/login.png)

## The Request

Entering some basic details and running a network monitor tool before we login, we can see a simple HTTP request to login.bwgame.com

> GET /login/?username=matt&userpassword=matt

Just a simple GET request with a plaintext username and password over HTTP - secure. :smile: 
We can use this easily to create some authentication system - let's work on the response.

## The Response

This isn't as easy because we don't have any data or packet dumps from the actual server to work on. We're going to need to open our disassembly of the game and see how the client handles
the response data to create a response. Responding any data not in the correct format results in an incorrect password dialog.

![Image of ida pro](/Writeups/1/parseuserida.png)

Following some disassembly we find that the response data is parsed like so: `bnwuserid:%d %d %d %s` We can immediately tell the first digit is a unique user id, the rest of the values
are currently unknown and don't seem to effect anything noticable. We can look into them some other time.

Sending a response of `bnwuserid:1 1 1 hello` will get us past the initial login stage. :smile: And get us greeted by our next step:

![Image of error](/Writeups/1/error_downloading_maplist.png)

## Next

[2. Getting Map List](/Writeups/2_get_maplist.md)
