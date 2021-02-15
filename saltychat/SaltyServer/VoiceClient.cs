using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SaltyShared;

namespace SaltyServer
{
    public class VoiceClient
    {
        #region Props/Fields
        internal CitizenFX.Core.Player Player { get; set; }
        internal string TeamSpeakName { get; set; }
        internal float VoiceRange { get; set; }
        internal bool IsAlive { get; set; }

        private bool _isRadioSpeakerEnabled = false;
        internal bool IsRadioSpeakerEnabled
        {
            get => this._isRadioSpeakerEnabled;
            set
            {
                this._isRadioSpeakerEnabled = value;

                this.Player.TriggerEvent(Event.SaltyChat_SetRadioSpeaker, this._isRadioSpeakerEnabled);

                foreach (RadioChannelMember radioChannelMembership in VoiceManager.Instance.GetPlayerRadioChannelMembership(this))
                {
                    radioChannelMembership.RadioChannel.SetSpeaker(this, value);
                }
            }
        }
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
