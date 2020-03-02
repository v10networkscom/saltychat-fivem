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
    'NUI/SaltyWebSocket.html',
    'Newtonsoft.Json.dll',
}

exports {
    'EstablishCall',
    'EndCall',

    'SetPlayerRadioSpeaker',
    'SetPlayerRadioChannel',
    'RemovePlayerRadioChannel',
    'SetRadioTowers'
}

VoiceEnabled 'true'
ServerUniqueIdentifier 'NMjxHW5psWaLNmFh0+kjnQik7Qc='
RequiredUpdateBranch ''
MinimumPluginVersion ''
SoundPack 'default'
IngameChannelId '25'
IngameChannelPassword '5V88FWWME615'
SwissChannelIds '61,62'
