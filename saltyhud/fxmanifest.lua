fx_version 'adamant'
game 'gta5'

ui_page 'nui/hud.html'

client_script 'SaltyClient/bin/Debug/SaltyClient.net.dll'

files {
    'SaltyClient/bin/Debug/SaltyClient.net.pdb',
    'nui/*',
    'Newtonsoft.Json.dll',
    'config.json'
}

exports {
    'SetEnabled'
}

dependencies {
    'saltychat'
}
