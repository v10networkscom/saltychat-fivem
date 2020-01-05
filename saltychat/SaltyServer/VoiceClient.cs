using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaltyServer
{
    public class VoiceClient
    {
        #region Props/Fields
        internal CitizenFX.Core.Player Player { get; set; }
        internal string TeamSpeakName { get; set; }
        internal float VoiceRange { get; set; }
        internal bool IsAlive { get; set; }
        internal bool RadioSpeaker { get; set; }
        #endregion

        #region CTOR
        internal VoiceClient(CitizenFX.Core.Player player, string teamSpeakName, float voiceRange, bool isAlive)
        {
            this.Player = player;
            this.TeamSpeakName = teamSpeakName;
            this.VoiceRange = voiceRange;
            this.IsAlive = isAlive;
        }
        #endregion
    }
}
