﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using SaltyShared;
using Newtonsoft.Json;

namespace SaltyClient
{
    internal class VoiceManager : BaseScript
    {
        #region Properties / Fields
        public bool IsEnabled { get; private set; }
        public bool IsConnected { get; private set; }
        private GameInstanceState _pluginState = GameInstanceState.NotInitiated;
        public GameInstanceState PlguinState
        {
            get => this._pluginState;
            set
            {
                this._pluginState = value;

                BaseScript.TriggerEvent(Event.SaltyChat_PluginStateChanged, (int)value);
            }
        }
        public bool IsNuiReady { get; private set; }

        public string TeamSpeakName { get; private set; }
        public bool IsAlive => Game.Player.GetIsAlive();
        public Configuration Configuration { get; private set; }

        public VoiceClient[] VoiceClients => this._voiceClients.Values.ToArray();
        private Dictionary<int, VoiceClient> _voiceClients = new Dictionary<int, VoiceClient>();

        public Tower[] RadioTowers { get; private set; }
        public CitizenFX.Core.UI.Notification RangeNotification { get; set; }

        public string WebSocketAddress { get; private set; } = "lh.v10.network:38088";

        private float _voiceRange = 0f;
        public float VoiceRange
        {
            get => this._voiceRange;
            private set
            {
                this._voiceRange = value;

                BaseScript.TriggerEvent(Event.SaltyChat_VoiceRangeChanged, value, Array.IndexOf(this.Configuration.VoiceRanges, value), this.Configuration.VoiceRanges.Length);
            }
        }

        public bool _canSendRadioTraffic = true;
        public bool CanSendRadioTraffic
        {
            get => this._canSendRadioTraffic;
            private set
            {
                if (this._canSendRadioTraffic == value || !this.Configuration.EnableRadioHardcoreMode)
                    return;

                this._canSendRadioTraffic = value;

                if (!value)
                {
                    foreach (RadioTraffic radioTraffic in this.RadioTrafficStates.ToArray())
                    {
                        if (radioTraffic.Name == this.TeamSpeakName)
                        {
                            if (radioTraffic.RadioChannelName == this.PrimaryRadioChannel)
                                this.OnPrimaryRadioReleased();
                            else if (radioTraffic.RadioChannelName == this.SecondaryRadioChannel)
                                this.OnSecondaryRadioReleased();
                        }
                    }
                }
            }
        }
        public bool _canReceiveRadioTraffic = true;
        public bool CanReceiveRadioTraffic
        {
            get => this._canReceiveRadioTraffic;
            private set
            {
                if (this._canReceiveRadioTraffic == value || !this.Configuration.EnableRadioHardcoreMode)
                    return;

                this._canReceiveRadioTraffic = value;

                if (value)
                {
                    foreach (RadioTraffic radioTraffic in this.RadioTrafficStates.ToArray().Where(r => r.Name != this.TeamSpeakName))
                    {
                        this.ExecuteCommand(
                            new PluginCommand(
                                Command.RadioCommunicationUpdate,
                                this.Configuration.ServerUniqueIdentifier,
                                new RadioCommunication(
                                    radioTraffic.Name,
                                    radioTraffic.SenderRadioType,
                                    radioTraffic.ReceiverRadioType,
                                    false,
                                    radioTraffic.RadioChannelName == this.PrimaryRadioChannel || radioTraffic.RadioChannelName == this.SecondaryRadioChannel,
                                    radioTraffic.RadioChannelName == this.SecondaryRadioChannel,
                                    radioTraffic.Relays,
                                    this.RadioVolume
                                )
                            )
                        );
                    }
                }
                else
                {
                    foreach (RadioTraffic radioTraffic in this.RadioTrafficStates.ToArray().Where(r => r.Name != this.TeamSpeakName))
                    {
                        this.ExecuteCommand(
                            new PluginCommand(
                                Command.StopRadioCommunication,
                                this.Configuration.ServerUniqueIdentifier,
                                new RadioCommunication(
                                    radioTraffic.Name,
                                    RadioType.None,
                                    RadioType.None,
                                    false,
                                    radioTraffic.RadioChannelName == this.PrimaryRadioChannel || radioTraffic.RadioChannelName == this.SecondaryRadioChannel,
                                    radioTraffic.RadioChannelName == this.SecondaryRadioChannel
                                )
                            )
                        );
                    }
                }
            }
        }
        public string PrimaryRadioChannel { get; private set; }
        internal List<int> PrimaryRadioChangeHandlerCookies { get; private set; }
        public string SecondaryRadioChannel { get; private set; }
        internal List<int> SecondaryRadioChangeHandlerCookies { get; private set; }
        public List<RadioTraffic> RadioTrafficStates { get; private set; } = new List<RadioTraffic>();
        public List<RadioTrafficState> ActiveRadioTraffic { get; set; } = new List<RadioTrafficState>();
        public bool IsMicClickEnabled { get; set; } = true;
        private bool IsUsingMegaphone { get; set; }

        public bool IsMicrophoneMuted { get; private set; }
        public bool IsMicrophoneEnabled { get; private set; }
        public bool IsSoundMuted { get; private set; }
        public bool IsSoundEnabled { get; private set; }

        public float RadioVolume { get; private set; } = 1.0f;
        public bool IsRadioSpeakerEnabled { get; set; }

        private List<int> _changeHandlerCookies;

        public static PlayerList PlayerList { get; private set; }
        #endregion

        #region Delegates
        public delegate float GetVoiceRangeDelegate();
        public delegate string GetRadioChannelDelegate(bool primary);
        public delegate float GetRadioVolumeDelegate();
        public delegate bool GetBoolDelegate();
        public delegate int GetPluginStateDelegate();
        #endregion

        #region CTOR
        public VoiceManager()
        {
            // NUI Callbacks
            API.RegisterNuiCallbackType(NuiEvent.SaltyChat_OnConnected);
            API.RegisterNuiCallbackType(NuiEvent.SaltyChat_OnDisconnected);
            API.RegisterNuiCallbackType(NuiEvent.SaltyChat_OnError);
            API.RegisterNuiCallbackType(NuiEvent.SaltyChat_OnMessage);
            API.RegisterNuiCallbackType(NuiEvent.SaltyChat_OnNuiReady);

            // Proximity Getter Exports
            GetVoiceRangeDelegate getVoiceRangeDelegate = new GetVoiceRangeDelegate(this.GetVoiceRange);
            this.Exports.Add("GetVoiceRange", getVoiceRangeDelegate);

            // Radio Getter Exports
            GetRadioChannelDelegate getRadioChannelDelegate = new GetRadioChannelDelegate(this.GetRadioChannel);
            this.Exports.Add("GetRadioChannel", getRadioChannelDelegate);

            GetRadioVolumeDelegate getRadioVolumeDelegate = new GetRadioVolumeDelegate(this.GetRadioVolume);
            this.Exports.Add("GetRadioVolume", getRadioVolumeDelegate);

            GetBoolDelegate getRadioSpeakerDelegate = new GetBoolDelegate(this.GetRadioSpeaker);
            this.Exports.Add("GetRadioSpeaker", getRadioSpeakerDelegate);

            GetBoolDelegate getMicClickDelegate = new GetBoolDelegate(this.GetMicClick);
            this.Exports.Add("GetMicClick", getMicClickDelegate);

            // Radio Setter Exports
            this.Exports.Add("SetRadioChannel", new Action<string, bool>(this.SetRadioChannel));
            this.Exports.Add("SetRadioVolume", new Action<float>(this.SetRadioVolume));
            this.Exports.Add("SetRadioSpeaker", new Action<bool>(this.SetRadioSpeaker));
            this.Exports.Add("SetMicClick", new Action<bool>(this.SetMicClick));

            // Misc Exports
            GetPluginStateDelegate getPluginStateDelegate = new GetPluginStateDelegate(this.GetPluginState);
            this.Exports.Add("GetPluginState", getPluginStateDelegate);

            this.Exports.Add("PlaySound", new Action<string, bool, string>(this.PlaySound));

            // StateBag Change Handler
            this._changeHandlerCookies = new List<int>();

            this._changeHandlerCookies.Add(API.AddStateBagChangeHandler(State.SaltyChat_VoiceRange, null, new Action<string, string, dynamic, int, bool>(this.VoiceRangeChangeHandler)));
            this._changeHandlerCookies.Add(API.AddStateBagChangeHandler(State.SaltyChat_IsUsingMegaphone, null, new Action<string, string, dynamic, int, bool>(this.MegaphoneChangeHandler)));

            VoiceManager.PlayerList = this.Players;
        }
        #endregion

        #region Events
        [EventHandler("onClientResourceStop")]
        private void OnResourceStop(string resourceName)
        {
            if (resourceName != API.GetCurrentResourceName())
                return;

            this.IsEnabled = false;
            this.IsConnected = false;

            lock (this._voiceClients)
            {
                this._voiceClients.Clear();
            }

            this.PrimaryRadioChannel = null;
            this.SecondaryRadioChannel = null;

            foreach (int cookie in this._changeHandlerCookies)
                API.RemoveStateBagChangeHandler(cookie);

            this._changeHandlerCookies = null;
        }
        #endregion

        #region Remote Events (Handling)
        [EventHandler(Event.SaltyChat_Initialize)]
        private void OnInitialize(string teamSpeakName, float voiceRange, dynamic towers)
        {
            this.TeamSpeakName = teamSpeakName;
            this.VoiceRange = voiceRange;

            this.OnUpdateRadioTowers(towers);

            this.IsEnabled = true;

            if (this.IsConnected)
                this.InitializePlugin();
            else if (this.IsNuiReady)
                this.ExecuteCommand("connect", this.WebSocketAddress);
            else
                Debug.WriteLine("[Salty Chat] Got server response, but NUI wasn't ready");

            //VoiceManager.DisplayDebug(true);
        }

        [EventHandler(Event.SaltyChat_RemoveClient)]
        private void OnClientRemove(string handle)
        {
            if (!Int32.TryParse(handle, out int serverId))
                return;

            lock (this._voiceClients)
            {
                if (this._voiceClients.TryGetValue(serverId, out VoiceClient client))
                {
                    this.ExecuteCommand(new PluginCommand(Command.RemovePlayer, this.Configuration.ServerUniqueIdentifier, new PlayerState(client.TeamSpeakName)));

                    this._voiceClients.Remove(serverId);
                }
            }
        }
        #endregion

        #region Remote Events (Phone)
        [EventHandler(Event.SaltyChat_EstablishCall)]
        private void OnEstablishCall(string handle, string teamSpeakName, dynamic position)
        {
            this.OnEstablishCallRelayed(handle, teamSpeakName, position, true, new List<dynamic>());
        }

        [EventHandler(Event.SaltyChat_EstablishCallRelayed)]
        private void OnEstablishCallRelayed(string handle, string teamSpeakName, dynamic position, bool direct, List<dynamic> relays)
        {
            if (!Int32.TryParse(handle, out int serverId))
                return;

            if (this.GetOrCreateVoiceClient(serverId, teamSpeakName, out VoiceClient client))
            {
                if (client.DistanceCulled)
                {
                    client.LastPosition = new CitizenFX.Core.Vector3(position[0], position[1], position[2]);
                    client.SendPlayerStateUpdate(this);
                }

                int signalDistortion = 0;

                if (this.Configuration.VariablePhoneDistortion)
                {
                    CitizenFX.Core.Vector3 playerPosition = Game.PlayerPed.Position;
                    CitizenFX.Core.Vector3 remotePlayerPosition = client.LastPosition;

                    signalDistortion = API.GetZoneScumminess(API.GetZoneAtCoords(playerPosition.X, playerPosition.Y, playerPosition.Z)) +
                                        API.GetZoneScumminess(API.GetZoneAtCoords(remotePlayerPosition.X, remotePlayerPosition.Y, remotePlayerPosition.Z));
                }

                this.ExecuteCommand(
                    new PluginCommand(
                        Command.PhoneCommunicationUpdate,
                        this.Configuration.ServerUniqueIdentifier,
                        new PhoneCommunication(
                            client.TeamSpeakName,
                            signalDistortion,
                            direct,
                            relays.Cast<string>().ToArray()
                        )
                    )
                );
            }
        }

        [EventHandler(Event.SaltyChat_EndCall)]
        private void OnEndCall(string handle)
        {
            if (!Int32.TryParse(handle, out int serverId))
                return;

            if (this._voiceClients.TryGetValue(serverId, out VoiceClient client))
            {
                this.ExecuteCommand(
                    new PluginCommand(
                        Command.StopPhoneCommunication,
                        this.Configuration.ServerUniqueIdentifier,
                        new PhoneCommunication(
                            client.TeamSpeakName
                        )
                    )
                );
            }
        }
        #endregion

        #region Remote Events (Radio)
        [EventHandler(Event.SaltyChat_SetRadioChannel)]
        private void OnSetRadioChannel(string radioChannel, bool isPrimary)
        {
            if (isPrimary)
            {
                if (this.PrimaryRadioChangeHandlerCookies != null)
                {
                    foreach (int cookie in this.PrimaryRadioChangeHandlerCookies)
                        API.RemoveStateBagChangeHandler(cookie);

                    this.PrimaryRadioChangeHandlerCookies = null;
                }

                if (String.IsNullOrEmpty(radioChannel))
                {
                    this.RadioChannelSenderChangeHandler("global", $"{State.SaltyChat_RadioChannelSender}:{this.PrimaryRadioChannel}", new List<dynamic>(), 0, false);

                    this.PrimaryRadioChannel = null;

                    this.PlaySound("leaveRadioChannel", false, "radio");

                    this.ExecuteCommand(new PluginCommand(Command.UpdateRadioChannelMembers, this.Configuration.ServerUniqueIdentifier, new RadioChannelMemberUpdate(new string[0], true)));
                }
                else
                {
                    this.PrimaryRadioChannel = radioChannel;

                    this.PrimaryRadioChangeHandlerCookies = new List<int>()
                    {
                        API.AddStateBagChangeHandler($"{State.SaltyChat_RadioChannelMember}:{radioChannel}", "global", new Action<string, string, List<object>, int, bool>(this.RadioChannelMemberChangeHandler)),
                        API.AddStateBagChangeHandler($"{State.SaltyChat_RadioChannelSender}:{radioChannel}", "global", new Action<string, string, List<dynamic>, int, bool>(this.RadioChannelSenderChangeHandler))
                    };

                    this.PlaySound("enterRadioChannel", false, "radio");

                    if (this.GlobalState[$"{State.SaltyChat_RadioChannelSender}:{radioChannel}"] != null)
                        this.RadioChannelSenderChangeHandler("global", $"{State.SaltyChat_RadioChannelSender}:{radioChannel}", this.GlobalState[$"{State.SaltyChat_RadioChannelSender}:{radioChannel}"], 0, false);
                }   
            }
            else
            {
                if (this.SecondaryRadioChangeHandlerCookies != null)
                {
                    foreach (int cookie in this.SecondaryRadioChangeHandlerCookies)
                        API.RemoveStateBagChangeHandler(cookie);

                    this.SecondaryRadioChangeHandlerCookies = null;
                }

                if (String.IsNullOrEmpty(radioChannel))
                {
                    this.RadioChannelSenderChangeHandler("global", $"{State.SaltyChat_RadioChannelSender}:{this.SecondaryRadioChannel}", new List<dynamic>(), 0, false);

                    this.PlaySound("leaveRadioChannel", false, "radio");

                    this.SecondaryRadioChannel = null;

                    this.ExecuteCommand(new PluginCommand(Command.UpdateRadioChannelMembers, this.Configuration.ServerUniqueIdentifier, new RadioChannelMemberUpdate(new string[0], false)));
                }
                else
                {
                    this.SecondaryRadioChannel = radioChannel;

                    this.SecondaryRadioChangeHandlerCookies = new List<int>()
                    {
                        API.AddStateBagChangeHandler($"{State.SaltyChat_RadioChannelMember}:{radioChannel}", "global", new Action<string, string, List<object>, int, bool>(this.RadioChannelMemberChangeHandler)),
                        API.AddStateBagChangeHandler($"{State.SaltyChat_RadioChannelSender}:{radioChannel}", "global", new Action<string, string, List<dynamic>, int, bool>(this.RadioChannelSenderChangeHandler))
                    };

                    this.PlaySound("enterRadioChannel", false, "radio");

                    if (this.GlobalState[$"{State.SaltyChat_RadioChannelSender}:{radioChannel}"] != null)
                        this.RadioChannelSenderChangeHandler("global", $"{State.SaltyChat_RadioChannelSender}:{radioChannel}", this.GlobalState[$"{State.SaltyChat_RadioChannelSender}:{radioChannel}"], 0, false);
                }
            }

            BaseScript.TriggerEvent(Event.SaltyChat_RadioChannelChanged, radioChannel, isPrimary);
        }

        [EventHandler(Event.SaltyChat_ChannelInUse)]
        private void OnChannelBlocked(string channelName)
        {
            this.PlaySound("offMicClick", false, "radio"); // offMicClick is just a placeholder, need to add a new sound to the default sound pack

            if (channelName == this.PrimaryRadioChannel)
                this.OnPrimaryRadioReleased();
            else if (channelName == this.SecondaryRadioChannel)
                this.OnSecondaryRadioReleased();
        }

        [EventHandler(Event.SaltyChat_SetRadioSpeaker)]
        private void OnSetRadioSpeaker(bool isRadioSpeakerEnabled)
        {
            this.IsRadioSpeakerEnabled = isRadioSpeakerEnabled;
        }

        [EventHandler(Event.SaltyChat_UpdateRadioTowers)]
        private void OnUpdateRadioTowers(dynamic towers)
        {
            List<Tower> radioTowers = new List<Tower>();
            
            foreach (dynamic tower in towers)
            {
                if (tower.GetType() == typeof(CitizenFX.Core.Vector3))
                    radioTowers.Add(new Tower(tower.X, tower.Y, tower.Z));
                else if (tower.Count == 3)
                    radioTowers.Add(new Tower(tower[0], tower[1], tower[2]));
                else if (tower.Count == 4)
                    radioTowers.Add(new Tower(tower[0], tower[1], tower[2], tower[3]));
            }

            this.RadioTowers = radioTowers.ToArray();

            this.ExecuteCommand(
                new PluginCommand(
                    Command.RadioTowerUpdate,
                    this.Configuration.ServerUniqueIdentifier,
                    new RadioTower(
                        this.RadioTowers
                    )
                )
            );
        }
        #endregion

        #region Exports (Proximity)
        internal float GetVoiceRange() => this.VoiceRange;
        #endregion

        #region Exports (Radio)
        internal string GetRadioChannel(bool primary) => primary ? this.PrimaryRadioChannel : this.SecondaryRadioChannel;

        internal float GetRadioVolume() => this.RadioVolume;

        internal bool GetRadioSpeaker() => this.IsRadioSpeakerEnabled;

        internal bool GetMicClick() => this.IsMicClickEnabled;

        internal void SetRadioChannel(string radioChannelName, bool primary)
        {
            if ((primary && this.PrimaryRadioChannel == radioChannelName) ||
                (!primary && this.SecondaryRadioChannel == radioChannelName))
                return;

            BaseScript.TriggerServerEvent(Event.SaltyChat_SetRadioChannel, radioChannelName, primary);
        }

        internal void SetRadioVolume(float volumeLevel)
        {
            if (volumeLevel < 0f)
                this.RadioVolume = 0f;
            else if (volumeLevel > 1.6f)
                this.RadioVolume = 1.6f;
            else
                this.RadioVolume = volumeLevel;
        }

        internal void SetRadioSpeaker(bool isRadioSpeakerEnabled)
        {
            BaseScript.TriggerServerEvent(Event.SaltyChat_SetRadioSpeaker, isRadioSpeakerEnabled);
        }

        internal void SetMicClick(bool isMicClickEnabled)
        {
            this.IsMicClickEnabled = isMicClickEnabled;
        }
        #endregion

        #region Exports (Misc)
        internal int GetPluginState() => (int)this.PlguinState;
        #endregion

        #region NUI Events
        [EventHandler("__cfx_nui:" + NuiEvent.SaltyChat_OnNuiReady)]
        private void OnNuiReady(dynamic dummy, dynamic cb)
        {
            this.IsNuiReady = true;

            if (this.IsEnabled && this.TeamSpeakName != null && !this.IsConnected)
            {
                Debug.WriteLine("[Salty Chat] NUI is now ready, connecting...");

                this.ExecuteCommand("connect", this.WebSocketAddress);
            }

            cb("");
        }

        [EventHandler("__cfx_nui:" + NuiEvent.SaltyChat_OnConnected)]
        private void OnConnected(dynamic dummy, dynamic cb)
        {
            this.IsConnected = true;

            if (this.IsEnabled)
                this.InitializePlugin();

            cb("");
        }

        [EventHandler("__cfx_nui:" + NuiEvent.SaltyChat_OnDisconnected)]
        private void OnDisconnected(dynamic dummy, dynamic cb)
        {
            this.IsConnected = false;
            this.PlguinState = GameInstanceState.NotInitiated;

            cb("");
        }

        [EventHandler("__cfx_nui:" + NuiEvent.SaltyChat_OnMessage)]
        private void OnMessage(dynamic message, dynamic cb)
        {
            cb("");

            PluginCommand pluginCommand = PluginCommand.Deserialize(message);

            if (pluginCommand.ServerUniqueIdentifier != this.Configuration.ServerUniqueIdentifier)
                return;

            switch (pluginCommand.Command)
            {
                case Command.PluginState:
                    {
                        if (pluginCommand.TryGetPayload(out PluginState pluginState))
                        {
                            BaseScript.TriggerServerEvent(Event.SaltyChat_CheckVersion, pluginState.Version);

                            // Sync radio related states after instance init
                            this.ExecuteCommand(
                                new PluginCommand(
                                    Command.RadioTowerUpdate,
                                    this.Configuration.ServerUniqueIdentifier,
                                    new RadioTower(this.RadioTowers)
                                )
                            );

                            if (!String.IsNullOrWhiteSpace(this.PrimaryRadioChannel))
                            {
                                this.RadioChannelMemberChangeHandler("global", $"{State.SaltyChat_RadioChannelMember}:{this.PrimaryRadioChannel}", this.GlobalState[$"{State.SaltyChat_RadioChannelMember}:{this.PrimaryRadioChannel}"], 0, false);
                                this.RadioChannelSenderChangeHandler("global", $"{State.SaltyChat_RadioChannelSender}:{this.PrimaryRadioChannel}", this.GlobalState[$"{State.SaltyChat_RadioChannelSender}:{this.PrimaryRadioChannel}"], 0, false);
                            }

                            if (!String.IsNullOrWhiteSpace(this.SecondaryRadioChannel))
                            {
                                this.RadioChannelMemberChangeHandler("global", $"{State.SaltyChat_RadioChannelMember}:{this.SecondaryRadioChannel}", this.GlobalState[$"{State.SaltyChat_RadioChannelMember}:{this.SecondaryRadioChannel}"], 0, false);
                                this.RadioChannelSenderChangeHandler("global", $"{State.SaltyChat_RadioChannelSender}:{this.SecondaryRadioChannel}", this.GlobalState[$"{State.SaltyChat_RadioChannelSender}:{this.SecondaryRadioChannel}"], 0, false);
                            }
                        }

                        break;
                    }
                case Command.Reset:
                    {
                        this.PlguinState = GameInstanceState.NotInitiated;

                        this.InitializePlugin();

                        break;
                    }
                case Command.Ping:
                    {
                        if (this.PlguinState != GameInstanceState.NotInitiated)
                            this.ExecuteCommand(new PluginCommand(this.Configuration.ServerUniqueIdentifier));

                        break;
                    }
                case Command.InstanceState:
                    {
                        if (pluginCommand.TryGetPayload(out InstanceState instanceState))
                            this.PlguinState = instanceState.State;

                        break;
                    }
                case Command.SoundState:
                    {
                        if (pluginCommand.TryGetPayload(out SoundState soundState))
                        {
                            if (soundState.IsMicrophoneMuted != this.IsMicrophoneMuted)
                            {
                                this.IsMicrophoneMuted = soundState.IsMicrophoneMuted;

                                BaseScript.TriggerEvent(Event.SaltyChat_MicStateChanged, this.IsMicrophoneMuted);
                            }

                            if (soundState.IsMicrophoneEnabled != this.IsMicrophoneEnabled)
                            {
                                this.IsMicrophoneEnabled = soundState.IsMicrophoneEnabled;

                                BaseScript.TriggerEvent(Event.SaltyChat_MicEnabledChanged, this.IsMicrophoneEnabled);
                            }

                            if (soundState.IsSoundMuted != this.IsSoundMuted)
                            {
                                this.IsSoundMuted = soundState.IsSoundMuted;

                                BaseScript.TriggerEvent(Event.SaltyChat_SoundStateChanged, this.IsSoundMuted);
                            }

                            if (soundState.IsSoundEnabled != this.IsSoundEnabled)
                            {
                                this.IsSoundEnabled = soundState.IsSoundEnabled;

                                BaseScript.TriggerEvent(Event.SaltyChat_SoundEnabledChanged, this.IsSoundEnabled);
                            }
                        }

                        break;
                    }
                case Command.TalkState:
                    {
                        if (pluginCommand.TryGetPayload(out TalkState talkState))
                            this.SetPlayerTalking(talkState.Name, talkState.IsTalking);

                        break;
                    }
                case Command.RadioTrafficState:
                    {
                        if (pluginCommand.TryGetPayload(out RadioTrafficState radioTrafficState))
                        {
                            lock (this.ActiveRadioTraffic)
                            {
                                RadioTrafficState activeRadioTrafficState = this.ActiveRadioTraffic.FirstOrDefault(r => r.Name == radioTrafficState.Name && r.IsPrimaryChannel == radioTrafficState.IsPrimaryChannel);

                                if (radioTrafficState.IsSending)
                                {
                                    if (activeRadioTrafficState == null)
                                        this.ActiveRadioTraffic.Add(radioTrafficState);
                                    else if (activeRadioTrafficState != null && activeRadioTrafficState.ActiveRelay != radioTrafficState.ActiveRelay)
                                        activeRadioTrafficState.ActiveRelay = radioTrafficState.ActiveRelay;
                                }
                                else
                                {
                                    if (activeRadioTrafficState != null)
                                        this.ActiveRadioTraffic.Remove(activeRadioTrafficState);
                                }
                            }

                            BaseScript.TriggerEvent(Event.SaltyChat_RadioTrafficStateChanged,
                                this.ActiveRadioTraffic.Any(r => r.IsPrimaryChannel && r.IsSending && r.ActiveRelay == null && r.Name != this.TeamSpeakName),   // Primary RX
                                this.ActiveRadioTraffic.Any(r => r.Name == this.TeamSpeakName && r.IsPrimaryChannel && r.IsSending),                            // Primary TX
                                this.ActiveRadioTraffic.Any(r => !r.IsPrimaryChannel && r.IsSending && r.ActiveRelay == null && r.Name != this.TeamSpeakName),  // Secondary RX
                                this.ActiveRadioTraffic.Any(r => r.Name == this.TeamSpeakName && !r.IsPrimaryChannel && r.IsSending)                            // Secondary TX
                            );
                        }

                        break;
                    }
            }
        }

        [EventHandler("__cfx_nui:" + NuiEvent.SaltyChat_OnError)]
        private async void OnError(dynamic message, dynamic cb)
        {
            cb("");

            try
            {
                PluginError pluginError = PluginError.Deserialize(message);

                switch (pluginError.Error)
                {
                    case Error.AlreadyInGame:
                        {
                            Debug.WriteLine($"[Salty Chat] Error: Seems like we are already in an instance, retry in 5 seconds...");

                            await BaseScript.Delay(5 * 1000);

                            this.InitializePlugin();

                            break;
                        }
                    default:
                        {
                            Debug.WriteLine($"[Salty Chat] Error: {pluginError.Error} - Message: {pluginError.Message}");

                            break;
                        }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"[Salty Chat] Error: We received an error, but couldn't deserialize it:{Environment.NewLine}{e}");
            }
        }
        #endregion

        #region StateBag Change Handler
        private void VoiceRangeChangeHandler(string bagName, string key, dynamic value, int reserved, bool replicated)
        {
            if (replicated || !bagName.StartsWith("player:"))
                return;

            int serverId = Int32.Parse(bagName.Split(':').Last());

            if (serverId == Game.Player.ServerId)
            {
                if (this.VoiceRange != value)
                    this.VoiceRange = value;

                return;
            }

            VoiceClient voiceClient = this.VoiceClients.FirstOrDefault(c => c.ServerId == serverId);

            if (voiceClient == null)
                return;

            voiceClient.VoiceRange = value;
        }

        private void MegaphoneChangeHandler(string bagName, string key, dynamic value, int reserved, bool replicated)
        {
            if (!bagName.StartsWith("player:"))
                return;

            int serverId = Int32.Parse(bagName.Split(':').Last());
            bool isUsingMegaphone = value == true;
            string teamSpeakName;

            if (serverId == Game.Player.ServerId)
            {
                if (replicated || value == null)
                    return;

                if (!isUsingMegaphone)
                    Game.Player.State.Set(State.SaltyChat_IsUsingMegaphone, null, true);

                teamSpeakName = this.TeamSpeakName;
            }
            else
            {
                VoiceClient voiceClient = this.VoiceClients.FirstOrDefault(c => c.ServerId == serverId);

                if (voiceClient == null || voiceClient.IsUsingMegaphone == isUsingMegaphone)
                    return;

                teamSpeakName = voiceClient.TeamSpeakName;
                voiceClient.IsUsingMegaphone = isUsingMegaphone;
            }

            this.ExecuteCommand(
                new PluginCommand(
                    isUsingMegaphone ? Command.MegaphoneCommunicationUpdate : Command.StopMegaphoneCommunication,
                    this.Configuration.ServerUniqueIdentifier,
                    new MegaphoneCommunication(
                        teamSpeakName,
                        this.Configuration.MegaphoneRange
                    )
                )
            );
        }

        private void RadioChannelMemberChangeHandler(string bagName, string key, List<object> value, int reserved, bool replicated)
        {
            string channelName = key.Split(':').Last();

            if (value == null)
                return;

            this.ExecuteCommand(new PluginCommand(Command.UpdateRadioChannelMembers, this.Configuration.ServerUniqueIdentifier, new RadioChannelMemberUpdate(value.Select(m => (string)m).ToArray(), channelName == this.PrimaryRadioChannel)));
        }

        private void RadioChannelSenderChangeHandler(string bagName, string key, List<dynamic> value, int reserved, bool replicated)
        {
            string channelName = key.Split(':').Last();

            if (value == null)
                return;

            foreach (dynamic sender in value)
            {
                int serverId = sender.ServerId;
                string teamSpeakName = sender.Name;
                Vector3 position = (Vector3)sender.Position;
                bool stateChanged = false;

                lock (this.RadioTrafficStates)
                {
                    RadioTraffic radioTraffic = this.RadioTrafficStates.FirstOrDefault(r => r.Name == teamSpeakName && r.RadioChannelName == channelName);

                    if (radioTraffic == null)
                    {
                        this.RadioTrafficStates.Add(new RadioTraffic(teamSpeakName, true, channelName, (RadioType)this.Configuration.RadioType, (RadioType)this.Configuration.RadioType, new string[0]));
                        stateChanged = true;
                    }
                }

                if (serverId == Game.Player.ServerId)
                {
                    if (stateChanged)
                    {
                        this.ExecuteCommand(
                            new PluginCommand(
                                Command.RadioCommunicationUpdate,
                                this.Configuration.ServerUniqueIdentifier,
                                new RadioCommunication(
                                    this.TeamSpeakName,
                                    (RadioType)this.Configuration.RadioType,
                                    (RadioType)this.Configuration.RadioType,
                                    this.IsMicClickEnabled && stateChanged,
                                    true,
                                    this.SecondaryRadioChannel == channelName,
                                    new string[0],
                                    this.RadioVolume
                                )
                            )
                        );
                    }
                }
                else if (this.GetOrCreateVoiceClient(serverId, teamSpeakName, out VoiceClient client))
                {
                    if (client.DistanceCulled)
                    {
                        client.LastPosition = position;
                        client.SendPlayerStateUpdate(this);
                    }

                    if (stateChanged && this.CanReceiveRadioTraffic)
                    {
                        this.ExecuteCommand(
                            new PluginCommand(
                                Command.RadioCommunicationUpdate,
                                this.Configuration.ServerUniqueIdentifier,
                                new RadioCommunication(
                                    client.TeamSpeakName,
                                    (RadioType)this.Configuration.RadioType,
                                    (RadioType)this.Configuration.RadioType,
                                    this.IsMicClickEnabled && stateChanged,
                                    true,
                                    this.SecondaryRadioChannel == channelName,
                                    this.IsRadioSpeakerEnabled ? new string[] { this.TeamSpeakName } : new string[0],
                                    this.RadioVolume
                                )
                            )
                        );
                    }
                }
            }

            lock (this.RadioTrafficStates)
            {
                foreach (RadioTraffic traffic in this.RadioTrafficStates.Where(r => r.RadioChannelName == channelName && !value.Any(s => s.Name == r.Name)).ToArray())
                {
                    this.ExecuteCommand(
                        new PluginCommand(
                            Command.StopRadioCommunication,
                            this.Configuration.ServerUniqueIdentifier,
                            new RadioCommunication(
                                traffic.Name,
                                (RadioType)this.Configuration.RadioType,
                                (RadioType)this.Configuration.RadioType,
                                this.IsMicClickEnabled,
                                true,
                                this.SecondaryRadioChannel == channelName
                            )
                        )
                    );

                    this.RadioTrafficStates.Remove(traffic);
                }
            }
        }
        #endregion

        #region Keybindings
        private void OnVoiceRangePressed()
        {
            if (!this.IsEnabled)
                return;

            this.ToggleVoiceRange();
        }

        private void OnVoiceRangeReleased()
        {
            // Empty dummy, so /-voiceRange isn't spammed in the chat
        }

        private void OnPrimaryRadioPressed()
        {
            Ped playerPed = Game.PlayerPed;

            if (!this.IsEnabled || !this.IsAlive || String.IsNullOrWhiteSpace(this.PrimaryRadioChannel) || !this.CanSendRadioTraffic)
                return;

            BaseScript.TriggerServerEvent(Event.SaltyChat_IsSending, this.PrimaryRadioChannel, true);
            Game.PlayerPed.Task.PlayAnimation("random@arrests", "generic_radio_enter", -2f, -1, (AnimationFlags)50);
        }

        private void OnPrimaryRadioReleased()
        {
            if (!this.IsEnabled || !this.IsAlive || String.IsNullOrWhiteSpace(this.PrimaryRadioChannel))
                return;

            BaseScript.TriggerServerEvent(Event.SaltyChat_IsSending, this.PrimaryRadioChannel, false);
            Game.PlayerPed.Task.ClearAnimation("random@arrests", "generic_radio_enter");
        }

        private void OnSecondaryRadioPressed()
        {
            Ped playerPed = Game.PlayerPed;

            if (!this.IsEnabled || !this.IsAlive || String.IsNullOrWhiteSpace(this.SecondaryRadioChannel) || !this.CanSendRadioTraffic)
                return;

            BaseScript.TriggerServerEvent(Event.SaltyChat_IsSending, this.SecondaryRadioChannel, true);
            Game.PlayerPed.Task.PlayAnimation("random@arrests", "generic_radio_enter", -2f, -1, (AnimationFlags)50);
        }

        private void OnSecondaryRadioReleased()
        {
            if (!this.IsEnabled || !this.IsAlive || String.IsNullOrWhiteSpace(this.SecondaryRadioChannel))
                return;

            BaseScript.TriggerServerEvent(Event.SaltyChat_IsSending, this.SecondaryRadioChannel, false);
            Game.PlayerPed.Task.ClearAnimation("random@arrests", "generic_radio_enter");
        }

        private void OnMegaphonePressed()
        {
            Ped playerPed = Game.PlayerPed;

            if (!this.IsEnabled || !this.IsAlive || !playerPed.IsInPoliceVehicle)
                return;

            Vehicle vehicle = playerPed.CurrentVehicle;

            if (vehicle.GetPedOnSeat(VehicleSeat.Driver) == playerPed || vehicle.GetPedOnSeat(VehicleSeat.Passenger) == playerPed)
            {
                Game.Player.State.Set(State.SaltyChat_IsUsingMegaphone, true, true);
                this.IsUsingMegaphone = true;
            }
        }

        private void OnMegaphoneReleased()
        {
            if (!this.IsEnabled || !this.IsUsingMegaphone)
                return;

            Game.Player.State.Set(State.SaltyChat_IsUsingMegaphone, false, true);
            this.IsUsingMegaphone = false;
        }
        #endregion

        #region Tick
        [Tick]
        private async Task FirstTick()
        {
            this.Configuration = JsonConvert.DeserializeObject<Configuration>(API.LoadResourceFile(API.GetCurrentResourceName(), "config.json"));

            // Register commands and key mappings
            API.RegisterCommand("+voiceRange", new Action(this.OnVoiceRangePressed), false);
            API.RegisterCommand("-voiceRange", new Action(this.OnVoiceRangeReleased), false);
            API.RegisterKeyMapping("+voiceRange", "Toggle Voice Range", "keyboard", this.Configuration.ToggleRange);

            API.RegisterCommand("+primaryRadio", new Action(this.OnPrimaryRadioPressed), false);
            API.RegisterCommand("-primaryRadio", new Action(this.OnPrimaryRadioReleased), false);
            API.RegisterKeyMapping("+primaryRadio", "Use Primary Radio", "keyboard", this.Configuration.TalkPrimary);

            API.RegisterCommand("+secondaryRadio", new Action(this.OnSecondaryRadioPressed), false);
            API.RegisterCommand("-secondaryRadio", new Action(this.OnSecondaryRadioReleased), false);
            API.RegisterKeyMapping("+secondaryRadio", "Use Secondary Radio", "keyboard", this.Configuration.TalkSecondary);

            API.RegisterCommand("+megaphone", new Action(this.OnMegaphonePressed), false);
            API.RegisterCommand("-megaphone", new Action(this.OnMegaphoneReleased), false);
            API.RegisterKeyMapping("+megaphone", "Use Megaphone", "keyboard", this.Configuration.TalkMegaphone);

            BaseScript.TriggerServerEvent(Event.SaltyChat_Initialize);

            this.Tick -= this.FirstTick;

            if (this.Configuration.VoiceEnabled)
            {
                this.Tick += this.OnControlTick;
                this.Tick += this.OnStateUpdateTick;
            }

            await Task.FromResult(0);
        }

        private async Task OnControlTick()
        {
            Game.DisableControlThisFrame(0, Control.PushToTalk);

            if (this.IsUsingMegaphone && (!Game.PlayerPed.IsInPoliceVehicle || !this.IsAlive))
            {
                this.OnMegaphoneReleased();
            }

            await Task.FromResult(0);
        }

        private async Task OnStateUpdateTick()
        {
            Player localPlayer = Game.Player;
            Ped playerPed = localPlayer.Character;

            if (this.IsConnected && this.PlguinState == GameInstanceState.Ingame)
            {
                CitizenFX.Core.Vector3 playerPosition = playerPed.Position;
                int playerRoomId = API.GetRoomKeyFromEntity(playerPed.Handle);
                Vehicle playerVehicle = playerPed.CurrentVehicle;
                bool hasPlayerVehicleOpening = playerVehicle == null || playerVehicle.HasOpening();

                List<PlayerState> playerStates = new List<PlayerState>();
                List<int> updatedPlayers = new List<int>();

                foreach (Player nPlayer in this.Players)
                {
                    if (nPlayer == localPlayer || !this.GetOrCreateVoiceClient(nPlayer, out VoiceClient voiceClient))
                        continue;

                    Ped nPed = nPlayer.Character;

                    if (this.Configuration.IgnoreInvisiblePlayers && !nPed.IsVisible)
                        continue;

                    voiceClient.LastPosition = nPed.Position;
                    int? muffleIntensity = null;

                    if (voiceClient.IsAlive)
                    {
                        int nPlayerRoomId = API.GetRoomKeyFromEntity(nPed.Handle);

                        if (nPlayerRoomId != playerRoomId && !API.HasEntityClearLosToEntity(playerPed.Handle, nPed.Handle, 17))
                        {
                            muffleIntensity = 10;
                        }
                        else
                        {
                            Vehicle nPlayerVehicle = nPed.CurrentVehicle;

                            if (playerVehicle != nPlayerVehicle)
                            {
                                bool hasNPlayerVehicleOpening = nPlayerVehicle == null || nPlayerVehicle.HasOpening();

                                if (!hasPlayerVehicleOpening && !hasNPlayerVehicleOpening)
                                    muffleIntensity = 10;
                                else if (!hasPlayerVehicleOpening || !hasNPlayerVehicleOpening)
                                    muffleIntensity = 6;
                            }
                        }
                    }

                    if (voiceClient.DistanceCulled)
                        voiceClient.DistanceCulled = false;

                    playerStates.Add(
                        new PlayerState(
                            voiceClient.TeamSpeakName,
                            voiceClient.LastPosition,
                            voiceClient.VoiceRange,
                            voiceClient.IsAlive,
                            voiceClient.DistanceCulled,
                            muffleIntensity
                        )
                    );

                    updatedPlayers.Add(voiceClient.ServerId);
                }

                foreach (VoiceClient culledVoiceClient in this.VoiceClients.Where(c => !c.DistanceCulled && !updatedPlayers.Contains(c.ServerId)))
                {
                    culledVoiceClient.DistanceCulled = true;

                    playerStates.Add(
                        new PlayerState(
                            culledVoiceClient.TeamSpeakName,
                            culledVoiceClient.LastPosition,
                            culledVoiceClient.VoiceRange,
                            culledVoiceClient.IsAlive,
                            culledVoiceClient.DistanceCulled
                        )
                    );
                }

                this.ExecuteCommand(
                    new PluginCommand(
                        Command.BulkUpdate,
                        this.Configuration.ServerUniqueIdentifier,
                        new BulkUpdate(
                            playerStates,
                            new SelfState(
                                playerPosition,
                                API.GetGameplayCamRot(0).Z,
                                this.VoiceRange,
                                this.IsAlive
                            )
                        )
                    )
                );
            }

            if (this.IsAlive)
            {
                bool isUnderWater = playerPed.IsSwimmingUnderWater;
                bool isSwimming = isUnderWater || playerPed.IsSwimming;

                if (isUnderWater)
                {
                    this.CanSendRadioTraffic = false;
                    this.CanReceiveRadioTraffic = false;
                }
                else if (isSwimming && API.GetEntitySpeed(playerPed.Handle) <= 2f)
                {
                    this.CanSendRadioTraffic = true;
                    this.CanReceiveRadioTraffic = true;
                }
                else if (isSwimming)
                {
                    this.CanSendRadioTraffic = false;
                    this.CanReceiveRadioTraffic = true;
                }
                else
                {
                    this.CanSendRadioTraffic = true;
                    this.CanReceiveRadioTraffic = true;
                }
            }
            else
            {
                this.CanSendRadioTraffic = false;
                this.CanReceiveRadioTraffic = false;
            }

            await BaseScript.Delay(280);
        }
        #endregion

        #region Methods (Proximity)
        private void SetPlayerTalking(string teamSpeakName, bool isTalking)
        {
            if (teamSpeakName == this.TeamSpeakName)
            {
                BaseScript.TriggerEvent(Event.SaltyChat_TalkStateChanged, isTalking);

                API.SetPlayerTalkingOverride(Game.Player.Handle, isTalking);

                // Lip sync workaround for OneSync
                if (isTalking)
                    API.PlayFacialAnim(Game.PlayerPed.Handle, "mic_chatter", "mp_facial");
                else
                    API.PlayFacialAnim(Game.PlayerPed.Handle, "mood_normal_1", "facials@gen_male@variations@normal");
            }
            else
            {
                VoiceClient voiceClient = this.VoiceClients.FirstOrDefault(v => v.TeamSpeakName == teamSpeakName);

                if (voiceClient != null && voiceClient.Player != null)
                    API.SetPlayerTalkingOverride(voiceClient.Player.Handle, isTalking);
            }
        }

        /// <summary>
        /// Toggles voice range through <see cref="Voice.VoiceRanges"/>
        /// </summary>
        public void ToggleVoiceRange()
        {
            int index = Array.IndexOf(this.Configuration.VoiceRanges, this.VoiceRange);

            if (index < 0)
            {
                index = 1;
                this.VoiceRange = this.Configuration.VoiceRanges[index];
            }
            else if (index + 1 >= this.Configuration.VoiceRanges.Length)
            {
                index = 0;
                this.VoiceRange = this.Configuration.VoiceRanges[index];
            }
            else
            {
                index++;
                this.VoiceRange = this.Configuration.VoiceRanges[index];
            }

            Game.Player.State.Set(State.SaltyChat_VoiceRange, this.VoiceRange, true);

            if (this.Configuration.EnableVoiceRangeNotification)
            {
                if (this.RangeNotification != null)
                    this.RangeNotification.Hide();

                this.RangeNotification = CitizenFX.Core.UI.Screen.ShowNotification(this.Configuration.VoiceRangeNotification.Replace("{voicerange}", this.VoiceRange.ToString()));
            }
        }
        #endregion

        #region Methods (Plugin)
        private void InitializePlugin()
        {
            if (this.PlguinState != GameInstanceState.NotInitiated)
                return;

            this.ExecuteCommand(
                new PluginCommand(
                    Command.Initiate,
                    new GameInstance(
                        this.Configuration.ServerUniqueIdentifier,
                        this.TeamSpeakName,
                        this.Configuration.IngameChannelId,
                        this.Configuration.IngameChannelPassword,
                        this.Configuration.SoundPack,
                        this.Configuration.SwissChannelIds,
                        this.Configuration.RequestTalkStates,
                        this.Configuration.RequestRadioTrafficStates,
                        this.Configuration.UltraShortRangeDistance,
                        this.Configuration.ShortRangeDistance,
                        this.Configuration.LongRangeDistace
                    )
                )
            );
        }

        /// <summary>
        /// Plays a file from soundpack specified in <see cref="VoiceManager.SoundPack"/>
        /// </summary>
        /// <param name="fileName">filename (without .wav) of the soundfile</param>
        /// <param name="loop">use <see cref="true"/> to let the plugin loop the sound</param>
        /// <param name="handle">use your own handle instead of the filename, so you can play the sound multiple times</param>
        public void PlaySound(string fileName, bool loop = false, string handle = null)
        {
            if (String.IsNullOrWhiteSpace(handle))
                handle = fileName;

            this.ExecuteCommand(
                new PluginCommand(
                    Command.PlaySound,
                    this.Configuration.ServerUniqueIdentifier,
                    new Sound(
                        fileName,
                        loop,
                        handle
                    )
                )
            );
        }

        /// <summary>
        /// Stops and dispose the sound
        /// </summary>
        /// <param name="handle">filename or handle of the sound</param>
        public void StopSound(string handle)
        {
            this.ExecuteCommand(
                new PluginCommand(
                    Command.StopSound,
                    this.Configuration.ServerUniqueIdentifier,
                    new Sound(handle)
                )
            );
        }

        private void ExecuteCommand(string funtion, object parameters)
        {
            API.SendNuiMessage(
                JsonConvert.SerializeObject(new { Function = funtion, Params = parameters })
            );
        }

        internal void ExecuteCommand(PluginCommand pluginCommand)
        {
            this.ExecuteCommand("runCommand", JsonConvert.SerializeObject(pluginCommand));
        }

        private void DisplayDebug(bool show)
        {
            this.ExecuteCommand("showBody", show);
        }
        #endregion

        #region Methods (Helper)
        public bool GetOrCreateVoiceClient(Player player, out VoiceClient voiceClient)
        {
            if (player == null)
            {
                voiceClient = null;
                return false;
            }

            lock (this._voiceClients)
            {
                if (this._voiceClients.TryGetValue(player.ServerId, out voiceClient))
                {
                    voiceClient.VoiceRange = player.GetVoiceRange();
                    voiceClient.IsAlive = player.GetIsAlive();
                }
                else
                {
                    string tsName = player.GetTeamSpeakName();

                    if (tsName == null)
                        return false;

                    voiceClient = new VoiceClient(player.ServerId, tsName, player.GetVoiceRange(), player.GetIsAlive());

                    this._voiceClients.Add(voiceClient.ServerId, voiceClient);
                }
            }

            return voiceClient != null;
        }

        public bool GetOrCreateVoiceClient(int serverId, string teamSpeakName, out VoiceClient voiceClient)
        {
            Player player = this.Players[serverId];

            lock (this._voiceClients)
            {
                if (this._voiceClients.TryGetValue(serverId, out voiceClient))
                {
                    if (player != null)
                    {
                        voiceClient.VoiceRange = player.GetVoiceRange();
                        voiceClient.IsAlive = player.GetIsAlive();
                    }
                }
                else
                {
                    if (player != null)
                    {
                        string tsName = player.GetTeamSpeakName();

                        if (tsName == null)
                            return false;

                        voiceClient = new VoiceClient(player.ServerId, tsName, player.GetVoiceRange(), player.GetIsAlive());
                    }
                    else
                    {
                        voiceClient = new VoiceClient(serverId, teamSpeakName, 0f, true) { DistanceCulled = true };
                    }

                    this._voiceClients.Add(voiceClient.ServerId, voiceClient);
                }
            }

            return voiceClient != null;
        }
        #endregion
    }
}
