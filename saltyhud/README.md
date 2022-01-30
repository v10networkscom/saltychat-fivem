# Salty Chat HUD for [FiveM](https://fivem.net/)

An example implementation of a HUD for Salty Chat on [FiveM](https://fivem.net/).  

You can report bugs or make sugguestions via issues, or contribute via pull requests - we appreciate any contribution.  
Join our [Discord](https://gaming.v10networks.com/Discord) and start with [Salty Chat](https://gaming.v10networks.com/SaltyChat)!

# Setup Steps
1. Download the latest [release](https://github.com/v10networkscom/saltychat-fivem/releases) and extract it into your resources
2. Add `start saltyhud` into your `server.cfg`

# Config
Variable | Type | Description
------------- | ------------- | -------------
Enabled | `bool` | Enables/Disables the HUD
RangeModifier | `float` | Modifier for the displayed range -> 1.0 = meters, 1.09361 = yards 
RangeText | `string` | Text of the notification when changing the voice range, `{voicerange}` will be replaced by the voice range
HideWhilePauseMenuOpen | `bool` | `true` hides the HUD while pause menu (ESC) is open

# Exports
## Client
### HideHud
Hides HUD if needed

Parameter | Type | Description
------------ | ------------- | -------------
hide | `bool` | `true` to hide HUD, `false` to display
