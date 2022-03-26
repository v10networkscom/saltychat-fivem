using Newtonsoft.Json;

namespace SaltyTalkieClient
{
    public class NuiMessage
    {
        [JsonProperty("messageType")]
        public NuiMessageType MessageType { get; set; }
        
        [JsonProperty("body")]
        public dynamic Body { get; set; }

        public NuiMessage()
        {
            this.MessageType = NuiMessageType.Focus;
        }

        public NuiMessage(NuiMessageType messageType, dynamic body)
        {
            this.MessageType = messageType;
            this.Body = body;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
