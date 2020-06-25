using System;
using System.Collections.Generic;
using System.Linq;
using SaltyShared;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Newtonsoft.Json;

namespace SaltyServer
{
    public class VoiceManager : BaseScript
    {
        #region Properties / Fields
        public static bool Enabled { get; private set; }
        public static string RequiredUpdateBranch { get; private set; }
        public static string MinimumPluginVersion { get; private set; }

        public static Vector3[] RadioTowers { get; private set; } = new Vector3[0];

        public static VoiceClient[] VoiceClients => VoiceManager._voiceClients.Values.ToArray();
        private static Dictionary<Player, VoiceClient> _voiceClients = new Dictionary<Player, VoiceClient>();

        public static RadioChannel[] RadioChannels => VoiceManager._radioChannels.ToArray();
        private static List<RadioChannel> _radioChannels = new List<RadioChannel>();
        #endregion

        #region CTOR
        public VoiceManager()
        {
            // General Exports
            this.Exports.Add("SetPlayerAlive", new Action<int, bool>(this.SetPlayerAlive));

            // Phone Exports
            this.Exports.Add("EstablishCall", new Action<int, int>(this.EstablishCall));
            this.Exports.Add("EndCall", new Action<int, int>(this.EndCall));

            // Radio Exports
            this.Exports.Add("SetPlayerRadioSpeaker", new Action<int, bool>(this.SetPlayerRadioSpeaker));
            this.Exports.Add("SetPlayerRadioChannel", new Action<int, string, bool>(this.SetPlayerRadioChannel));
            this.Exports.Add("RemovePlayerRadioChannel", new Action<int, string>(this.RemovePlayerRadioChannel));
            this.Exports.Add("SetRadioTowers", new Action<dynamic>(this.SetRadioTowers));
        }
        #endregion

        #region Server Events
        [EventHandler("onResourceStart")]
        private void OnResourceStart(string resourceName)
        {
            if (resourceName != API.GetCurrentResourceName())
                return;

            VoiceManager.Enabled = API.GetResourceMetadata(resourceName, "VoiceEnabled", 0).Equals("true", StringComparison.OrdinalIgnoreCase);

            if (VoiceManager.Enabled)
            {
                VoiceManager.RequiredUpdateBranch = API.GetResourceMetadata(resourceName, "RequiredUpdateBranch", 0);
                VoiceManager.MinimumPluginVersion = API.GetResourceMetadata(resourceName, "MinimumPluginVersion", 0);
            }
        }

        [EventHandler("onResourceStop")]
        private void OnResourceStop(string resourceName)
        {
            if (resourceName != API.GetCurrentResourceName())
                return;

            VoiceManager.Enabled = false;

            lock (VoiceManager._voiceClients)
            {
                VoiceManager._voiceClients.Clear();
            }

            lock (VoiceManager._radioChannels)
            {
                foreach (RadioChannel radioChannel in VoiceManager._radioChannels)
                {
                    foreach (RadioChannelMember member in radioChannel.Members)
                    {
                        radioChannel.RemoveMember(member.VoiceClient);
                    }
                }

                VoiceManager._radioChannels.Clear();
            }
        }

        [EventHandler("playerDropped")]
        private void OnPlayerDisconnected([FromSource] Player player, string reason)
        {
            if (!VoiceManager._voiceClients.TryGetValue(player, out VoiceClient client))
                return;

            foreach (RadioChannel radioChannel in VoiceManager.RadioChannels.Where(c => c.IsMember(client)))
            {
                radioChannel.RemoveMember(client);
            }

            lock (VoiceManager._voiceClients)
            {
                VoiceManager._voiceClients.Remove(player);
            }

            BaseScript.TriggerClientEvent(Event.SaltyChat_RemoveClient, player.Handle);
        }
        #endregion

        #region RemoteEvents (Proximity)
        [EventHandler(Event.SaltyChat_SetVoiceRange)]
        private void OnSetVoiceRange([FromSource] Player player, float voiceRange)
        {
            if (!VoiceManager._voiceClients.TryGetValue(player, out VoiceClient client))
                return;

            if (Array.IndexOf(SharedData.VoiceRanges, voiceRange) >= 0)
            {
                client.VoiceRange = voiceRange;

                BaseScript.TriggerClientEvent(Event.SaltyChat_UpdateVoiceRange, player.Handle, client.VoiceRange);
            }
        }
        #endregion

        #region Exports (General)
        private void SetPlayerAlive(int netId, bool isAlive)
        {
            Player player = this.Players[netId];

            lock (VoiceManager._voiceClients)
            {
                if (VoiceManager._voiceClients.ContainsKey(player))
                {
                    VoiceManager._voiceClients[player].IsAlive = isAlive;

                    BaseScript.TriggerClientEvent(Event.SaltyChat_UpdateAlive, player.Handle, isAlive);
                }
            }
        }
        #endregion

        #region Exports (Phone)
        private void EstablishCall(int callerNetId, int partnerNetId)
        {
            Player caller = this.Players[callerNetId];
            Player callPartner = this.Players[partnerNetId];

            caller.TriggerEvent(Event.SaltyChat_EstablishCall, callPartner.Handle, JsonConvert.SerializeObject(callPartner.Character.Position));
            callPartner.TriggerEvent(Event.SaltyChat_EstablishCall, caller.Handle, JsonConvert.SerializeObject(caller.Character.Position));
        }

        private void EndCall(int callerNetId, int partnerNetId)
        {
            Player caller = this.Players[callerNetId];
            Player callPartner = this.Players[partnerNetId];

            caller.TriggerEvent(Event.SaltyChat_EndCall, callPartner.Handle);
            callPartner.TriggerEvent(Event.SaltyChat_EndCall, caller.Handle);
        }
        #endregion

        #region Exports (Radio)
        private void SetPlayerRadioSpeaker(int netId, bool toggle)
        {
            Player player = this.Players[netId];

            if (!VoiceManager._voiceClients.TryGetValue(player, out VoiceClient voiceClient))
                return;

            VoiceManager.SetRadioSpeaker(voiceClient, toggle);
        }

        private void SetPlayerRadioChannel(int netId, string radioChannelName, bool isPrimary)
        {
            Player player = this.Players[netId];

            if (!VoiceManager._voiceClients.TryGetValue(player, out VoiceClient voiceClient))
                return;

            VoiceManager.JoinRadioChannel(voiceClient, radioChannelName, isPrimary);
        }

        private void RemovePlayerRadioChannel(int netId, string radioChannelName)
        {
            Player player = this.Players[netId];

            if (!VoiceManager._voiceClients.TryGetValue(player, out VoiceClient voiceClient))
                return;

            VoiceManager.LeaveRadioChannel(voiceClient, radioChannelName);
        }

        private void SetRadioTowers(dynamic towers)
        {
            List<Vector3> towerPositions = new List<Vector3>();

            foreach (dynamic tower in towers)
            {
                towerPositions.Add(new Vector3(tower[0], tower[1], tower[2]));
            }

            VoiceManager.RadioTowers = towerPositions.ToArray();

            BaseScript.TriggerClientEvent(Event.SaltyChat_UpdateRadioTowers, VoiceManager.RadioTowers);
        }
        #endregion

        #region Remote Events (Salty Chat)
        [EventHandler(Event.SaltyChat_Initialize)]
        private void OnInitialize([FromSource] Player player)
        {
            if (!VoiceManager.Enabled)
                return;

            VoiceClient voiceClient;

            lock (VoiceManager._voiceClients)
            {
                voiceClient = new VoiceClient(player, VoiceManager.GetTeamSpeakName(), SharedData.VoiceRanges[1], true);

                if (VoiceManager._voiceClients.ContainsKey(player))
                    VoiceManager._voiceClients[player] = voiceClient;
                else
                    VoiceManager._voiceClients.Add(player, voiceClient);
            }

            player.TriggerEvent(Event.SaltyChat_Initialize, voiceClient.TeamSpeakName, VoiceManager.RadioTowers);

            Vector3 voiceClientPosition = voiceClient.Player.Character != null ? voiceClient.Player.Character.Position : new Vector3(0.0f, 0.0f, 0.0f);
            string clientJson = JsonConvert.SerializeObject(new SaltyShared.VoiceClient(voiceClient.Player.GetServerId(), voiceClient.TeamSpeakName, voiceClient.VoiceRange, true, new Position(voiceClientPosition.X, voiceClientPosition.Y, voiceClientPosition.Z)));
            
            List<SaltyShared.VoiceClient> voiceClients = new List<SaltyShared.VoiceClient>();

            foreach (VoiceClient client in VoiceManager.VoiceClients.Where(c => c.Player != player))
            {
                Vector3 clientPosition = client.Player.Character != null ? client.Player.Character.Position : new Vector3(0.0f, 0.0f, 0.0f);

                voiceClients.Add(new SaltyShared.VoiceClient(client.Player.GetServerId(), client.TeamSpeakName, client.VoiceRange, client.IsAlive, new Position(clientPosition.X, clientPosition.Y, clientPosition.Z)));

                client.Player.TriggerEvent(Event.SaltyChat_UpdateClient, clientJson);
            }

            player.TriggerEvent(Event.SaltyChat_SyncClients, JsonConvert.SerializeObject(voiceClients));
        }

        [EventHandler(Event.SaltyChat_CheckVersion)]
        private void OnCheckVersion([FromSource] Player player, string updateChannel, string version)
        {
            if (!VoiceManager._voiceClients.TryGetValue(player, out VoiceClient client))
                return;

            if (!VoiceManager.IsVersionAccepted(updateChannel, version))
            {
                player.Drop($"[Salty Chat] Required Branch: {VoiceManager.RequiredUpdateBranch} | Required Version: {VoiceManager.MinimumPluginVersion}");
                return;
            }
        }
        #endregion

        #region Commands (Radio)
#if DEBUG
        [Command("speaker")]
        private void OnSetRadioSpeaker(Player player, string[] args)
        {
            if (args.Length < 1)
            {
                player.SendChatMessage("Usage", "/speaker {true/false}");
                return;
            }
            
            if (!VoiceManager._voiceClients.TryGetValue(player, out VoiceClient voiceClient))
                return;

            bool toggle = String.Equals(args[0], "true", StringComparison.OrdinalIgnoreCase);

            VoiceManager.SetRadioSpeaker(voiceClient, toggle);

            player.SendChatMessage("Speaker", $"The speaker is now {(toggle ? "on" : "off")}.");
        }

        [Command("joinradio")]
        private void OnJoinRadioChannel(Player player, string[] args)
        {
            if (args.Length < 1)
            {
                player.SendChatMessage("Usage", "/joinradio {radioChannelName}");
                return;
            }
            
            if (!VoiceManager._voiceClients.TryGetValue(player, out VoiceClient voiceClient))
                return;

            VoiceManager.JoinRadioChannel(voiceClient, args[0], true);

            player.SendChatMessage("Radio", $"You joined channel \"{args[0]}\".");
        }

        [Command("joinsecradio")]
        private void OnJoinSecondaryRadioChannel(Player player, string[] args)
        {
            if (args.Length < 1)
            {
                player.SendChatMessage("Usage", "/joinsecradio {radioChannelName}");
                return;
            }
            
            if (!VoiceManager._voiceClients.TryGetValue(player, out VoiceClient voiceClient))
                return;

            VoiceManager.JoinRadioChannel(voiceClient, args[0], false);

            player.SendChatMessage("Radio", $"You joined secondary channel \"{args[0]}\".");
        }

        [Command("leaveradio")]
        private void OnLeaveRadioChannel(Player player, string[] args)
        {
            if (args.Length < 1)
            {
                player.SendChatMessage("Usage", "/leaveradio {radioChannelName}");
                return;
            }
            
            if (!VoiceManager._voiceClients.TryGetValue(player, out VoiceClient voiceClient))
                return;

            VoiceManager.LeaveRadioChannel(voiceClient, args[0]);

            player.SendChatMessage("Radio", $"You left channel \"{args[0]}\".");
        }
#endif
        #endregion

        #region Remote Events (Radio)
        [EventHandler(Event.SaltyChat_IsSending)]
        private void OnSendingOnRadio([FromSource] Player player, string radioChannelName, bool isSending)
        {
            if (!VoiceManager._voiceClients.TryGetValue(player, out VoiceClient voiceClient))
                return;

            RadioChannel radioChannel = VoiceManager.GetRadioChannel(radioChannelName, false);

            if (radioChannel == null || !radioChannel.IsMember(voiceClient))
                return;

            radioChannel.Send(voiceClient, isSending);
        }

        [EventHandler(Event.SaltyChat_SetRadioChannel)]
        private void OnJoinRadioChannel([FromSource] Player player, string radioChannelName, bool isPrimary)
        {
            if (!VoiceManager._voiceClients.TryGetValue(player, out VoiceClient voiceClient))
                return;

            VoiceManager.LeaveRadioChannel(voiceClient, isPrimary);

            if (!String.IsNullOrEmpty(radioChannelName))
            {
                VoiceManager.JoinRadioChannel(voiceClient, radioChannelName, isPrimary);
            }
        }
        #endregion

        #region Remote Events(Megaphoone)
        [EventHandler(Event.SaltyChat_IsUsingMegaphone)]
        private void OnIsUsingMegaphone([FromSource] Player player, bool isSending)
        {
            if (!VoiceManager._voiceClients.TryGetValue(player, out VoiceClient voiceClient))
                return;

            string positionJson = JsonConvert.SerializeObject(voiceClient.Player.Character.Position);
            float range = 100f;
            foreach (VoiceClient remoteClient in VoiceManager.VoiceClients)
            {
                remoteClient.Player.TriggerEvent(Event.SaltyChat_IsUsingMegaphone, voiceClient.Player.Handle, range, isSending, positionJson);
            }
        }
        #endregion

        #region Methods (Radio)
        public static RadioChannel GetRadioChannel(string name, bool create)
        {
            RadioChannel radioChannel;

            lock (VoiceManager._radioChannels)
            {
                radioChannel = VoiceManager.RadioChannels.FirstOrDefault(r => r.Name == name);

                if (radioChannel == null && create)
                {
                    radioChannel = new RadioChannel(name);

                    VoiceManager._radioChannels.Add(radioChannel);
                }
            }

            return radioChannel;
        }

        public static IEnumerable<RadioChannelMember> GetPlayerRadioChannelMembership(VoiceClient voiceClient)
        {
            foreach (RadioChannel radioChannel in VoiceManager.RadioChannels)
            {
                RadioChannelMember membership = radioChannel.Members.FirstOrDefault(m => m.VoiceClient == voiceClient);

                if (membership != null)
                {
                    yield return membership;
                }
            }
        }

        public static void SetRadioSpeaker(VoiceClient voiceClient, bool toggle)
        {
            voiceClient.RadioSpeaker = toggle;
        }

        public static void JoinRadioChannel(VoiceClient voiceClient, string radioChannelName, bool isPrimary)
        {
            foreach (RadioChannel channel in VoiceManager.RadioChannels)
            {
                if (channel.Members.Any(v => v.VoiceClient == voiceClient && v.IsPrimary == isPrimary))
                    return;
            }

            RadioChannel radioChannel = VoiceManager.GetRadioChannel(radioChannelName, true);

            radioChannel.AddMember(voiceClient, isPrimary);
        }

        public static void LeaveRadioChannel(VoiceClient voiceClient)
        {
            foreach (RadioChannelMember membership in VoiceManager.GetPlayerRadioChannelMembership(voiceClient))
            {
                membership.RadioChannel.RemoveMember(voiceClient);

                if (membership.RadioChannel.Members.Length == 0)
                {
                    lock (VoiceManager._radioChannels)
                    {
                        VoiceManager._radioChannels.Remove(membership.RadioChannel);
                    }
                }
            }
        }

        public static void LeaveRadioChannel(VoiceClient voiceClient, string radioChannelName)
        {
            foreach (RadioChannelMember membership in VoiceManager.GetPlayerRadioChannelMembership(voiceClient).Where(m => m.RadioChannel.Name == radioChannelName))
            {
                membership.RadioChannel.RemoveMember(voiceClient);

                if (membership.RadioChannel.Members.Length == 0)
                {
                    lock (VoiceManager._radioChannels)
                    {
                        VoiceManager._radioChannels.Remove(membership.RadioChannel);
                    }
                }
            }
        }

        public static void LeaveRadioChannel(VoiceClient voiceClient, bool primary)
        {
            foreach (RadioChannelMember membership in VoiceManager.GetPlayerRadioChannelMembership(voiceClient).Where(m => m.IsPrimary == primary))
            {
                membership.RadioChannel.RemoveMember(voiceClient);

                if (membership.RadioChannel.Members.Length == 0)
                {
                    lock (VoiceManager._radioChannels)
                    {
                        VoiceManager._radioChannels.Remove(membership.RadioChannel);
                    }
                }
            }
        }
        #endregion

        #region Methods (Misc)
        public static string GetTeamSpeakName()
        {
            string name;

            do
            {
                name = Guid.NewGuid().ToString().Replace("-", "");

                if (name.Length > 30)
                {
                    name = name.Remove(29, name.Length - 30);
                }
            }
            while (VoiceManager._voiceClients.Values.Any(c => c.TeamSpeakName == name));

            return name;
        }

        public static bool IsVersionAccepted(string branch, string version)
        {
            if (!String.IsNullOrWhiteSpace(VoiceManager.RequiredUpdateBranch) && VoiceManager.RequiredUpdateBranch != branch)
            {
                return false;
            }

            if (!String.IsNullOrWhiteSpace(VoiceManager.MinimumPluginVersion))
            {
                try
                {
                    string[] minimumVersionArray = VoiceManager.MinimumPluginVersion.Split('.');
                    string[] versionArray = version.Split('.');

                    int lengthCounter = 0;

                    if (versionArray.Length >= minimumVersionArray.Length)
                    {
                        lengthCounter = minimumVersionArray.Length;
                    }
                    else
                    {
                        lengthCounter = versionArray.Length;
                    }

                    for (int i = 0; i < lengthCounter; i++)
                    {
                        int min = Convert.ToInt32(minimumVersionArray[i]);
                        int cur = Convert.ToInt32(versionArray[i]);

                        if (cur > min)
                        {
                            return true;
                        }
                        else if (min > cur)
                        {
                            return false;
                        }
                    }
                }
                catch
                {
                    return false;
                }
            }

            return true;
        }
        #endregion
    }
}
