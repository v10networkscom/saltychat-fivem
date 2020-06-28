using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using SaltyShared;

namespace SaltyClient
{
    internal class VoiceManager : BaseScript
    {
        #region Properties / Fields
        public bool IsEnabled { get; private set; }
        public bool IsConnected { get; private set; }
        public bool IsIngame { get; private set; }

        public string TeamSpeakName { get; private set; }
        public string ServerUniqueIdentifier { get; private set; }
        public string SoundPack { get; private set; }
        public ulong IngameChannel { get; private set; }
        public string IngameChannelPassword { get; private set; }
        public ulong[] SwissChannelIds { get; private set; }

        public VoiceClient[] VoiceClients => this._voiceClients.Values.ToArray();
        private Dictionary<int, VoiceClient> _voiceClients = new Dictionary<int, VoiceClient>();

        public Vector3[] RadioTowers { get; private set; }

        public float VoiceRange { get; private set; } = SharedData.VoiceRanges[1];
        public string PrimaryRadioChannel { get; private set; }
        public string SecondaryRadioChannel { get; private set; }
        private bool IsUsingMegaphone { get; set; }

        public bool IsMicrophoneMuted { get; private set; }
        public bool IsMicrophoneEnabled { get; private set; }
        public bool IsSoundMuted { get; private set; }
        public bool IsSoundEnabled { get; private set; }

        public static PlayerList PlayerList { get; private set; }
        #endregion

        #region Delegates
        public delegate string GetRadioChannelDelegate(bool primary);
        #endregion

        #region CTOR
        public VoiceManager()
        {
            API.RegisterNuiCallbackType(NuiEvent.SaltyChat_OnConnected);
            API.RegisterNuiCallbackType(NuiEvent.SaltyChat_OnDisconnected);
            API.RegisterNuiCallbackType(NuiEvent.SaltyChat_OnError);
            API.RegisterNuiCallbackType(NuiEvent.SaltyChat_OnMessage);

            GetRadioChannelDelegate getRadioChannelDelegate = new GetRadioChannelDelegate(this.GetRadioChannel);
            this.Exports.Add("GetRadioChannel", getRadioChannelDelegate);
            this.Exports.Add("SetRadioChannel", new Action<string, bool>(this.SetRadioChannel));

            VoiceManager.PlayerList = this.Players;
        }
        #endregion

        #region Events
        [EventHandler("onClientResourceStart")]
        private void OnResourceStart(string resourceName)
        {
            if (resourceName != API.GetCurrentResourceName())
                return;

            this.ServerUniqueIdentifier = API.GetResourceMetadata(resourceName, "ServerUniqueIdentifier", 0);
            this.SoundPack = API.GetResourceMetadata(resourceName, "SoundPack", 0);
            this.IngameChannel = UInt64.Parse(API.GetResourceMetadata(resourceName, "IngameChannelId", 0));
            this.IngameChannelPassword = API.GetResourceMetadata(resourceName, "IngameChannelPassword", 0);

            string swissChannelIds = API.GetResourceMetadata(resourceName, "SwissChannelIds", 0);

            if (!String.IsNullOrEmpty(swissChannelIds))
            {
                this.SwissChannelIds = swissChannelIds.Split(',').Select(s => UInt64.Parse(s.Trim())).ToArray();
            }

            BaseScript.TriggerServerEvent(Event.SaltyChat_Initialize);
        }

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
        }
        #endregion

        #region Remote Events (Handling)
        [EventHandler(Event.SaltyChat_Initialize)]
        private void OnInitialize(string teamSpeakName, dynamic towers)
        {
            this.TeamSpeakName = teamSpeakName;

            List<Vector3> towerPositions = new List<Vector3>();

            foreach (dynamic tower in towers)
            {
                towerPositions.Add(new Vector3(tower[0], tower[1], tower[2]));
            }

            this.RadioTowers = towerPositions.ToArray();

            this.IsEnabled = true;

            if (this.IsConnected)
                this.InitializePlugin();
            else
                this.ExecuteCommand("connect", "127.0.0.1:38088");

            //VoiceManager.DisplayDebug(true);
        }

        [EventHandler(Event.SaltyChat_SyncClients)]
        private void OnClientSync(string json)
        {
            try
            {
                SaltyShared.VoiceClient[] voiceClients = Newtonsoft.Json.JsonConvert.DeserializeObject<SaltyShared.VoiceClient[]>(json);

                lock (this._voiceClients)
                {
                    this._voiceClients.Clear();

                    foreach (SaltyShared.VoiceClient sharedVoiceClient in voiceClients)
                    {
                        VoiceClient voiceClient = new VoiceClient(
                            sharedVoiceClient.PlayerId,
                            sharedVoiceClient.TeamSpeakName,
                            sharedVoiceClient.VoiceRange,
                            sharedVoiceClient.IsAlive,
                            new CitizenFX.Core.Vector3(
                                sharedVoiceClient.Position.X,
                                sharedVoiceClient.Position.Y,
                                sharedVoiceClient.Position.Z
                            )
                        );

                        this._voiceClients.Add(sharedVoiceClient.PlayerId, voiceClient);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SaltyChat_SyncClients: Error while parsing voice clients{Environment.NewLine}{ex.ToString()}");
            }
        }

        [EventHandler(Event.SaltyChat_UpdateClient)]
        private void OnClientUpdate(string json)
        {
            try
            {
                SaltyShared.VoiceClient sharedVoiceClient = Newtonsoft.Json.JsonConvert.DeserializeObject<SaltyShared.VoiceClient>(json);

                VoiceClient voiceClient = new VoiceClient(
                    sharedVoiceClient.PlayerId,
                    sharedVoiceClient.TeamSpeakName,
                    sharedVoiceClient.VoiceRange,
                    sharedVoiceClient.IsAlive,
                    new CitizenFX.Core.Vector3(
                        sharedVoiceClient.Position.X,
                        sharedVoiceClient.Position.Y,
                        sharedVoiceClient.Position.Z
                    )
                );

                lock (this._voiceClients)
                {
                    if (this._voiceClients.ContainsKey(voiceClient.ServerId))
                    {
                        this._voiceClients[voiceClient.ServerId] = voiceClient;
                    }
                    else
                    {
                        this._voiceClients.Add(voiceClient.ServerId, voiceClient);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SaltyChat_UpdateClient: Error while parsing voice client{Environment.NewLine}{ex.ToString()}");
            }
        }

        [EventHandler(Event.SaltyChat_UpdateVoiceRange)]
        private void OnClientUpdateVoiceRange(string handle, float voiceRange)
        {
            if (!Int32.TryParse(handle, out int serverId))
                return;

            lock (this._voiceClients)
            {
                if (this._voiceClients.TryGetValue(serverId, out VoiceClient client))
                {
                    client.VoiceRange = voiceRange;
                }
            }
        }

        [EventHandler(Event.SaltyChat_UpdateAlive)]
        private void OnClientUpdateAlive(string handle, bool isAlive)
        {
            if (!Int32.TryParse(handle, out int serverId))
                return;

            lock (this._voiceClients)
            {
                if (this._voiceClients.TryGetValue(serverId, out VoiceClient client))
                {
                    client.IsAlive = isAlive;
                }
            }
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
                    this.ExecuteCommand(new PluginCommand(Command.RemovePlayer, this.ServerUniqueIdentifier, new PlayerState(client.TeamSpeakName)));

                    this._voiceClients.Remove(serverId);
                }
            }
        }
        #endregion

        #region Remote Events (Phone)
        [EventHandler(Event.SaltyChat_EstablishCall)]
        private void OnEstablishCall(string handle, string positionJson)
        {
            if (!Int32.TryParse(handle, out int serverId))
                return;

            if (this._voiceClients.TryGetValue(serverId, out VoiceClient client))
            {
                if (client.DistanceCulled)
                {
                    client.LastPosition = Newtonsoft.Json.JsonConvert.DeserializeObject<CitizenFX.Core.Vector3>(positionJson);
                    client.SendPlayerStateUpdate(this);
                }

                CitizenFX.Core.Vector3 playerPosition = Game.PlayerPed.Position;
                CitizenFX.Core.Vector3 remotePlayerPosition = client.LastPosition;

                int signalDistortion = API.GetZoneScumminess(API.GetZoneAtCoords(playerPosition.X, playerPosition.Y, playerPosition.Z));
                signalDistortion += API.GetZoneScumminess(API.GetZoneAtCoords(remotePlayerPosition.X, remotePlayerPosition.Y, remotePlayerPosition.Z));

                this.ExecuteCommand(
                    new PluginCommand(
                        Command.PhoneCommunicationUpdate,
                        this.ServerUniqueIdentifier,
                        new PhoneCommunication(
                            client.TeamSpeakName,
                            signalDistortion
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
                        this.ServerUniqueIdentifier,
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
                this.PrimaryRadioChannel = radioChannel;

                if (String.IsNullOrEmpty(radioChannel))
                    this.PlaySound("leaveRadioChannel", false, "radio");
                else
                    this.PlaySound("enterRadioChannel", false, "radio");
            }
            else
            {
                this.SecondaryRadioChannel = radioChannel;

                if (String.IsNullOrEmpty(radioChannel))
                    this.PlaySound("leaveRadioChannel", false, "radio");
                else
                    this.PlaySound("enterRadioChannel", false, "radio");
            }
        }

        [EventHandler(Event.SaltyChat_IsSending)]
        private void OnPlayerIsSending(string handle, string radioChannel, bool isSending, bool stateChange, string positionJson)
        {
            this.OnPlayerIsSendingRelayed(handle, radioChannel, isSending, stateChange, positionJson, true, new List<dynamic>());
        }

        [EventHandler(Event.SaltyChat_IsSendingRelayed)]
        private void OnPlayerIsSendingRelayed(string handle, string radioChannel, bool isSending, bool stateChange, string positionJson, bool direct, List<dynamic> relays)
        {
            if (!Int32.TryParse(handle, out int serverId))
                return;

            if (serverId == Game.Player.ServerId)
            {
                if (isSending)
                {
                    this.ExecuteCommand(
                        new PluginCommand(
                            Command.RadioCommunicationUpdate,
                            this.ServerUniqueIdentifier,
                            new RadioCommunication(
                                this.TeamSpeakName,
                                RadioType.LongRange,
                                RadioType.LongRange,
                                stateChange,
                                direct,
                                this.SecondaryRadioChannel == radioChannel,
                                relays.Select(r => (string)r).ToArray()
                            )
                        )
                    );
                }
                else
                {
                    this.ExecuteCommand(
                        new PluginCommand(
                            Command.StopRadioCommunication,
                            this.ServerUniqueIdentifier,
                            new RadioCommunication(
                                this.TeamSpeakName,
                                RadioType.None,
                                RadioType.None,
                                stateChange,
                                this.SecondaryRadioChannel == radioChannel
                            )
                        )
                    );
                }
            }
            else if (this._voiceClients.TryGetValue(serverId, out VoiceClient client))
            {
                if (client.DistanceCulled)
                {
                    client.LastPosition = Newtonsoft.Json.JsonConvert.DeserializeObject<CitizenFX.Core.Vector3>(positionJson);
                    client.SendPlayerStateUpdate(this);
                }

                if (isSending)
                {
                    this.ExecuteCommand(
                        new PluginCommand(
                            Command.RadioCommunicationUpdate,
                            this.ServerUniqueIdentifier,
                            new RadioCommunication(
                                client.TeamSpeakName,
                                RadioType.LongRange,
                                RadioType.LongRange,
                                stateChange,
                                direct,
                                this.SecondaryRadioChannel == radioChannel,
                                relays.Select(r => (string)r).ToArray()
                            )
                        )
                    );
                }
                else
                {
                    this.ExecuteCommand(
                        new PluginCommand(
                            Command.StopRadioCommunication,
                            this.ServerUniqueIdentifier,
                            new RadioCommunication(
                                client.TeamSpeakName,
                                RadioType.None,
                                RadioType.None,
                                stateChange,
                                this.SecondaryRadioChannel == radioChannel
                            )
                        )
                    );
                }
            }
        }

        [EventHandler(Event.SaltyChat_UpdateRadioTowers)]
        private void OnUpdateRadioTowers(dynamic towers)
        {
            List<Vector3> towerPositions = new List<Vector3>();

            foreach (dynamic tower in towers)
            {
                towerPositions.Add(new Vector3(tower[0], tower[1], tower[2]));
            }

            this.RadioTowers = towerPositions.ToArray();

            if (this.IsIngame)
            {
                this.ExecuteCommand(
                    new PluginCommand(
                        Command.RadioTowerUpdate,
                        this.ServerUniqueIdentifier,
                        new RadioTower(
                            towerPositions.ToArray()
                        )
                    )
                );
            }
        }
        #endregion

        #region Remote Events(Megaphone)
        [EventHandler(Event.SaltyChat_IsUsingMegaphone)]
        private void OnIsUsingMegaphone(string handle, float range, bool isSending, string positionJson)
        {
            if (!Int32.TryParse(handle, out int serverId))
                return;

            if (this._voiceClients.TryGetValue(serverId, out VoiceClient client))
            {
                if (client.DistanceCulled)
                {
                    client.LastPosition = Newtonsoft.Json.JsonConvert.DeserializeObject<CitizenFX.Core.Vector3>(positionJson);
                    client.SendPlayerStateUpdate(this);
                }

                if (isSending)
                {
                    this.ExecuteCommand(
                        new PluginCommand(
                            Command.MegaphoneCommunicationUpdate,
                            this.ServerUniqueIdentifier,
                            new MegaphoneCommunication(
                                this.TeamSpeakName,
                                range
                            )
                        )
                    );
                }
                else
                {
                    this.ExecuteCommand(
                        new PluginCommand(
                            Command.StopRadioCommunication,
                            this.ServerUniqueIdentifier,
                            new MegaphoneCommunication(
                                this.TeamSpeakName,
                                0f
                            )
                        )
                    );
                }
            }
        }
        #endregion

        #region Exports (Radio)
        internal string GetRadioChannel(bool primary)
        {
            if (primary)
                return this.PrimaryRadioChannel;
            else
                return this.SecondaryRadioChannel;
        }

        internal void SetRadioChannel(string radioChannelName, bool primary)
        {
            if ((primary && this.PrimaryRadioChannel == radioChannelName) ||
                (!primary && this.SecondaryRadioChannel == radioChannelName))
                return;

            BaseScript.TriggerServerEvent(Event.SaltyChat_SetRadioChannel, radioChannelName, primary);
        }
        #endregion

        #region NUI Events
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

            cb("");
        }

        [EventHandler("__cfx_nui:" + NuiEvent.SaltyChat_OnMessage)]
        private void OnMessage(dynamic message, dynamic cb)
        {
            cb("");

            PluginCommand pluginCommand = PluginCommand.Deserialize(message);

            if (pluginCommand.ServerUniqueIdentifier != this.ServerUniqueIdentifier)
                return;

            switch (pluginCommand.Command)
            {
                case Command.PluginState:
                    {
                        if (pluginCommand.TryGetPayload(out PluginState pluginState))
                        {
                            BaseScript.TriggerServerEvent(Event.SaltyChat_CheckVersion, pluginState.Version);

                            this.ExecuteCommand(
                                new PluginCommand(
                                    Command.RadioTowerUpdate,
                                    this.ServerUniqueIdentifier,
                                    new RadioTower(this.RadioTowers)
                                )
                            );
                        }   

                        break;
                    }
                case Command.Reset:
                    {
                        this.IsIngame = false;

                        this.InitializePlugin();

                        break;
                    }
                case Command.Ping:
                    {
                        this.ExecuteCommand(new PluginCommand(this.ServerUniqueIdentifier));

                        break;
                    }
                case Command.InstanceState:
                    {
                        if (pluginCommand.TryGetPayload(out InstanceState instanceState))
                        {
                            this.IsIngame = instanceState.IsReady;
                        }

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
            }
        }

        [EventHandler("__cfx_nui:" + NuiEvent.SaltyChat_OnError)]
        private void OnError(dynamic message, dynamic cb)
        {
            try
            {
                PluginError pluginError = PluginError.Deserialize(message);

                switch (pluginError.Error)
                {
                    case Error.AlreadyInGame:
                        {
                            Debug.WriteLine($"[Salty Chat] Error: Seems like we are already in an instance, retry...");

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
                Debug.WriteLine($"[Salty Chat] Error: We received an error, but couldn't deserialize it:{Environment.NewLine}{e.ToString()}");
            }

            cb("");
        }
        #endregion

        #region Tick
        [Tick]
        private async Task OnControlTick()
        {
            Game.DisableControlThisFrame(0, Control.EnterCheatCode);
            Game.DisableControlThisFrame(0, Control.PushToTalk);
            Game.DisableControlThisFrame(0, Control.VehiclePushbikeSprint);
            Game.DisableControlThisFrame(0, Control.SpecialAbilitySecondary);

            if (Game.Player.IsAlive)
            {
                Ped playerPed = Game.PlayerPed;

                if (Game.IsControlJustPressed(0, Control.EnterCheatCode))
                {
                    this.ToggleVoiceRange();
                }

                if (playerPed.IsInPoliceVehicle)
                {
                    Vehicle vehicle = playerPed.CurrentVehicle;

                    if (vehicle.GetPedOnSeat(VehicleSeat.Driver) == playerPed || vehicle.GetPedOnSeat(VehicleSeat.Passenger) == playerPed)
                    {
                        if (Game.IsControlJustPressed(0, Control.SpecialAbilitySecondary))
                        {
                            BaseScript.TriggerServerEvent(Event.SaltyChat_IsUsingMegaphone, true);
                            this.IsUsingMegaphone = true;
                        }
                        else if (Game.IsControlJustReleased(0, Control.SpecialAbilitySecondary))
                        {
                            BaseScript.TriggerServerEvent(Event.SaltyChat_IsUsingMegaphone, false);
                            this.IsUsingMegaphone = false;
                        }
                    }
                }
                else if (this.IsUsingMegaphone)
                {
                    BaseScript.TriggerServerEvent(Event.SaltyChat_IsUsingMegaphone, false);
                    this.IsUsingMegaphone = false;
                }

                if (this.PrimaryRadioChannel != null)
                {
                    if (Game.IsControlJustPressed(0, Control.PushToTalk))
                        BaseScript.TriggerServerEvent(Event.SaltyChat_IsSending, this.PrimaryRadioChannel, true);
                    else if (Game.IsControlJustReleased(0, Control.PushToTalk))
                        BaseScript.TriggerServerEvent(Event.SaltyChat_IsSending, this.PrimaryRadioChannel, false);
                }

                if (this.SecondaryRadioChannel != null)
                {
                    if (Game.IsControlJustPressed(0, Control.VehiclePushbikeSprint))
                        BaseScript.TriggerServerEvent(Event.SaltyChat_IsSending, this.SecondaryRadioChannel, true);
                    else if (Game.IsControlJustReleased(0, Control.VehiclePushbikeSprint))
                        BaseScript.TriggerServerEvent(Event.SaltyChat_IsSending, this.SecondaryRadioChannel, false);
                }
            }

            await Task.FromResult(0);
        }

        [Tick]
        private async Task OnStateUpdateTick()
        {
            if (this.IsConnected && this.IsIngame)
            {
                List<PlayerState> playerStates = new List<PlayerState>();

                Ped playerPed = Game.PlayerPed;
                CitizenFX.Core.Vector3 playerPosition = playerPed.Position;
                int playerRoomId = API.GetRoomKeyFromEntity(playerPed.Handle);

                foreach (VoiceClient client in this.VoiceClients)
                {
                    Player nPlayer = client.Player;

                    if (nPlayer == null)
                    {
                        if (client.DistanceCulled)
                            continue;

                        client.DistanceCulled = true;

                        playerStates.Add(
                            new PlayerState(
                                client.TeamSpeakName,
                                client.LastPosition,
                                client.VoiceRange,
                                client.IsAlive,
                                client.DistanceCulled
                            )
                        );
                    }
                    else
                    {
                        if (client.DistanceCulled)
                            client.DistanceCulled = false;

                        Ped nPed = nPlayer.Character;
                        client.LastPosition = nPed.Position;

                        int nPlayerRoomId = API.GetRoomKeyFromEntity(nPed.Handle);
                        int? muffleIntensity = null;

                        if (nPlayerRoomId != playerRoomId && !API.HasEntityClearLosToEntity(playerPed.Handle, nPed.Handle, 17))
                            muffleIntensity = 10;

                        playerStates.Add(
                            new PlayerState(
                                client.TeamSpeakName,
                                client.LastPosition,
                                client.VoiceRange,
                                client.IsAlive,
                                client.DistanceCulled,
                                muffleIntensity
                            )
                        );
                    }
                }

                this.ExecuteCommand(
                    new PluginCommand(
                        Command.BulkUpdate,
                        this.ServerUniqueIdentifier,
                        new BulkUpdate(
                            playerStates,
                            new PlayerState(
                                playerPosition,
                                API.GetGameplayCamRot(0).Z
                            )
                        )
                    )
                );
            }

            await BaseScript.Delay(250);
        }
        #endregion

        #region Methods (Proximity)
        private void SetPlayerTalking(string teamSpeakName, bool isTalking)
        {
            Ped playerPed = null;
            VoiceClient voiceClient = this.VoiceClients.FirstOrDefault(v => v.TeamSpeakName == teamSpeakName);

            if (voiceClient != null)
            {
                playerPed = voiceClient.Player?.Character;
            }
            else if (teamSpeakName == this.TeamSpeakName)
            {
                playerPed = Game.PlayerPed;

                BaseScript.TriggerEvent(Event.SaltyChat_TalkStateChanged, isTalking);
            }

            if (playerPed != null)
            {
                API.SetPlayerTalkingOverride(playerPed.Handle, isTalking);

                // Lip sync workaround for OneSync
                if (isTalking)
                    API.PlayFacialAnim(playerPed.Handle, "mic_chatter", "mp_facial");
                else
                    API.PlayFacialAnim(playerPed.Handle, "mood_normal_1", "facials@gen_male@variations@normal");
            }
        }

        /// <summary>
        /// Toggles voice range through <see cref="Voice.VoiceRanges"/>
        /// </summary>
        public void ToggleVoiceRange()
        {
            int index = Array.IndexOf(SharedData.VoiceRanges, this.VoiceRange);

            if (index < 0)
            {
                this.VoiceRange = SharedData.VoiceRanges[1];
            }
            else if (index + 1 >= SharedData.VoiceRanges.Length)
            {
                this.VoiceRange = SharedData.VoiceRanges[0];
            }
            else
            {
                this.VoiceRange = SharedData.VoiceRanges[index + 1];
            }

            BaseScript.TriggerServerEvent(Event.SaltyChat_SetVoiceRange, this.VoiceRange);

            CitizenFX.Core.UI.Screen.ShowNotification($"New voice range is {this.VoiceRange} metres.");
        }
        #endregion

        #region Methods (Plugin)
        private void InitializePlugin()
        {
            this.ExecuteCommand(
                new PluginCommand(
                    Command.Initiate,
                    new GameInstance(
                        this.ServerUniqueIdentifier,
                        this.TeamSpeakName,
                        this.IngameChannel,
                        this.IngameChannelPassword,
                        this.SoundPack,
                        this.SwissChannelIds
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
                    this.ServerUniqueIdentifier,
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
                    this.ServerUniqueIdentifier,
                    new Sound(handle)
                )
            );
        }

        private void ExecuteCommand(string funtion, object parameters)
        {
            API.SendNuiMessage(
                Newtonsoft.Json.JsonConvert.SerializeObject(new { Function = funtion, Params = parameters })
            );
        }

        internal void ExecuteCommand(PluginCommand pluginCommand)
        {
            this.ExecuteCommand("runCommand", Util.ToJson(pluginCommand));
        }

        private void DisplayDebug(bool show)
        {
            this.ExecuteCommand("showBody", show);
        }
        #endregion
    }
}
