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
                    if (this._lastPrimaryRadioChannel != null)
                        this.PrimaryRadioChannel = null;

                    if (this._lastSecondaryRadioChannel != null)
                        this.SecondaryRadioChannel = null;
                }
            }
        }
        public bool IsMicClickEnabled { get; set; } = true; // ToDo: Sync with Salty Chat

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

        public List<RadioTrafficState> RadioTrafficStates { get; set; } = new List<RadioTrafficState>();
        #endregion

        #region CTOR
        public Radio()
        {
            API.RegisterNuiCallbackType("ready");
            API.RegisterNuiCallbackType("setPrimaryChannel");
            API.RegisterNuiCallbackType("setSecondaryChannel");
            API.RegisterNuiCallbackType("radioVolumeUp");
            API.RegisterNuiCallbackType("radioVolumeDown");
            API.RegisterNuiCallbackType("toggleMicClick");
            API.RegisterNuiCallbackType("toggleSpeaker");
            API.RegisterNuiCallbackType("togglePower");
            API.RegisterNuiCallbackType("unfocus");
        }
        #endregion

        #region Radio Events
        [EventHandler("SaltyChat_PrimaryRadioChannelChanged")]
        private void OnPrimaryRadioChannelChanged(string radioChannel)
        {
            if (this.IsPoweredOn)
                this._lastPrimaryRadioChannel = radioChannel;

            API.SendNuiMessage(
                new NuiMessage(
                    NuiMessageType.SetPrimaryRadioChannel,
                new RadioChannel(radioChannel)
                ).ToString()
            );
        }

        [EventHandler("SaltyChat_SecondaryRadioChannelChanged")]
        private void OnSecondaryRadioChannelChanged(string radioChannel)
        {
            if (this.IsPoweredOn)
                this._lastSecondaryRadioChannel = radioChannel;

            API.SendNuiMessage(
                new NuiMessage(
                    NuiMessageType.SetSecondaryChannel,
                    new RadioChannel(radioChannel)
                ).ToString()
            );
        }

        [EventHandler("SaltyChat_RadioTrafficStateChanged")]
        private void OnRadioTrafficStateChanged(string playerName, bool isSending, bool isPrimaryChannel, string activeRelay)
        {
            lock (this.RadioTrafficStates)
            {
                RadioTrafficState radioTrafficState = this.RadioTrafficStates.FirstOrDefault(r => r.PlayerName == playerName && r.IsPrimaryChannel == isPrimaryChannel);

                if (isSending)
                {
                    if (radioTrafficState == null)
                        this.RadioTrafficStates.Add(new RadioTrafficState(playerName, isSending, isPrimaryChannel, activeRelay));
                    else if (radioTrafficState != null && radioTrafficState.ActiveRelay != activeRelay)
                        radioTrafficState.ActiveRelay = activeRelay;
                }
                else
                {
                    if (radioTrafficState != null)
                        this.RadioTrafficStates.Remove(radioTrafficState);
                }
            }

            string localPlayerName = this.PlayerName;

            API.SendNuiMessage(
                new NuiMessage(
                    NuiMessageType.SetRadioState,
                    new RadioState(
                        this.RadioTrafficStates.Any(r => r.IsPrimaryChannel && r.IsSending && r.ActiveRelay == null && r.PlayerName != localPlayerName),
                        this.RadioTrafficStates.Any(r => r.PlayerName == localPlayerName && r.IsPrimaryChannel && r.IsSending),
                        this.RadioTrafficStates.Any(r => !r.IsPrimaryChannel && r.IsSending && r.ActiveRelay == null && r.PlayerName != localPlayerName),
                        this.RadioTrafficStates.Any(r => r.PlayerName == localPlayerName && !r.IsPrimaryChannel && r.IsSending)
                    )
                ).ToString()
            );
        }
        #endregion

        #region NUI
        [EventHandler("__cfx_nui:ready")]
        private void OnNuiReady(dynamic dummy, dynamic cb)
        {
            this._lastPrimaryRadioChannel = this.PrimaryRadioChannel;
            this._lastSecondaryRadioChannel = this.SecondaryRadioChannel;

            cb(
                new InitData(
                    this.IsPoweredOn,
                    this.IsRadioSpeakerEnabled,
                    this._lastPrimaryRadioChannel,
                    this._lastSecondaryRadioChannel,
                    this.IsMicClickEnabled,
                    this.RadioVolume
                ).ToString()
            );
        }

        [EventHandler("__cfx_nui:setPrimaryChannel")]
        private void OnNuiSetPrimaryChannel(dynamic channelName, dynamic cb)
        {
            if (channelName.GetType() != typeof(string))
                channelName = Convert.ToString(channelName);

            this.PrimaryRadioChannel = $"st_{channelName}";

            cb("");
        }

        [EventHandler("__cfx_nui:setSecondaryChannel")]
        private void OnNuiSetSecondaryChannel(dynamic channelName, dynamic cb)
        {
            if (channelName.GetType() != typeof(string))
                channelName = Convert.ToString(channelName);
            
            this.SecondaryRadioChannel = $"st_{channelName}";

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
            Debug.WriteLine("power toggle :)");

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

        #region Tick
        [Tick]
        public async Task ControlTickAsync()
        {
            if (Game.IsControlJustPressed(0, Control.InteractionMenu))
                API.SetNuiFocus(true, true);

            await Task.FromResult(0);
        }

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