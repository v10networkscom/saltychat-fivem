fx_version 'adamant'

game 'gta5'

ui_page 'NUI/SaltyWebSocket.html'
mono_rt2 'Prerelease expiring 2023-12-31. See https://aka.cfx.re/mono-rt2-preview for info.'

dependencies {
    '/onesync',
    '/server:6721'
}

client_scripts {
    'SaltyClient/bin/Debug/SaltyClient.net.dll'
}

server_scripts {
    'SaltyServer/bin/Debug/netstandard2.0/SaltyServer.net.dll'
}

files {
    'SaltyClient/bin/Debug/SaltyClient.net.pdb',
    'NUI/SaltyWebSocket.html',
    'Newtonsoft.Json.dll',
    'config.json',
}

exports {
    'GetVoiceRange',
    'GetRadioChannel',
    'GetRadioVolume',
    'GetRadioSpeaker',
    'GetMicClick',
    'SetRadioChannel',
    'SetRadioVolume',
    'SetRadioSpeaker',
    'SetMicClick',
    'GetPluginState',
    'PlaySound'
}

server_export 'GetPlayerAlive'
server_export 'SetPlayerAlive'
server_export 'GetPlayerVoiceRange'
server_export 'SetPlayerVoiceRange'
server_export 'EstablishCall'
server_export 'EndCall'
server_export 'GetPlayersInRadioChannel'
server_export 'SetPlayerRadioSpeaker'
server_export 'SetPlayerRadioChannel'
server_export 'RemovePlayerRadioChannel'
server_export 'SetRadioTowers'
