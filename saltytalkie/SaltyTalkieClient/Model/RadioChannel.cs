using Newtonsoft.Json;

namespace SaltyTalkieClient
{
    public class RadioChannel
    {
        [JsonProperty("channelName")]
        public string ChannelName { get; set; }

        public RadioChannel(string channelName)
        {
            this.ChannelName = channelName;
        }
    }
}