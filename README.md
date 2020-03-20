
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

# Keybinds
Description | Control | Default QWERTY
:---: | :---: | :---:
Toggle voice range | EnterCheatCode | ~ / `
Talk on primary radio | PushToTalk | N
Talk on secondary radio | VehiclePushbikeSprint | Caps

# Events
## Client
### SaltyChat_TalkStateChanged
Parameter | Type | Description
------------ | ------------- | -------------
isTalking | `bool` | `true` if player starts talking, `false` when the player stops talking

### SaltyChat_MicStateChanged
Parameter | Type | Description
------------ | ------------- | -------------
isMicrophoneMuted | `bool` | `true` if player mutes mic, `false` when the player unmutes mic

### SaltyChat_SoundStateChanged
Parameter | Type | Description
------------ | ------------- | -------------
isSoundMuted | `bool` | `true` if player mutes sound, `false` when the player unmutes sound

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

##  How to use SaltyChat with a phone (gcPhone as an example)

Make sure:

- that you have the WebRTC feature disabled or similar (gcPhone: `html/static/config/config.json`)
- that you have removed all old code you may have added when using TokoVOIP or other modifications to your phones voice/connecting system.

  

  

After you are sure the above points are done, you can add the SaltyChat exports to the gcPhone's (or other, SERVER files where u have both the caller and the receiver's ID) for gcPhone its the `server.lua` file.

  

You will need the following lines of code:

  

To create a Call:

```lua

exports['saltychat']:EstablishCall(AppelsEnCours[id].receiver_src, AppelsEnCours[id].transmitter_src)

exports['saltychat']:EstablishCall(AppelsEnCours[id].transmitter_src, AppelsEnCours[id].receiver_src)

```

To cancel a Call:

```lua

exports['saltychat']:EndCall(AppelsEnCours[id].receiver_src, AppelsEnCours[id].transmitter_src)

exports['saltychat']:EndCall(AppelsEnCours[id].transmitter_src, AppelsEnCours[id].receiver_src)

```

  (AppelsEnCours[id] is only available for gcPhone. Make sure u have the equivallent of your caller and receiver id )

  

- Look for the 'gcPhone:acceptCall' event (or equivallent) and add the code to create a call like this:

![How to Create a call](https://screens.egopvp.com/files/2020/03/20/notepad_mnyJPbqAUB.png)

  

- Look for the 'gcPhone:rejectCall' event (or equivallent) and add the code to cancel a call like this:

![](https://screens.egopvp.com/files/2020/03/20/notepad_9SYhcnSYAB.png)

  

##  Errors

  

If you encounter errors when trying to use that code:

  

- check for Spelling mistakes (often in the `"AppelsEnCours"` part on gcPhone!)

- make sure your Phone is started AFTER SaltyChat.

- make sure u are in the server files and not the any client files.

- make sure your Phone was working before using this fix.
