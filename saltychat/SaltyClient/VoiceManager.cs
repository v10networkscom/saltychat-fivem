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
        public static bool IsEnabled { get; private set; }
        public static bool IsConnected { get; private set; }
        public static bool IsIngame { get; private set; }

        public static string TeamSpeakName { get; private set; }
        public static string ServerUniqueIdentifier { get; private set; }
        public static string SoundPack { get; private set; }
        public static ulong IngameChannel { get; private set; }
        public static string IngameChannelPassword { get; private set; }
        public static ulong[] SwissChannelIds { get; private set; }

        public static VoiceClient[] VoiceClients => VoiceManager._voiceClients.Values.ToArray();
        private static Dictionary<int, VoiceClient> _voiceClients = new Dictionary<int, VoiceClient>();

        public static float VoiceRange { get; private set; } = SharedData.VoiceRanges[1];
        public static string PrimaryRadioChannel { get; private set; }
        public static string SecondaryRadioChannel { get; private set; }

        public static bool IsTalking { get; private set; }
        public static bool IsMicrophoneMuted { get; private set; }
        public static bool IsSoundMuted { get; private set; }
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
        }
        #endregion

        #region Events
        [EventHandler("onClientResourceStart")]
        private void OnResourceStart(string resourceName)
        {
            if (resourceName != API.GetCurrentResourceName())
                return;

            VoiceManager.ServerUniqueIdentifier = API.GetResourceMetadata(resourceName, "ServerUniqueIdentifier", 0);
            VoiceManager.SoundPack = API.GetResourceMetadata(resourceName, "SoundPack", 0);
            VoiceManager.IngameChannel = UInt64.Parse(API.GetResourceMetadata(resourceName, "IngameChannelId", 0));
            VoiceManager.IngameChannelPassword = API.GetResourceMetadata(resourceName, "IngameChannelPassword", 0);

            string swissChannelIds = API.GetResourceMetadata(resourceName, "SwissChannelIds", 0);

            if (!String.IsNullOrEmpty(swissChannelIds))
            {
                VoiceManager.SwissChannelIds = swissChannelIds.Split(',').Select(s => UInt64.Parse(s.Trim())).ToArray();
            }

            BaseScript.TriggerServerEvent(Event.SaltyChat_Initialize);
        }

        [EventHandler("onClientResourceStop")]
        private void OnResourceStop(string resourceName)
        {
            if (resourceName != API.GetCurrentResourceName())
                return;

            VoiceManager.IsEnabled = false;
            VoiceManager.IsConnected = false;

            lock (VoiceManager._voiceClients)
            {
                VoiceManager._voiceClients.Clear();
            }

            VoiceManager.PrimaryRadioChannel = null;
            VoiceManager.SecondaryRadioChannel = null;
        }
        #endregion

        #region Remote Events (Handling)
        [EventHandler(Event.SaltyChat_Initialize)]
        private void OnInitialize(string teamSpeakName)
        {
            VoiceManager.TeamSpeakName = teamSpeakName;
            VoiceManager.IsEnabled = true;

            if (VoiceManager.IsConnected)
                this.InitializePlugin();
            else
                this.ExecuteCommand("connect", "127.0.0.1:8088");

            //Voice.DisplayDebug(true);
        }

        [EventHandler(Event.SaltyChat_UpdateClient)]
        private void OnClientUpdate(string handle, string teamSpeakName, float voiceRange)
        {
            if (!Int32.TryParse(handle, out int serverId))
                return;

            if (Game.Player.ServerId == serverId)
                return;

            lock (VoiceManager._voiceClients)
            {
                if (VoiceManager._voiceClients.TryGetValue(serverId, out VoiceClient client))
                {
                    client.TeamSpeakName = teamSpeakName;
                    client.VoiceRange = voiceRange;
                }
                else
                {
                    VoiceManager._voiceClients.Add(serverId, new VoiceClient(this.Players[serverId], teamSpeakName, voiceRange));
                }
            }
        }

        [EventHandler(Event.SaltyChat_IsTalking)]
        private void OnIsTalking(string handle, bool isTalking)
        {
            if (!Int32.TryParse(handle, out int serverId))
                return;

            Player player = this.Players[serverId];

            if (player == null)
                return;

            API.SetPlayerTalkingOverride(player.Handle, isTalking);

            // Lip sync workaround for OneSync
            if (isTalking)
                API.PlayFacialAnim(player.Character.Handle, "mic_chatter", "mp_facial");
            else
                API.PlayFacialAnim(player.Character.Handle, "mood_normal_1", "facials@gen_male@variations@normal");
        }

        [EventHandler(Event.SaltyChat_RemoveClient)]
        private void OnClientRemove(string handle)
        {
            if (!Int32.TryParse(handle, out int serverId))
                return;

            lock (VoiceManager._voiceClients)
            {
                if (VoiceManager._voiceClients.TryGetValue(serverId, out VoiceClient client))
                {
                    this.ExecuteCommand(new PluginCommand(Command.RemovePlayer, VoiceManager.ServerUniqueIdentifier, new PlayerState(client.TeamSpeakName)));

                    VoiceManager._voiceClients.Remove(serverId);
                }
            }
        }
        #endregion

        #region Remote Events (Phone)
        [EventHandler(Event.SaltyChat_EstablishCall)]
        private void OnEstablishCall(string handle)
        {
            if (!Int32.TryParse(handle, out int serverId))
                return;

            if (VoiceManager._voiceClients.TryGetValue(serverId, out VoiceClient client))
            {
                Vector3 playerPosition = Game.PlayerPed.Position;
                Vector3 remotePlayerPosition = client.Player.Character.Position;

                int signalDistortion = API.GetZoneScumminess(API.GetZoneAtCoords(playerPosition.X, playerPosition.Y, playerPosition.Z));
                signalDistortion += API.GetZoneScumminess(API.GetZoneAtCoords(remotePlayerPosition.X, remotePlayerPosition.Y, remotePlayerPosition.Z));

                this.ExecuteCommand(
                    new PluginCommand(
                        Command.PhoneCommunicationUpdate,
                        VoiceManager.ServerUniqueIdentifier,
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

            if (VoiceManager._voiceClients.TryGetValue(serverId, out VoiceClient client))
            {
                this.ExecuteCommand(
                    new PluginCommand(
                        Command.StopPhoneCommunication,
                        VoiceManager.ServerUniqueIdentifier,
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
                VoiceManager.PrimaryRadioChannel = radioChannel;

                if (String.IsNullOrEmpty(radioChannel))
                    this.PlaySound("leaveRadioChannel", false, "radio");
                else
                    this.PlaySound("enterRadioChannel", false, "radio");
            }
            else
            {
                VoiceManager.SecondaryRadioChannel = radioChannel;

                if (String.IsNullOrEmpty(radioChannel))
                    this.PlaySound("leaveRadioChannel", false, "radio");
                else
                    this.PlaySound("enterRadioChannel", false, "radio");
            }   
        }

        [EventHandler(Event.SaltyChat_IsSending)]
        private void OnPlayerIsSending(string handle, string radioChannel, bool isSending, bool stateChange)
        {
            this.OnPlayerIsSendingRelayed(handle, radioChannel, isSending, stateChange, true, new List<dynamic>());
        }

        [EventHandler(Event.SaltyChat_IsSendingRelayed)]
        private void OnPlayerIsSendingRelayed(string handle, string radioChannel, bool isSending, bool stateChange, bool direct, List<dynamic> relays)
        {
            if (!Int32.TryParse(handle, out int serverId))
                return;

            if (serverId == Game.Player.ServerId)
            {
                this.PlaySound("selfMicClick", false, "MicClick");
            }
            else if (VoiceManager._voiceClients.TryGetValue(serverId, out VoiceClient client))
            {
                if (isSending)
                {
                    this.ExecuteCommand(
                        new PluginCommand(
                            Command.RadioCommunicationUpdate,
                            VoiceManager.ServerUniqueIdentifier,
                            new RadioCommunication(
                                client.TeamSpeakName,
                                RadioType.LongRange,
                                RadioType.LongRange,
                                stateChange,
                                direct,
                                VoiceManager.SecondaryRadioChannel == radioChannel,
                                relays.Select(r => (string)r).ToArray()
                            )
                        )
                    );
                }
                else
                {
                    this.ExecuteCommand(
                        new PluginCommand(
                            Command.RadioCommunicationUpdate,
                            VoiceManager.ServerUniqueIdentifier,
                            new RadioCommunication(
                                client.TeamSpeakName,
                                RadioType.None,
                                RadioType.None,
                                stateChange,
                                VoiceManager.SecondaryRadioChannel == radioChannel
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

            this.ExecuteCommand(
                new PluginCommand(
                    Command.RadioTowerUpdate,
                    VoiceManager.ServerUniqueIdentifier,
                    new RadioTower(
                        towerPositions.ToArray()
                    )
                )
            );
        }
        #endregion

        #region Exports (Radio)
        internal string GetRadioChannel(bool primary)
        {
            if (primary)
                return VoiceManager.PrimaryRadioChannel;
            else
                return VoiceManager.SecondaryRadioChannel;
        }

        internal void SetRadioChannel(string radioChannelName, bool primary)
        {
            if ((primary && VoiceManager.PrimaryRadioChannel == radioChannelName) ||
                (!primary && VoiceManager.SecondaryRadioChannel == radioChannelName))
                return;

            BaseScript.TriggerServerEvent(Event.SaltyChat_SetRadioChannel, radioChannelName, primary);
        }
        #endregion

        #region NUI Events
        [EventHandler("__cfx_nui:" + NuiEvent.SaltyChat_OnConnected)]
        private void OnConnected()
        {
            VoiceManager.IsConnected = true;

            if (VoiceManager.IsEnabled)
                this.InitializePlugin();
        }

        [EventHandler("__cfx_nui:" + NuiEvent.SaltyChat_OnDisconnected)]
        private void OnDisconnected()
        {
            VoiceManager.IsConnected = false;
        }

        [EventHandler("__cfx_nui:" + NuiEvent.SaltyChat_OnMessage)]
        private void OnMessage(dynamic message)
        {
            PluginCommand pluginCommand = PluginCommand.Deserialize(message);

            if (pluginCommand.Command == Command.Ping && pluginCommand.ServerUniqueIdentifier == VoiceManager.ServerUniqueIdentifier)
            {
                this.ExecuteCommand(new PluginCommand(VoiceManager.ServerUniqueIdentifier));
                return;
            }

            if (!pluginCommand.TryGetState(out PluginState pluginState))
                return;

            if (pluginState.IsReady != VoiceManager.IsIngame)
            {
                BaseScript.TriggerServerEvent(Event.SaltyChat_CheckVersion, pluginState.UpdateBranch, pluginState.Version);

                VoiceManager.IsIngame = pluginState.IsReady;
            }

            if (pluginState.IsTalking != VoiceManager.IsTalking)
            {
                VoiceManager.IsTalking = pluginState.IsTalking;

                BaseScript.TriggerEvent(Event.SaltyChat_TalkStateChanged, VoiceManager.IsTalking);

                BaseScript.TriggerServerEvent(Event.SaltyChat_IsTalking, VoiceManager.IsTalking);
            }

            if (pluginState.IsMicrophoneMuted != VoiceManager.IsMicrophoneMuted)
            {
                VoiceManager.IsMicrophoneMuted = pluginState.IsMicrophoneMuted;

                BaseScript.TriggerEvent(Event.SaltyChat_MicStateChanged, VoiceManager.IsMicrophoneMuted);
            }

            if (pluginState.IsSoundMuted != VoiceManager.IsSoundMuted)
            {
                VoiceManager.IsSoundMuted = pluginState.IsSoundMuted;

                BaseScript.TriggerEvent(Event.SaltyChat_SoundStateChanged, VoiceManager.IsSoundMuted);
            }   
        }

        [EventHandler("__cfx_nui:" + NuiEvent.SaltyChat_OnError)]
        private void OnError(dynamic message)
        {
            PluginCommand pluginCommand = PluginCommand.Deserialize(message);

            if (pluginCommand.TryGetError(out PluginError pluginError))
                Debug.WriteLine($"[Salty Chat] Error: {pluginError.Error} - Message: {pluginError.Message}");
        }
        #endregion

        #region Tick
        [Tick]
        private async Task OnControlTick()
        {
            Game.DisableControlThisFrame(0, Control.EnterCheatCode);
            Game.DisableControlThisFrame(0, Control.PushToTalk);
            Game.DisableControlThisFrame(0, Control.VehiclePushbikeSprint);

            if (Game.Player.IsAlive)
            {
                if (Game.IsControlJustPressed(0, Control.EnterCheatCode))
                {
                    this.ToggleVoiceRange();
                }

                if (VoiceManager.PrimaryRadioChannel != null)
                {
                    if (Game.IsControlJustPressed(0, Control.PushToTalk))
                        BaseScript.TriggerServerEvent(Event.SaltyChat_IsSending, VoiceManager.PrimaryRadioChannel, true);
                    else if (Game.IsControlJustReleased(0, Control.PushToTalk))
                        BaseScript.TriggerServerEvent(Event.SaltyChat_IsSending, VoiceManager.PrimaryRadioChannel, false);
                }

                if (VoiceManager.SecondaryRadioChannel != null)
                {
                    if (Game.IsControlJustPressed(0, Control.VehiclePushbikeSprint))
                        BaseScript.TriggerServerEvent(Event.SaltyChat_IsSending, VoiceManager.SecondaryRadioChannel, true);
                    else if (Game.IsControlJustReleased(0, Control.VehiclePushbikeSprint))
                        BaseScript.TriggerServerEvent(Event.SaltyChat_IsSending, VoiceManager.SecondaryRadioChannel, false);
                }
            }

            await Task.FromResult(0);
        }

        [Tick]
        private async Task OnStateUpdateTick()
        {
            if (VoiceManager.IsConnected && VoiceManager.IsIngame)
            {
                List<PlayerState> playerStates = new List<PlayerState>();

                Vector3 playerPosition = Game.PlayerPed.Position;

                foreach (VoiceClient client in VoiceManager.VoiceClients)
                {
                    Ped ped = client.Player.Character;

                    if (!ped.Exists())
                        continue;

                    playerStates.Add(
                        new PlayerState(
                            client.TeamSpeakName,
                            ped.Position,
                            client.VoiceRange,
                            client.Player.IsAlive
                        )
                    );
                }

                this.ExecuteCommand(
                    new PluginCommand(
                        Command.BulkUpdate,
                        VoiceManager.ServerUniqueIdentifier,
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

        #region Methods
        private void InitializePlugin()
        {
            this.ExecuteCommand(
                new PluginCommand(
                    Command.Initiate,
                    new GameInstance(
                        VoiceManager.ServerUniqueIdentifier,
                        VoiceManager.TeamSpeakName,
                        VoiceManager.IngameChannel,
                        VoiceManager.IngameChannelPassword,
                        VoiceManager.SoundPack,
                        VoiceManager.SwissChannelIds
                    )
                )
            );
        }

        /// <summary>
        /// Toggles voice range through <see cref="Voice.VoiceRanges"/>
        /// </summary>
        public void ToggleVoiceRange()
        {
            int index = Array.IndexOf(SharedData.VoiceRanges, VoiceManager.VoiceRange);

            if (index < 0)
            {
                VoiceManager.VoiceRange = SharedData.VoiceRanges[1];
            }
            else if (index + 1 >= SharedData.VoiceRanges.Length)
            {
                VoiceManager.VoiceRange = SharedData.VoiceRanges[0];
            }
            else
            {
                VoiceManager.VoiceRange = SharedData.VoiceRanges[index + 1];
            }

            BaseScript.TriggerServerEvent(Event.SaltyChat_SetVoiceRange, VoiceManager.VoiceRange);

            CitizenFX.Core.UI.Screen.ShowNotification($"New voice range is {VoiceManager.VoiceRange} metres.");
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
                    VoiceManager.ServerUniqueIdentifier,
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
                    VoiceManager.ServerUniqueIdentifier,
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

        private void ExecuteCommand(PluginCommand pluginCommand)
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
