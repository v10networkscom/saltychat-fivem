# Salty Chat HUD for [FiveM](https://fivem.net/)

An example implementation of a HUD for Salty Chat on [FiveM](https://fivem.net/).  

You can report bugs or make sugguestions via issues, or contribute via pull requests - we appreciate any contribution.  
Join our [Discord](https://discord.gg/MBCnqSf) and start with [Salty Chat](https://www.saltmine.de/)!

# Setup Steps
1. Copy the folder `saltyhud` into your resources
2. [Build the solution](https://github.com/saltminede/saltychat-docs/blob/master/installing-vs.md#installing-visual-studio) (`saltychat\SaltyChat-FiveM.sln`) with Visual Studio 2019, so the `*.net.dll` files get build
3. Add `start saltyhud` into your `server.cfg`

# Config
Variable | Type | Description
------------- | ------------- | -------------
Enabled | `bool` | Enables/Disables the HUD
RangeModifier | `float` | Modifier for the displayed range -> 1.0 = meters | 1.09361 = yards 
RangeText | `string` | Text of the notification when changing the voice range, `{voicerange}` will be replaced by the voice range
HideWhilePauseMenuOpen | `bool` | `true` hides the HUD while pause menu (ESC) is open

# Exports
## Client
### SetEnabled
Enables of disables the HUD

Parameter | Type | Description
------------ | ------------- | -------------
enable | `bool` | `true` to enable the HUD, `false` to disable
