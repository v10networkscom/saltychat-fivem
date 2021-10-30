using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaltyClient
{
    public class VoiceClient
    {
        public int ServerId { get; set; }
        public CitizenFX.Core.Player Player => VoiceManager.PlayerList[this.ServerId];
        public string TeamSpeakName { get; set; }
        public float VoiceRange { get; set; }
        public bool IsAlive { get; set; }
        public bool IsUsingMegaphone { get; set; }
        public CitizenFX.Core.Vector3 LastPosition { get; set; }
        public bool DistanceCulled { get; set; }

        public VoiceClient(int serverId, string teamSpeakName, float voiceRange, bool isAlive)
        {
            this.ServerId = serverId;
            this.TeamSpeakName = teamSpeakName;
            this.VoiceRange = voiceRange;
            this.IsAlive = isAlive;
        }

        internal void SendPlayerStateUpdate(VoiceManager voiceManager)
        {
            voiceManager.ExecuteCommand(new PluginCommand(Command.PlayerStateUpdate, voiceManager.Configuration.ServerUniqueIdentifier, new PlayerState(this.TeamSpeakName, this.LastPosition, this.VoiceRange, this.IsAlive, this.DistanceCulled)));
        }
    }
}
