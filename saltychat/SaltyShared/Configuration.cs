using System;
using System.Collections.Generic;
using System.Text;

namespace SaltyShared
{
    public class Configuration
    {
        public bool VoiceEnabled { get; set; } = true;
        public string ServerUniqueIdentifier { get; set; }
        public string MinimumPluginVersion { get; set; }
        public string SoundPack { get; set; }
        public long IngameChannelId { get; set; }
        public string IngameChannelPassword { get; set; }
        public long[] SwissChannelIds { get; set; } = new long[0];

        public int ToggleRange { get; set; } = 243; //EnterCheatCode
        public int TalkPrimary { get; set; } = 249; //PushToTalk
        public int TalkSecondary { get; set; } = 137; //VehiclePushbikeSprint
        public int TalkMegaphone { get; set; } = 29; //SpecialAbilitySecondary
    }
}
