using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Newtonsoft.Json;
using SaltyShared;

namespace SaltyClient
{
    public class HudBase : BaseScript
    {
        #region Props/Fields
        internal Configuration Configuration { get; set; }

        internal bool IsMenuOpen { get; set; }

        internal float VoiceRange { get; set; }
        internal bool IsTalking { get; set; }
        internal bool IsMicrophoneMuted { get; set; }
        internal bool IsSoundMuted { get; set; }
        #endregion

        #region CTOR
        public HudBase()
        {
            this.Exports.Add("SetEnabled", new Action<bool>(this.SetEnabled));
        }
        #endregion

        #region Exports
        public void SetEnabled(bool enable)
        {
            if (enable && !this.Configuration.Enabled)
                this.Display(true);
            else if (!enable && this.Configuration.Enabled)
                this.Display(false);

            this.Configuration.Enabled = enable;
        }
        #endregion

        #region Salty Chat Exports
        public float GetVoiceRange() => this.Exports["saltychat"].GetVoiceRange();
        #endregion

        #region Event Handler
        [EventHandler("SaltyChat_PluginStateChanged")]
        public void OnPluginStateChanged(int pluginState)
        {
            this.SendNuiMessage(MessageType.PluginState, pluginState);
        }

        [EventHandler("SaltyChat_VoiceRangeChanged")]
        public void OnVoiceRangeChanged(float voiceRange, int index, int availableVoiceRanges)
        {
            this.VoiceRange = voiceRange;

            float range = this.VoiceRange * this.Configuration.RangeModifier;

            this.SendNuiMessage(MessageType.SetRange, this.Configuration.RangeText.Replace("{voicerange}", range.ToString("0.#")));
        }

        [EventHandler("SaltyChat_TalkStateChanged")]
        public void OnTalkStateChanged(bool isTalking)
        {
            this.IsTalking = isTalking;

            if (isTalking)
                this.SendNuiMessage(MessageType.SetSoundState, SoundState.Talking);
            else if (this.IsSoundMuted)
                this.SendNuiMessage(MessageType.SetSoundState, SoundState.SoundMuted);
            else if (this.IsMicrophoneMuted)
                this.SendNuiMessage(MessageType.SetSoundState, SoundState.MicrophoneMuted);
            else
                this.SendNuiMessage(MessageType.SetSoundState, SoundState.Idle);
        }

        [EventHandler("SaltyChat_MicStateChanged")]
        public void OnMicStateChanged(bool isMicrophoneMuted)
        {
            this.IsMicrophoneMuted = isMicrophoneMuted;

            if (this.IsSoundMuted)
                this.SendNuiMessage(MessageType.SetSoundState, SoundState.SoundMuted);
            else if (this.IsMicrophoneMuted)
                this.SendNuiMessage(MessageType.SetSoundState, SoundState.MicrophoneMuted);
            else
                this.SendNuiMessage(MessageType.SetSoundState, SoundState.Idle);
        }

        [EventHandler("SaltyChat_SoundStateChanged")]
        public void OnSoundStateChanged(bool isSoundMuted)
        {
            this.IsSoundMuted = isSoundMuted;

            if (this.IsSoundMuted)
                this.SendNuiMessage(MessageType.SetSoundState, SoundState.SoundMuted);
            else if (this.IsMicrophoneMuted)
                this.SendNuiMessage(MessageType.SetSoundState, SoundState.MicrophoneMuted);
            else
                this.SendNuiMessage(MessageType.SetSoundState, SoundState.Idle);
        }
        #endregion

        #region Tick
        [Tick]
        private async Task FirstTickAsync()
        {
            this.Configuration = JsonConvert.DeserializeObject<Configuration>(API.LoadResourceFile(API.GetCurrentResourceName(), "config.json"));

            this.VoiceRange = this.GetVoiceRange();

            if (this.Configuration.Enabled && (!this.Configuration.HideWhilePauseMenuOpen || !API.IsPauseMenuActive()))
                this.Display(true);

            if (this.Configuration.HideWhilePauseMenuOpen)
                this.Tick += this.ControlTickAsync;

            this.Tick -= this.FirstTickAsync;

            await Task.FromResult(0);
        }

        private async Task ControlTickAsync()
        {
            if (API.IsPauseMenuActive() && !this.IsMenuOpen)
            {
                this.IsMenuOpen = true;

                this.Display(false);
            }
            else if (!API.IsPauseMenuActive() && this.IsMenuOpen)
            {
                this.IsMenuOpen = false;

                this.Display(true);
            }

            await Task.FromResult(0);
        }
        #endregion

        #region Methods
        public void Display(bool display) => this.SendNuiMessage(MessageType.Display, display);

        public void SendNuiMessage(MessageType type, object data)
        {
            API.SendNuiMessage(
                JsonConvert.SerializeObject(
                    new NuiMessage(type, data)
                )
            );
        }
        #endregion
    }

    public enum SoundState
    {
        Idle = 0,
        Talking = 1,
        MicrophoneMuted = 2,
        SoundMuted = 3
    }
}
