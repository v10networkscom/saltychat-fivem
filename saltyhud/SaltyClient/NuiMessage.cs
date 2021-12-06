using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SaltyClient
{
    public class NuiMessage
    {
        [JsonProperty("type")]
        public MessageType Type { get; set; }

        [JsonProperty("data")]
        public object Data { get; set; }

        public NuiMessage (MessageType type, object data)
        {
            this.Type = type;
            this.Data = data;
        }
    }

    public enum MessageType
    {
        Display = 0,
        PluginState = 1,
        SetRange = 2,
        SetSoundState = 3,
        SetRadioChannel = 4,
        SetRadioState = 5,
        SetPosition = 6
    }
}
