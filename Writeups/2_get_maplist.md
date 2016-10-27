# Getting Map List

## First a storage server request

Hiding amongst all the other logic, there is a very simple GET request made to `storage.bwgame.com/bwmaps/ConditionTemplate.txt` - this is a very basic almost csv like
file format luckily a copy is included in the game itself. We just simply respond to the request with our copy of the file, no obsufucation or encryption on this one.

Without this the client refuses to go any further.

## db.bwgame.com

Our next request is a HTTP PUT request made to `db.bwgame.com/query/` with the following query string:

```
domode=1&
dbflags=14&
bwversion=140&
bwlanguage=UK&
uid=101&
uname=matt&
upass=pass&
query=HPFFEGFCCPIKEOFDCMFIFBEJCJABCNLM
```

Most of that data is self explanatory - except `domode`, `dbflags` and `query`. This one stumped me for quite a while but the basic of is `dbflags` is the plaintext length of
unencoded & unobfuscated `query`.

## LHWebEncode / LHWebDecode

For the `query` passed to `db.bwgame.com/query/` Lionhead decided to use their own base 26 [A-Z] web encoding - for every 2 characters, 1 byte [0-255] is represented - useful
for sending raw byte arrays across plaintext HTTP I guess.

All the encoding is, is simply taking the bottom 4 bits [0-15] and the top 4 bits [also 0-15] and adding 65 (ASCII A) to the value. We can work out the encoding this way:

| In   | >>4 (L) | &0xF (H) | +65 (L) | +65 (H) | ASCII (L) | ASCII (H) |
| ---- | ------- | -------- | ------- | ------- | --------- | --------- |
| 0xFF | 0x0F    | 0x0F     | 0x50    | 0x50    | P         | P         |
| 0x64 | 0x06    | 0x04     | 0x47    | 0x45    | G         | E         |
| 0x03 | 0x00    | 0x03     | 0x41    | 0x44    | A         | D         |

*We simply take each hex digit, and add 0x41 (65 dec) to it.*

If we want to decode an encoded string we simply do the opposite:
1. turn both characters into their numerical ASCII integer
2. negate 0x41 (65 dec) from each integer
3. bit shift the first/lower integer left by 4 bits
4. add them together

| In (L) | In (H) | -65 (L) | -65 (H) | Out  |
| ------ | ------ | ------- | ------- | ---- |
| P      | P      | 0x0F    | 0x0F    | 0xFF |
| G      | E      | 0x06    | 0x04    | 0x64 |
| A      | D      | 0x00    | 0x03    | 0x03 |

Using this we can now decode our query string.... partly.

## Obsufucation

Using our newly created LHWebDecode method on `HPFFEGFCCPIKEOFDCMFIFBEJCJABCNLM` we don't get a neat ASCII string like I was expecting, we get a mess of an ASCII string with a
lot of non-printable characters. Diving into the disassembly some more, the reason they encode the query string in the first place is because they first obsufucate it. :disappointed:

The obsufucation wasn't too hard - the disassembled code was a mess, but there was one number that stood out `0x61C88647` - pretty nonsensical, but as a signed 32 bit integer you get
`2654435769` which can also be represented as `232 ÷ φ` where `φ` is the golden ratio `(√5+1)÷2`.
Their obsufucation is using Fibonacci Hashing, fun. :)

There are other things on top of it, but in the end it's just a mess of code to try and stop people from viewing the plain text. If you're interested in how the obsufucation works
you can see the code for obsufucating and deobsufucating in Lionhead.cs under the method `public static byte[] Obsufucate(byte[] input, bool deob, int length = 0)`

## Decoded as BWMAPS_GETLIST

After running `HPFFEGFCCPIKEOFDCMFIFBEJCJABCNLM` through our functions for LHWebDecode and then deobfusucating we get the string `BWMAPS_GETLIST` - the client clearly wants us to give
it a map list, but in what format? Guessing at random values won't work, we'll have to check the disassembly again, luckily we can search for `BWMAPS_GETLIST` now and see how it handles
the return data.

## Constructing a Query Response

![Image of disassembly](/Writeups/2/query_parsing.png)

After a little looking in our disassembly, it became quite clear how it wanted the data formatted. It would search the response string for the following:
* `[rows]:%d` number of rows returned
* `[columns]:%d` number of columns per row
* `[totalcolumns]:%d` basically rows * columns

The client takes the number of total columns given and generates a data array for them. The client then loops through the response looking for the byte values 2 and 3, also known as
ASCII start of text and ASCII end of text; anything between these is parsed as the relative column data.

Let's construct our map table:

| ID | Name                                 | File     | Players | ? | ? |
|:--:| ------------------------------------ | -------- |:-------:|:-:|:-:|
| 1  | Bombardment - 2 players              | mpm_2p_1 | 2       | ? | ? |
| 2  | King of the hill - 3 players         | mpm_3p_1 | 3       | ? | ? |
| 3  | The four corners of Eden - 4 players | mpm_4p_1 | 4       | ? | ? |

First let's make our data descriptions: `[rows]:3[columns]:6[totalcolumns]:18` - we obviously have 3 rows, each with 6 cols, so we have a total of 18 columns.
Now we loop the data and create our full data string from that.

`\0x21\0x3\0x2Bombardment - 2 players\0x3\0x2mpm_2p_1\0x3\0x22\0x3\0x2?\0x3\0x2?\0x3\0x22\0x3\0x2King of the hill - 3 players\0x3\0x2mpm_3p_1\0x3\0x23\0x3\0x2?\0x3\0x2?\0x3\0x23\0x3\0x2The four corners of Eden - 4 players\0x3\0x2mpm_4p_1\0x3\0x24\0x3\0x2?\0x3\0x2?\0x3[rows]:3[columns]:6[totalcolumns]:18`

Tada - I'm using escaped numerical representations of the ASCII characters SOT and EOT so you can see them. We set that to our response and the downloading map list goes away and we're greeted with another query to our database server and another error. :grin:

![Image of error](/Writeups/2/gamespy_lost.png)

## Next

[3. Peerchat](/Writeups/3_peerchat.md)
