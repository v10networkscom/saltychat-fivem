using Newtonsoft.Json;

namespace SaltyTalkieClient
{
    public class RadioState
    {
        [JsonProperty("primaryReceive")]
        public bool PrimaryReceive { get; set; }
        
        [JsonProperty("primaryTransmit")]
        public bool PrimaryTransmit { get; set; }
        
        [JsonProperty("secondaryReceive")]
        public bool SecondaryReceive { get; set; }
        
        [JsonProperty("secondaryTransmit")]
        public bool SecondaryTransmit { get; set; }

        public RadioState(bool primaryReceive, bool primaryTransmit, bool secondaryReceive, bool secondaryTransmit)
        {
            this.PrimaryReceive = primaryReceive;
            this.PrimaryTransmit = primaryTransmit;
            this.SecondaryReceive = secondaryReceive;
            this.SecondaryTransmit = secondaryTransmit;
        }
    }
}