# Salty Chat for [FiveM](https://fivem.net/)

[![Build Status](https://api.travis-ci.com/saltminede/saltychat-fivem.svg?branch=master)](https://travis-ci.org/saltminede/saltychat-fivem)

An example implementation of Salty Chat for [FiveM](https://fivem.net/) OneSync and OneSync Infinity.  
If you want to use Salty Chat without OneSync, use the [non-onesync](https://github.com/saltminede/saltychat-fivem/tree/non-onesync) branch.

You can report bugs or make sugguestions via issues, or contribute via pull requests - we appreciate any contribution.  
Join our [Discord](https://discord.gg/MBCnqSf) and start with [Salty Chat](https://www.saltmine.de/)!

# Setup Steps
1. Copy the folder `saltychat` into your resources
2. [Build the solution](https://github.com/saltminede/saltychat-docs/blob/master/installing-vs.md#installing-visual-studio) (`saltychat\SaltyChat-FiveM.sln`) with Visual Studio 2019, so the `*.net.dll` files get build
3. Add `start saltychat` into your `server.cfg`
4. Open `fxmanifest.lua` and adjust the [variables](https://github.com/saltminede/saltychat-docs/blob/master/setup.md#config-variables)
```
VoiceEnabled "true"
ServerUniqueIdentifier "NMjxHW5psWaLNmFh0+kjnQik7Qc="
RequiredUpdateBranch ""
MinimumPluginVersion ""
SoundPack "default"
IngameChannelId "25"
IngameChannelPassword "5V88FWWME615"
SwissChannelIds "61,62"
```

**Attention**: CFX team implemented a NUI blacklist and blocked local (`127.0.0.1` and `localhost`) WebSocket connections, so we had to use a workaround.
If the clientside can't connect to the WebSocket, make sure that you can resolve `lh.saltmine.de`:
1. Open `Windows Command Prompt` by searching `cmd`
2. Execute `nslookup lh.saltmine.de`

If it resolved to `127.0.0.1` then your issue is probably somewhere else, if not then you can use e.g. [Google DNS servers](https://developers.google.com/speed/public-dns/docs/using#google_public_dns_ip_addresses).

# Keybinds
Description | Control | Default QWERTY
:---: | :---: | :---:
Toggle voice range | EnterCheatCode | ~ / `
Talk on primary radio | PushToTalk | N
Talk on secondary radio | VehiclePushbikeSprint | Caps
Talk with Megaphone | SpecialAbilitySecondary | B

# Events
## Client
### SaltyChat_TalkStateChanged
Parameter | Type | Description
------------ | ------------- | -------------
isTalking | `bool` | `true` when player starts talking, `false` when the player stops talking

### SaltyChat_MicStateChanged
Parameter | Type | Description
------------ | ------------- | -------------
isMicrophoneMuted | `bool` | `true` when player mutes mic, `false` when the player unmutes mic

### SaltyChat_MicEnabledChanged
Parameter | Type | Description
------------ | ------------- | -------------
isMicrophoneEnabled | `bool` | `false` when player disabled mic, `true` when the player enabled mic

### SaltyChat_SoundStateChanged
Parameter | Type | Description
------------ | ------------- | -------------
isSoundMuted | `bool` | `true` when player mutes sound, `false` when the player unmutes sound

### SaltyChat_SoundEnabledChanged
Parameter | Type | Description
------------ | ------------- | -------------
isSoundEnabled | `bool` | `false` when player disabled sound, `true` when the player enabled sound

# Exports
## Client
### GetRadioChannel
Get the current radio channel.

Parameter | Type | Description
------------ | ------------- | -------------
primary | `bool` | Whether to get the primary or secondary channel

### SetRadioChannel
Set the current radio channel.

Parameter | Type | Description
------------ | ------------- | -------------
radioChannelName | `string` | Name of the radio channel
primary | `bool` | Whether to set the primary or secondary channel

## Server
### SetPlayerAlive
Sets player `IsAlive` flag.

Parameter | Type | Description
------------ | ------------- | -------------
netId | `int` | Server ID of the player
isAlive | `bool` | `true` if player is alive, otherwise `false`

### EstablishCall
Starts a call between two players.

Parameter | Type | Description
------------ | ------------- | -------------
callerNetId | `int` | Server ID of the caller
partnerNetId | `int` | Server ID of the call partner

### EndCall
Ends a call between two players.

Parameter | Type | Description
------------ | ------------- | -------------
callerNetId | `int` | Server ID of the caller
partnerNetId | `int` | Server ID of the call partner

### SetPlayerRadioSpeaker
Turns radio speaker of an player on/off.

Parameter | Type | Description
------------ | ------------- | -------------
netId | `int` | Server ID of the player
toggle | `bool` | `true` to turn on speaker, `false` to turn it off

### SetPlayerRadioChannel
Sets a player's radio channel.

Parameter | Type | Description
------------ | ------------- | -------------
netId | `int` | Server ID of the player
radioChannelName | `string` | Name of the radio channel
isPrimary | `bool` | `true` to set the channel as primary, `false` to set it as secondary

### RemovePlayerRadioChannel
Removes a player from the radio channel.

Parameter | Type | Description
------------ | ------------- | -------------
netId | `int` | Server ID of the player
radioChannelName | `string` | Name of the radio channel

### SetRadioTowers
Sets the radio towers.

Parameter | Type | Description
------------ | ------------- | -------------
towers | `float[][]` | Array with radio tower positions (X, Y, Z)
