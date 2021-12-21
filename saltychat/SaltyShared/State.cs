using System;
using System.Collections.Generic;
using System.Text;

namespace SaltyShared
{
    public static class State
    {
        #region Player States
        public const string SaltyChat_TeamSpeakName = "SaltyChat_TeamSpeakName";
        public const string SaltyChat_VoiceRange = "SaltyChat_VoiceRange";
        public const string SaltyChat_IsAlive = "SaltyChat_IsAlive";
        public const string SaltyChat_IsUsingMegaphone = "SaltyChat_IsUsingMegaphone";
        #endregion

        #region Global States
        public const string SaltyChat_RadioChannelMember = "SaltyChat_RadioChannelMember";
        public const string SaltyChat_RadioChannelSender = "SaltyChat_RadioChannelSender";
        #endregion
    }
}
