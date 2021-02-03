using Newtonsoft.Json;

namespace SaltyTalkieClient
{
    public class InitData
    {
        [JsonProperty("isPoweredOn")]
        public  bool IsPoweredOn { get; set; }
        
        [JsonProperty("isSpeakerEnabled")]
        public bool IsSpeakerEnabled { get; set; }
        
        [JsonProperty("primaryChannel")]
        public string PrimaryChannel { get; set; }
        
        [JsonProperty("secondaryChannel")]
        public string SecondaryChannel { get; set; }
        
        [JsonProperty("isMicClickEnabled")]
        public bool IsMicClickEnabled { get; set; }
        
        [JsonProperty("radioVolume")]
        public int RadioVolume { get; set; }

        public InitData(bool isPoweredOn, bool speaker, string primaryChannel, string secondaryChannel, bool isMicClickEnabled, int radioVolume)
        {
            this.IsPoweredOn = isPoweredOn;
            this.IsSpeakerEnabled = speaker;
            this.PrimaryChannel = primaryChannel;
            this.SecondaryChannel = secondaryChannel;
            this.IsMicClickEnabled = isMicClickEnabled;
            this.RadioVolume = radioVolume;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}