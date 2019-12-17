resource_manifest_version "44febabe-d386-4d18-afbe-5e627f4af937"

ui_page "NUI/SaltyWebSocket.html"

client_scripts {
    "SaltyClient/bin/Debug/SaltyClient.net.dll"
}

server_scripts {
    "SaltyServer/bin/Debug/netstandard2.0/SaltyServer.net.dll"
}

files {
    "NUI/SaltyWebSocket.html",
    "Newtonsoft.Json.dll",
}

exports {
    "EstablishCall",
    "EndCall",

    "SetPlayerRadioSpeaker",
    "SetPlayerRadioChannel",
    "RemovePlayerRadioChannel",
    "SetRadioTowers"
}

VoiceEnabled "true"
ServerUniqueIdentifier "NMjxHW5psWaLNmFh0+kjnQik7Qc="
RequiredUpdateBranch ""
MinimumPluginVersion ""
SoundPack "default"
IngameChannel "25"
IngameChannelPassword "5V88FWWME615"
