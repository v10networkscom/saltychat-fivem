# Salty Chat for [FiveM](https://fivem.net/)
An example implementation of Salty Chat for [FiveM](https://fivem.net/).

You can report bugs or make sugguestions via issues, or contribute via pull requests - we appreciate any contribution.

Join our [Discord](https://discord.gg/MBCnqSf) and start with [Salty Chat](https://www.saltmine.de/)!

# Setup Steps
1. Copy the folder `saltychat` into your resources
2. Add the following into your `server.cfg` and edit it accordingly
```
## Define variables before starting the resource
set VoiceEnabled "true"
set ServerUniqueIdentifier "NMjxHW5psWaLNmFh0+kjnQik7Qc="
set RequiredUpdateBranch ""
set MinimumPluginVersion ""
set SoundPack "default"
set IngameChannel "25"
set IngameChannelPassword "5V88FWWXME615"

start saltychat
```
3. Build the project so the `*.net.dll` files get build
