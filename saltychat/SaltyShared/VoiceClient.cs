using System;
using System.Collections.Generic;
using System.Text;

namespace SaltyShared
{
    public class VoiceClient
    {
        public int PlayerId { get; set; }
        public string TeamSpeakName { get; set; }
        public float VoiceRange { get; set; }

        public VoiceClient(int playerId, string teamSpeakName, float voiceRange)
        {
            this.PlayerId = playerId;
            this.TeamSpeakName = teamSpeakName;
            this.VoiceRange = voiceRange;
        }
    }
}
