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

        internal float VoiceRange
        {
            get => this.Player.State[State.SaltyChat_VoiceRange] ?? 0f;
            set => this.Player.State.Set(State.SaltyChat_VoiceRange, value, true);
        }
        
        internal bool IsAlive
        {
            get => this.Player.State[State.SaltyChat_IsAlive] == true;
            set => this.Player.State.Set(State.SaltyChat_IsAlive, value, true);
        }

        internal bool IsRadioSpeakerEnabled { get; set; }
        #endregion

        #region CTOR
        internal VoiceClient(CitizenFX.Core.Player player, string teamSpeakName, float voiceRange, bool isAlive)
        {
            this.Player = player;
            this.TeamSpeakName = teamSpeakName;
            this.VoiceRange = voiceRange;
            this.IsAlive = isAlive;

            this.Player.State.Set(State.SaltyChat_TeamSpeakName, this.TeamSpeakName, true);
        }
        #endregion

        #region Methods
        internal void TriggerEvent(string eventName, params object[] args) => this.Player.TriggerEvent(eventName, args);

        internal void SetPhoneSpeakerEnabled(bool isEnabled)
        {
            foreach (PhoneCallMember phoneCallMembership in VoiceManager.Instance.GetPlayerPhoneCallMembership(this))
            {
                phoneCallMembership.PhoneCall.SetSpeaker(this, isEnabled);
            }
        }
        #endregion
    }
}
