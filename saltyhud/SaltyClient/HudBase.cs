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

        private bool _isDisplaying = false;
        internal bool IsDisplaying
        {
            get => this._isDisplaying;
            set
            {
                if (this._isDisplaying == value)
                    return;

                this._isDisplaying = value;

                this.SendNuiMessage(MessageType.Display, value);
            }
        }

        internal bool IsHidden { get; set; }
        internal bool IsTalking { get; set; }
        internal bool IsMicrophoneMuted { get; set; }
        internal bool IsSoundMuted { get; set; }
        #endregion

        #region CTOR
        public HudBase()
        {
            API.RegisterNuiCallbackType("OnNuiReady");

            this.Exports.Add("HideHud", new Action<bool>(this.HideHud));

            this.Configuration = JsonConvert.DeserializeObject<Configuration>(API.LoadResourceFile(API.GetCurrentResourceName(), "config.json"));
        }
        #endregion

        #region Exports
        public void HideHud(bool hide)
        {
            this.IsHidden = hide;
        }
        #endregion

        #region Salty Chat Exports
        public float GetVoiceRange() => this.Exports["saltychat"].GetVoiceRange();
        public String GetRadioChannel(bool primary) => this.Exports["saltychat"].GetRadioChannel(primary);
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
            this.SetVoiceRange(voiceRange);
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

        [EventHandler("SaltyChat_RadioChannelChanged")]
        public void OnRadioStateChanged(string radioChannel, bool isPrimaryChannel)
        {
            if (isPrimaryChannel)
            {
                if (String.IsNullOrWhiteSpace(radioChannel))
                {
                    this.SendNuiMessage(MessageType.SetRadioState, SoundState.SoundMuted);
                }
                else
                {
                    this.SendNuiMessage(MessageType.SetRadioChannel, this.Configuration.RadioText.Replace("{channel}", radioChannel));
                    this.SendNuiMessage(MessageType.SetRadioState, SoundState.Idle);
                }
            }
        }

        [EventHandler("SaltyChat_RadioTrafficStateChanged")]
        public void OnRadioStateChanged(string name, bool isSending, bool isPrimaryChannel, bool activeRelay)
        {
            if (isSending)
                this.SendNuiMessage(MessageType.SetRadioState, SoundState.Talking);
            else
                this.SendNuiMessage(MessageType.SetRadioState, SoundState.Idle);
        }
        #endregion

        #region NUI
        [EventHandler("__cfx_nui:OnNuiReady")]
        private void OnNuiReady(dynamic dummy, dynamic cb)
        {
            this.Tick += this.FirstTickAsync;

            cb("");
        }
        #endregion

        #region Tick
        private async Task FirstTickAsync()
        {
            if (this.Configuration.Enabled)
            {
                this.SetVoiceRange(this.GetVoiceRange());
                this.UpdatePosition(Configuration.Position[0], Configuration.Position[1]);

                this.Tick += this.ControlTickAsync;
            }

            this.Tick -= this.FirstTickAsync;

            await Task.FromResult(0);
        }

        private async Task ControlTickAsync()
        {
            if (this.IsHidden)
            {
                if (this.IsDisplaying)
                    this.IsDisplaying = false;
            }
            else if (this.Configuration.HideWhilePauseMenuOpen)
            {
                bool isPauseMenuActive = API.IsPauseMenuActive();

                if (this.IsDisplaying && isPauseMenuActive)
                {
                    this.IsDisplaying = false;
                }
                else if (!this.IsDisplaying && !isPauseMenuActive)
                {
                    this.IsDisplaying = true;
                }
            }
            else if (!this.IsDisplaying)
            {
                this.IsDisplaying = true;
            }

            await Task.FromResult(0);
        }
        #endregion

        #region Methods
        public void SetVoiceRange(float voiceRange)
        {
            voiceRange *= this.Configuration.RangeModifier;
            this.SendNuiMessage(MessageType.SetRange, this.Configuration.RangeText.Replace("{voicerange}", voiceRange.ToString("0.#")));
        }

        public void SendNuiMessage(MessageType type, object data)
        {
            API.SendNuiMessage(
                JsonConvert.SerializeObject(
                    new NuiMessage(type, data)
                )
            );
        }

        private void UpdatePosition(int x, int y)
        {
            Configuration.Position[0] = x;
            Configuration.Position[1] = y;

            int[] positions = { Configuration.Position[0], Configuration.Position[1] };
            this.SendNuiMessage(MessageType.SetPosition, positions);
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
