fx_version 'adamant'

game 'gta5'

ui_page 'NUI/SaltyWebSocket.html'

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

server_export 'SetPlayerAlive'
server_export 'EstablishCall'
server_export 'EndCall'
server_export 'SetPlayerRadioSpeaker'
server_export 'SetPlayerRadioChannel'
server_export 'RemovePlayerRadioChannel'
server_export 'SetRadioTowers'
