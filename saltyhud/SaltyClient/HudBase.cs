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
        internal bool RadioActive { get; set; }
        internal int TickCounter { get; set; }
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
        public String GetRadioChannel() => this.Exports["saltychat"].GetRadioChannel(true);
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
            {
                this.SendNuiMessage(MessageType.SetSoundState, SoundState.Talking);
            }
            else if (this.IsSoundMuted)
            {
                this.SendNuiMessage(MessageType.SetSoundState, SoundState.SoundMuted);
            }
            else if (this.IsMicrophoneMuted)
            {
                this.SendNuiMessage(MessageType.SetSoundState, SoundState.MicrophoneMuted);
            }
            else
            {
                this.SendNuiMessage(MessageType.SetSoundState, SoundState.Idle);
            }
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
        [EventHandler("SaltyChat_RadioTrafficStateChanged")]
        public void OnRadioStateChanged(String name, bool isSending, bool isPrimaryChannel, bool activeRelay)
        {
            if (isSending)
                this.SendNuiMessage(MessageType.SetRadioState, SoundState.Talking);
            else
                this.SendNuiMessage(MessageType.SetRadioState, SoundState.Idle);
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

            this.Tick += this.ControlTickAsync;

            this.Tick -= this.FirstTickAsync;

            this.UpdatePosition(Configuration.Position[0], Configuration.Position[1]);

            await Task.FromResult(0);
        }

        private async Task ControlTickAsync()
        {

            if (this.Configuration.HideWhilePauseMenuOpen)
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
            }

            UpdateRadioChannel();

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

        private void UpdatePosition(int x, int y)
        {
            Configuration.Position[0] = x;
            Configuration.Position[1] = y;

            int[] positions = { Configuration.Position[0], Configuration.Position[1] };
            this.SendNuiMessage(MessageType.SetPosition, positions);
        }

        private void UpdateRadioChannel()
        {
            /* Better would be an event on change of radio channel */
            this.TickCounter = (this.TickCounter + 1) % 100;

            if (this.TickCounter == 0)
            {
                String radioChannel = this.GetRadioChannel();

                if (radioChannel == null)
                {
                    //this.SendNuiMessage(MessageType.SetRadioChannel, "");
                    if (this.RadioActive == true)
                    {
                        this.SendNuiMessage(MessageType.SetRadioState, SoundState.SoundMuted);
                        this.RadioActive = false;
                    }
                }
                else
                {
                    if (this.RadioActive == false)
                    {
                        this.SendNuiMessage(MessageType.SetRadioChannel, this.Configuration.RadioText.Replace("{channel}", radioChannel));
                        this.SendNuiMessage(MessageType.SetRadioState, SoundState.Idle);
                        this.RadioActive = true;
                    }
                }
            }
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
