using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace SaltyTalkieClient
{
    public class Radio : BaseScript
    {
        #region Props/Fields
        public Configuration Configuration { get; set; }

        public bool _isPoweredOn = true;
        public bool IsPoweredOn
        {
            get => this._isPoweredOn;
            set
            {
                this._isPoweredOn = value;

                if (value)
                {
                    if (this._lastPrimaryRadioChannel != null)
                        this.PrimaryRadioChannel = this._lastPrimaryRadioChannel;

                    if (this._lastSecondaryRadioChannel != null)
                        this.SecondaryRadioChannel = this._lastSecondaryRadioChannel;
                }
                else
                {
                    if (this.PrimaryRadioChannel != null)
                    {
                        this._lastPrimaryRadioChannel = this.PrimaryRadioChannel;
                        this.PrimaryRadioChannel = null;
                    }

                    if (this.SecondaryRadioChannel != null)
                    {
                        this._lastSecondaryRadioChannel = this.SecondaryRadioChannel;
                        this.SecondaryRadioChannel = null;
                    }
                }
            }
        }
        public bool IsMicClickEnabled
        {
            get => this.Exports["saltychat"].GetMicClick();
            set => this.Exports["saltychat"].SetMicClick(value);
        }

        public string PlayerName => this.Exports["saltychat"].GetPlayerName();

        private string _lastPrimaryRadioChannel;
        public string PrimaryRadioChannel
        {
            get => this.Exports["saltychat"].GetRadioChannel(true);
            set => this.Exports["saltychat"].SetRadioChannel(value, true);
        }

        private string _lastSecondaryRadioChannel;
        public string SecondaryRadioChannel
        {
            get => this.Exports["saltychat"].GetRadioChannel(false);
            set => this.Exports["saltychat"].SetRadioChannel(value, false);
        }

        public int RadioVolume
        {
            get => (int) (this.Exports["saltychat"].GetRadioVolume() * 100);
            set
            {
                float volume = (float) value;
                this.Exports["saltychat"].SetRadioVolume(volume / 100);
            }
        }

        public bool IsRadioSpeakerEnabled
        {
            get => this.Exports["saltychat"].GetRadioSpeaker();
            set => this.Exports["saltychat"].SetRadioSpeaker(value);
        }

        public float VoiceRange
        {
            get => this.Exports["saltychat"].GetVoiceRange();
        }
        #endregion

        #region CTOR
        public Radio()
        {
            this.Configuration = Newtonsoft.Json.JsonConvert.DeserializeObject<Configuration>(API.LoadResourceFile(API.GetCurrentResourceName(), "config.json"));
            this._isPoweredOn = this.Configuration.IsPoweredOnStartup;

            API.RegisterNuiCallbackType("ready");
            API.RegisterNuiCallbackType("setPrimaryChannel");
            API.RegisterNuiCallbackType("setSecondaryChannel");
            API.RegisterNuiCallbackType("radioVolumeUp");
            API.RegisterNuiCallbackType("radioVolumeDown");
            API.RegisterNuiCallbackType("toggleMicClick");
            API.RegisterNuiCallbackType("toggleSpeaker");
            API.RegisterNuiCallbackType("togglePower");
            API.RegisterNuiCallbackType("unfocus");

            API.RegisterCommand("+focusRadio", new Action(this.OnFocusPressed), false);
            API.RegisterCommand("-focusRadio", new Action(this.OnFocusReleased), false);
            API.RegisterKeyMapping("+focusRadio", "Focus Radio", "keyboard", this.Configuration.DisplayRadioKeybind);
        }
        #endregion

        #region Radio Events
        [EventHandler("SaltyChat_RadioChannelChanged")]
        private void OnPrimaryRadioChannelChanged(string radioChannel, bool isPrimaryChannel)
        {
            API.SendNuiMessage(
                new NuiMessage(
                    isPrimaryChannel ? NuiMessageType.SetPrimaryRadioChannel : NuiMessageType.SetSecondaryRadioChannel,
                new RadioChannel(radioChannel)
                ).ToString()
            );
        }

        [EventHandler("SaltyChat_RadioTrafficStateChanged")]
        private void OnRadioTrafficStateChanged(bool primaryReceive, bool primaryTransmit, bool secondaryReceive, bool secondaryTransmit)
        {
            API.SendNuiMessage(
                new NuiMessage(
                    NuiMessageType.SetRadioState,
                    new RadioState(primaryReceive, primaryTransmit, secondaryReceive, secondaryTransmit)
                ).ToString()
            );
        }
        #endregion

        #region NUI
        [EventHandler("__cfx_nui:ready")]
        private void OnNuiReady(dynamic dummy, dynamic cb)
        {
            cb(
                new InitData(
                    this.IsPoweredOn,
                    this.IsRadioSpeakerEnabled,
                    this.PrimaryRadioChannel,
                    this.SecondaryRadioChannel,
                    this.IsMicClickEnabled,
                    this.RadioVolume
                ).ToString()
            );
        }

        [EventHandler("__cfx_nui:setPrimaryChannel")]
        private void OnNuiSetPrimaryChannel(dynamic channelName, dynamic cb)
        {
            if (channelName == null)
            {
                this.PrimaryRadioChannel = null;
            }
            else
            {
                if (channelName.GetType() != typeof(string))
                    channelName = Convert.ToString(channelName);

                this.PrimaryRadioChannel = $"st_{channelName}";
            }

            cb("");
        }

        [EventHandler("__cfx_nui:setSecondaryChannel")]
        private void OnNuiSetSecondaryChannel(dynamic channelName, dynamic cb)
        {
            if (channelName == null)
            {
                this.SecondaryRadioChannel = null;
            }
            else
            {
                if (channelName.GetType() != typeof(string))
                    channelName = Convert.ToString(channelName);

                this.SecondaryRadioChannel = $"st_{channelName}";
            }

            cb("");
        }

        [EventHandler("__cfx_nui:toggleMicClick")]
        private void OnNuiToggleMicClick(dynamic dummy, dynamic cb)
        {
            this.IsMicClickEnabled = !this.IsMicClickEnabled;

            cb(this.IsMicClickEnabled);
        }

        [EventHandler("__cfx_nui:toggleSpeaker")]
        private void OnNuiToggleSpeaker(dynamic dummy, dynamic cb)
        {
            this.IsRadioSpeakerEnabled = !this.IsRadioSpeakerEnabled;

            cb(this.IsRadioSpeakerEnabled);
        }

        [EventHandler("__cfx_nui:togglePower")]
        private void OnNuiTogglePower(dynamic dummy, dynamic cb)
        {
            this.IsPoweredOn = !this.IsPoweredOn;

            cb(this.IsPoweredOn);
        }

        [EventHandler("__cfx_nui:radioVolumeUp")]
        private void OnNuiVolumeUp(dynamic dummy, dynamic cb)
        {
            this.RadioVolume += 10;

            cb(this.RadioVolume);
        }

        [EventHandler("__cfx_nui:radioVolumeDown")]
        private void OnNuiVolumeDown(dynamic dummy, dynamic cb)
        {
            this.RadioVolume -= 10;

            cb(this.RadioVolume);
        }

        [EventHandler("__cfx_nui:unfocus")]
        private void OnNuiUnfocus(dynamic dummy, dynamic cb)
        {
            API.SetNuiFocus(false, false);

            cb("");
        }
        #endregion

        #region Keybinds
        private void OnFocusPressed()
        {
            API.SendNuiMessage(new NuiMessage().ToString());

            API.SetNuiFocus(true, true);
        }

        private void OnFocusReleased()
        {
            // Dummy
        }
        #endregion

        #region Tick
#if DEBUG
        [Tick]
        public async Task FirstTickAsync()
        {
            this.Tick -= this.FirstTickAsync;

            API.SetNuiFocus(false, false);

            await Task.FromResult(0);
        }

        //[Tick]
        public async Task RadioTrafficDummyAsync()
        {
            API.SendNuiMessage(
                new NuiMessage(
                    NuiMessageType.SetRadioState,
                    new RadioState(
                        true,
                        false,
                        false,
                        true
                    )
                ).ToString()
            );

            await BaseScript.Delay(888);
            
            API.SendNuiMessage(
                new NuiMessage(
                    NuiMessageType.SetRadioState,
                    new RadioState(
                        false,
                        true,
                        true,
                        false
                    )
                ).ToString()
            );
            
            await BaseScript.Delay(888);
        }
#endif
        #endregion
    }
}
