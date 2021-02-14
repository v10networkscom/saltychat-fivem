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
        public ulong IngameChannelId { get; set; }
        public string IngameChannelPassword { get; set; }
        public ulong[] SwissChannelIds { get; set; } = new ulong[0];

        public float[] VoiceRanges { get; set; } = new float[] { 3f, 8f, 15f, 32f };
        public float MegaphoneRange { get; set; } = 120f;
        public string NamePattern { get; set; } = "{guid}";
        public bool RequestTalkStates { get; set; } = true;
        public bool RequestRadioTrafficStates { get; set; } = false;

        public int ToggleRange { get; set; } = 243; //EnterCheatCode
        public int TalkPrimary { get; set; } = 249; //PushToTalk
        public int TalkSecondary { get; set; } = 137; //VehiclePushbikeSprint
        public int TalkMegaphone { get; set; } = 29; //SpecialAbilitySecondary
    }
}
