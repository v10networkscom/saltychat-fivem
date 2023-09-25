using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SaltyShared;
using CitizenFX.Core;
using CitizenFX.Server;
using CitizenFX.Server.Native;
using Newtonsoft.Json;

namespace SaltyServer
{
    public class VoiceManager : BaseScript
    {
        #region Properties / Fields
        public static VoiceManager Instance { get; private set; }

        public float[][] RadioTowers { get; private set; } = new float[0][];

        public VoiceClient[] VoiceClients => this._voiceClients.Values.ToArray();
        private Dictionary<Player, VoiceClient> _voiceClients = new Dictionary<Player, VoiceClient>();

        public PhoneCall[] PhoneCalls => this._phoneCalls.ToArray();
        private List<PhoneCall> _phoneCalls = new List<PhoneCall>();

        public RadioChannel[] RadioChannels => this._radioChannels.ToArray();
        private List<RadioChannel> _radioChannels = new List<RadioChannel>();

        public Configuration Configuration { get; private set; }

        internal PlayerList PlayerList { get; private set; } = new PlayerList();
        #endregion

        #region CTOR
        public VoiceManager()
        {
            VoiceManager.Instance = this;
        }
        #endregion

        #region Server Events
        [EventHandler("onResourceStart")]
        private void OnResourceStart(string resourceName)
        {
            if (resourceName != Natives.GetCurrentResourceName())
                return;

            this.Configuration = JsonConvert.DeserializeObject<Configuration>(Natives.LoadResourceFile(Natives.GetCurrentResourceName(), "config.json"));

            string onesyncState = Natives.GetConvar("onesync", "off");

            switch (onesyncState)
            {
                case "on":
                case "legacy":
                    {
                        break;
                    }
                default:
                    {
                        this.Configuration.VoiceEnabled = false;

                        Debug.WriteLine("OneSync is required for this script version. Please add \"+set onesync on\" to your server launch arguments.");

                        break;
                    }
            }
        }

        [EventHandler("onResourceStop")]
        private void OnResourceStop(string resourceName)
        {
            if (resourceName != Natives.GetCurrentResourceName())
                return;

            this.Configuration.VoiceEnabled = false;

            // Clear all voice clients
            lock (this._voiceClients)
            {
                this._voiceClients.Clear();
            }

            // Clear all phone calls
            lock (this._phoneCalls)
            {
                this._phoneCalls.Clear();
            }

            // Clear all radio channels
            lock (this._radioChannels)
            {
                this._radioChannels.Clear();
            }
        }

        [EventHandler("playerDropped")]
        private void OnPlayerDisconnected([Source] Player player, string reason)
        {
            // Return if player wasn't a registered voice client
            if (!this._voiceClients.TryGetValue(player, out VoiceClient voiceClient))
                return;

            // Remove player from all phone calls
            foreach (PhoneCall phoneCall in this.PhoneCalls.Where(c => c.IsMember(voiceClient)))
            {
                this.LeavePhoneCall(voiceClient, phoneCall);
            }

            // Remove player from all radio channels
            this.LeaveRadioChannel(voiceClient);

            // Tell all players to remove the player
            Events.TriggerAllClientsEvent(Event.SaltyChat_RemoveClient, player.Handle);
        }
        #endregion

        #region Exports (General)
        [Export("GetPlayerAlive")]
        private bool GetPlayerAlive(int netId)
        {
            Player player = this.PlayerList[netId];

            if (!this._voiceClients.TryGetValue(player, out VoiceClient voiceClient))
                return false;

            return voiceClient.IsAlive;
        }

        [Export("SetPlayerAlive")]
        private void SetPlayerAlive(int netId, bool isAlive)
        {
            Player player = this.PlayerList[netId];

            if (!this._voiceClients.TryGetValue(player, out VoiceClient voiceClient))
                return;

            voiceClient.IsAlive = isAlive;

            foreach (RadioChannelMember radioChannelMember in this.GetPlayerRadioChannelMembership(voiceClient).Where(m => m.IsSending))
            {
                radioChannelMember.RadioChannel.Send(voiceClient, false);
            }
        }

        [Export("GetPlayerVoiceRange")]
        private float GetPlayerVoiceRange(int netId)
        {
            Player player = this.PlayerList[netId];

            if (!this._voiceClients.TryGetValue(player, out VoiceClient voiceClient))
                return 0f;

            return voiceClient.VoiceRange;
        }

        [Export("SetPlayerVoiceRange")]
        private void SetPlayerVoiceRange(int netId, float voiceRange)
        {
            Player player = this.PlayerList[netId];

            if (!this._voiceClients.TryGetValue(player, out VoiceClient voiceClient))
                return;

            voiceClient.VoiceRange = voiceRange;
        }
        #endregion

        #region Exports (Phone)
        [Export("AddPlayerToCall")]
        private void AddPlayerToCall(string identifier, int playerHandle) => this.AddPlayersToCall(identifier, new List<dynamic>() { playerHandle });

        [Export("AddPlayersToCall")]
        private void AddPlayersToCall(string identifier, List<dynamic> players)
        {
            PhoneCall phoneCall = this.GetPhoneCall(identifier, true);

            foreach (int playerHandle in players)
            {
                VoiceClient voiceClient = this.VoiceClients.FirstOrDefault(c => c.Player.Handle == playerHandle);

                if (voiceClient == null)
                    continue;

                this.JoinPhoneCall(voiceClient, phoneCall);
            }
        }

        [Export("RemovePlayerFromCall")]
        private void RemovePlayerFromCall(string identifier, int playerHandle) => this.RemovePlayersFromCall(identifier, new List<dynamic>() { playerHandle });

        [Export("RemovePlayersFromCall")]
        private void RemovePlayersFromCall(string identifier, List<dynamic> players)
        {
            PhoneCall phoneCall = this.GetPhoneCall(identifier, false);

            if (phoneCall == null)
                return;

            foreach (int playerHandle in players)
            {
                VoiceClient voiceClient = this.VoiceClients.FirstOrDefault(c => c.Player.Handle == playerHandle);

                if (voiceClient == null)
                    continue;

                this.LeavePhoneCall(voiceClient, phoneCall);
            }
        }

        [Export("SetPlayerPhoneSpeaker")]
        private void SetPlayerPhoneSpeaker(int playerHandle, bool isEnabled)
        {
            Player player = this.PlayerList[playerHandle];

            if (!this._voiceClients.TryGetValue(player, out VoiceClient voiceClient))
                return;

            voiceClient.SetPhoneSpeakerEnabled(isEnabled);
        }

        [Obsolete]
        [Export("EstablishCall")]
        private void EstablishCall(int callerNetId, int partnerNetId)
        {
            VoiceClient caller = this.VoiceClients.FirstOrDefault(c => c.Player.Handle == callerNetId);
            VoiceClient callPartner = this.VoiceClients.FirstOrDefault(c => c.Player.Handle == partnerNetId);

            if (caller == null || callPartner == null)
                return;

            caller.TriggerEvent(Event.SaltyChat_EstablishCall, partnerNetId, callPartner.TeamSpeakName, callPartner.Player.GetPosition());
            callPartner.TriggerEvent(Event.SaltyChat_EstablishCall, callerNetId, caller.TeamSpeakName, caller.Player.GetPosition());
        }

        [Obsolete]
        [Export("EndCall")]
        private void EndCall(int callerNetId, int partnerNetId)
        {
            Player caller = this.PlayerList[callerNetId];
            Player callPartner = this.PlayerList[partnerNetId];

            caller.TriggerEvent(Event.SaltyChat_EndCall, callPartner.Handle);
            callPartner.TriggerEvent(Event.SaltyChat_EndCall, caller.Handle);
        }
        #endregion

        #region Exports (Radio)
        [Export("GetPlayersInRadioChannel")]
        private int[] GetPlayersInRadioChannel(string radioChannelName)
        {
            RadioChannel radioChannel = this.GetRadioChannel(radioChannelName, false);

            if (radioChannel == null)
                return new int[0];

            return radioChannel.Members.Select(m => m.VoiceClient.Player.Handle).ToArray();
        }

        [Export("SetPlayerRadioSpeaker")]
        private void SetPlayerRadioSpeaker(int netId, bool toggle)
        {
            Player player = this.PlayerList[netId];

            if (!this._voiceClients.TryGetValue(player, out VoiceClient voiceClient))
                return;

            voiceClient.IsRadioSpeakerEnabled = toggle;
        }

        [Export("SetPlayerRadioChannel")]
        private void SetPlayerRadioChannel(int netId, string radioChannelName, bool isPrimary)
        {
            Player player = this.PlayerList[netId];

            if (!this._voiceClients.TryGetValue(player, out VoiceClient voiceClient))
                return;

            this.JoinRadioChannel(voiceClient, radioChannelName, isPrimary);
        }

        [Export("RemovePlayerRadioChannel")]
        private void RemovePlayerRadioChannel(int netId, string radioChannelName)
        {
            Player player = this.PlayerList[netId];

            if (!this._voiceClients.TryGetValue(player, out VoiceClient voiceClient))
                return;

            this.LeaveRadioChannel(voiceClient, radioChannelName);
        }

        [Export("SetRadioTowers")]
        private void SetRadioTowers(dynamic towers)
        {
            List<float[]> radioTowers = new List<float[]>();

            foreach (dynamic tower in towers)
            {
                if (tower.GetType() == typeof(Vector3))
                    radioTowers.Add(new float[] { tower.X, tower.Y, tower.Z });
                else if (tower.Length == 3)
                    radioTowers.Add(new float[] { (float)tower[0], (float)tower[1], (float)tower[2] });
                else if (tower.Length == 4)
                    radioTowers.Add(new float[] { (float)tower[0], (float)tower[1], (float)tower[2], (float)tower[3] });
            }

            this.RadioTowers = radioTowers.ToArray();

            Events.TriggerAllClientsEvent(Event.SaltyChat_UpdateRadioTowers, this.RadioTowers.ToList());
        }
        #endregion

        #region Remote Events (Salty Chat)
        [EventHandler(Event.SaltyChat_Initialize)]
        private void OnInitialize([Source] Player player)
        {
            if (!this.Configuration.VoiceEnabled)
                return;

            VoiceClient voiceClient;

            lock (this._voiceClients)
            {
                string playerName = this.GetTeamSpeakName(player);

                if (String.IsNullOrWhiteSpace(playerName))
                {
                    Debug.WriteLine($"Failed to generate a unique name for player {player.Handle}. Ensure that you use a unique name pattern in your config.json.");
                    return;
                }

                voiceClient = new VoiceClient(player, playerName, this.Configuration.VoiceRanges[1], true);

                if (this._voiceClients.ContainsKey(player))
                    this._voiceClients[player] = voiceClient;
                else
                    this._voiceClients.Add(player, voiceClient);
            }

            player.TriggerEvent(Event.SaltyChat_Initialize, voiceClient.TeamSpeakName, voiceClient.VoiceRange, this.RadioTowers.ToList());
        }

        [EventHandler(Event.SaltyChat_CheckVersion)]
        private void OnCheckVersion([Source] Player player, string version)
        {
            if (!this._voiceClients.TryGetValue(player, out _))
                return;

            if (!this.IsVersionAccepted(version))
            {
                player.Drop($"[Salty Chat] You need to have version {this.Configuration.MinimumPluginVersion} or later.");
                return;
            }
        }
        #endregion

        #region Commands (General/Proximity)
#if DEBUG
        [Command("setalive", RemapParameters = true)]
        private void OnSetAlive([Source] Player player, string[] args)
        {
            if (!this._voiceClients.TryGetValue(player, out VoiceClient voiceClient))
            {
                return;
            }
            else if (args.Length < 1)
            {
                player.SendChatMessage("Usage", "/setalive {true/false}");
                return;
            }

            bool isAlive = String.Equals(args[0], "true", StringComparison.OrdinalIgnoreCase);

            this.SetPlayerAlive(player.Handle, isAlive);

            player.SendChatMessage("General", $"You are now {(isAlive ? "alive" : "dead")}");
        }
#endif
        #endregion

        #region Commands (Phone)
#if DEBUG
        [Command("joincall", RemapParameters = true)]
        private void OnJoinPhoneCall([Source] Player player, string[] args)
        {
            if (!this._voiceClients.TryGetValue(player, out VoiceClient voiceClient))
            {
                return;
            }
            else if (args.Length < 1)
            {
                player.SendChatMessage("Usage", "/joincall {identifier}");
                return;
            }

            string identifier = args[0];

            this.JoinPhoneCall(voiceClient, identifier);

            player.SendChatMessage("PhoneCall", $"Joined call {identifier}");
        }

        [Command("leavecall", RemapParameters = true)]
        private void OnLeavePhoneCall([Source] Player player, string[] args)
        {
            if (!this._voiceClients.TryGetValue(player, out VoiceClient voiceClient))
            {
                return;
            }
            else if (args.Length < 1)
            {
                player.SendChatMessage("Usage", "/leavecall {identifier}");
                return;
            }

            string identifier = args[0];

            this.LeavePhoneCall(voiceClient, identifier);

            player.SendChatMessage("PhoneCall", $"Left call {identifier}");
        }

        [Command("setphonespeaker", RemapParameters = true)]
        private void OnSetPhoneSpeaker([Source] Player player, string[] args)
        {
            if (args.Length < 1)
            {
                player.SendChatMessage("Usage", "/setphonespeaker {true/false}");
                return;
            }

            if (!this._voiceClients.TryGetValue(player, out VoiceClient voiceClient))
                return;

            bool toggle = String.Equals(args[0], "true", StringComparison.OrdinalIgnoreCase);

            voiceClient.SetPhoneSpeakerEnabled(toggle);

            player.SendChatMessage("PhoneSpeaker", $"The speaker is now {(toggle ? "on" : "off")}.");
        }
#endif
        #endregion

        #region Commands (Radio)
#if DEBUG
        [Command("setradiospeaker", RemapParameters = true)]
        private void OnSetRadioSpeaker([Source] Player player, string[] args)
        {
            if (args.Length < 1)
            {
                player.SendChatMessage("Usage", "/radiospeaker {true/false}");
                return;
            }

            if (!this._voiceClients.TryGetValue(player, out VoiceClient voiceClient))
                return;

            bool toggle = String.Equals(args[0], "true", StringComparison.OrdinalIgnoreCase);

            voiceClient.IsRadioSpeakerEnabled = toggle;

            player.SendChatMessage("RadioSpeaker", $"The speaker is now {(toggle ? "on" : "off")}.");
        }

        [Command("joinradio", RemapParameters = true)]
        private void OnJoinRadioChannel([Source] Player player, string[] args)
        {
            if (args.Length < 1)
            {
                player.SendChatMessage("Usage", "/joinradio {radioChannelName}");
                return;
            }

            if (!this._voiceClients.TryGetValue(player, out VoiceClient voiceClient))
                return;

            this.JoinRadioChannel(voiceClient, args[0], true);

            player.SendChatMessage("Radio", $"You joined channel \"{args[0]}\".");
        }

        [Command("joinsecradio", RemapParameters = true)]
        private void OnJoinSecondaryRadioChannel([Source] Player player, string[] args)
        {
            if (args.Length < 1)
            {
                player.SendChatMessage("Usage", "/joinsecradio {radioChannelName}");
                return;
            }

            if (!this._voiceClients.TryGetValue(player, out VoiceClient voiceClient))
                return;

            this.JoinRadioChannel(voiceClient, args[0], false);

            player.SendChatMessage("Radio", $"You joined secondary channel \"{args[0]}\".");
        }

        [Command("leaveradio", RemapParameters = true)]
        private void OnLeaveRadioChannel([Source] Player player, string[] args)
        {
            if (args.Length < 1)
            {
                player.SendChatMessage("Usage", "/leaveradio {radioChannelName}");
                return;
            }

            if (!this._voiceClients.TryGetValue(player, out VoiceClient voiceClient))
                return;

            this.LeaveRadioChannel(voiceClient, args[0]);

            player.SendChatMessage("Radio", $"You left channel \"{args[0]}\".");
        }
#endif
        #endregion

        #region Remote Events (Radio)
        [EventHandler(Event.SaltyChat_IsSending)]
        private void OnSendingOnRadio([Source] Player player, string radioChannelName, bool isSending)
        {
            if (!this._voiceClients.TryGetValue(player, out VoiceClient voiceClient))
                return;

            RadioChannel radioChannel = this.GetRadioChannel(radioChannelName, false);

            if (radioChannel == null || !radioChannel.IsMember(voiceClient))
                return;

            radioChannel.Send(voiceClient, isSending);
        }

        [EventHandler(Event.SaltyChat_SetRadioChannel)]
        private void OnJoinRadioChannel([Source] Player player, string radioChannelName, bool isPrimary)
        {
            if (!this._voiceClients.TryGetValue(player, out VoiceClient voiceClient))
                return;

            this.LeaveRadioChannel(voiceClient, isPrimary);

            if (!String.IsNullOrEmpty(radioChannelName))
            {
                this.JoinRadioChannel(voiceClient, radioChannelName, isPrimary);
            }
        }

        [EventHandler(Event.SaltyChat_SetRadioSpeaker)]
        private void OnSetRadioSpeaker([Source] Player player, bool isRadioSpeakerEnabled)
        {
            if (!this._voiceClients.TryGetValue(player, out VoiceClient voiceClient))
                return;

            voiceClient.IsRadioSpeakerEnabled = isRadioSpeakerEnabled;
        }
        #endregion

        #region Methods (Phone)
        public PhoneCall GetPhoneCall(string identifier, bool create)
        {
            PhoneCall phoneCall;

            lock (this._phoneCalls)
            {
                phoneCall = this.PhoneCalls.FirstOrDefault(r => r.Identifier == identifier);

                if (phoneCall == null && create)
                {
                    phoneCall = new PhoneCall(identifier);

                    this._phoneCalls.Add(phoneCall);
                }
            }

            return phoneCall;
        }

        public void JoinPhoneCall(VoiceClient voiceClient, string identifier)
        {
            PhoneCall phoneCall = this.GetPhoneCall(identifier, true);

            this.JoinPhoneCall(voiceClient, phoneCall);
        }

        public void JoinPhoneCall(VoiceClient voiceClient, PhoneCall phoneCall)
        {
            phoneCall.AddMember(voiceClient);
        }

        public void LeavePhoneCall(VoiceClient voiceClient, string identifier)
        {
            PhoneCall phoneCall = this.GetPhoneCall(identifier, false);

            if (phoneCall != null)
                this.LeavePhoneCall(voiceClient, phoneCall);
        }

        public void LeavePhoneCall(VoiceClient voiceClient, PhoneCall phoneCall)
        {
            phoneCall.RemoveMember(voiceClient);

            if (phoneCall.Members.Length == 0)
            {
                lock (this._phoneCalls)
                {
                    this._phoneCalls.Remove(phoneCall);
                }
            }
        }

        public IEnumerable<PhoneCallMember> GetPlayerPhoneCallMembership(VoiceClient voiceClient)
        {
            foreach (PhoneCall phoneCall in this.PhoneCalls)
            {
                PhoneCallMember membership = phoneCall.Members.FirstOrDefault(m => m.VoiceClient == voiceClient);

                if (membership != null)
                {
                    yield return membership;
                }
            }
        }
        #endregion

        #region Methods (Radio)
        public RadioChannel GetRadioChannel(string name, bool create)
        {
            RadioChannel radioChannel;

            lock (this._radioChannels)
            {
                radioChannel = this.RadioChannels.FirstOrDefault(r => r.Name == name);

                if (radioChannel == null && create)
                {
                    radioChannel = new RadioChannel(name);

                    this._radioChannels.Add(radioChannel);
                }
            }

            return radioChannel;
        }

        public IEnumerable<RadioChannelMember> GetPlayerRadioChannelMembership(VoiceClient voiceClient)
        {
            foreach (RadioChannel radioChannel in this.RadioChannels)
            {
                RadioChannelMember membership = radioChannel.Members.FirstOrDefault(m => m.VoiceClient == voiceClient);

                if (membership != null)
                {
                    yield return membership;
                }
            }
        }

        public void JoinRadioChannel(VoiceClient voiceClient, string radioChannelName, bool isPrimary)
        {
            foreach (RadioChannel channel in this.RadioChannels)
            {
                if (channel.Members.Any(v => v.VoiceClient == voiceClient && v.IsPrimary == isPrimary))
                    return;
            }

            RadioChannel radioChannel = this.GetRadioChannel(radioChannelName, true);

            radioChannel.AddMember(voiceClient, isPrimary);
        }

        public void LeaveRadioChannel(VoiceClient voiceClient)
        {
            foreach (RadioChannelMember membership in this.GetPlayerRadioChannelMembership(voiceClient))
            {
                this.LeaveRadioChannel(voiceClient, membership.RadioChannel);
            }
        }

        public void LeaveRadioChannel(VoiceClient voiceClient, string radioChannelName)
        {
            foreach (RadioChannelMember membership in this.GetPlayerRadioChannelMembership(voiceClient).Where(m => m.RadioChannel.Name == radioChannelName))
            {
                this.LeaveRadioChannel(voiceClient, membership.RadioChannel);
            }
        }

        public void LeaveRadioChannel(VoiceClient voiceClient, bool primary)
        {
            foreach (RadioChannelMember membership in this.GetPlayerRadioChannelMembership(voiceClient).Where(m => m.IsPrimary == primary))
            {
                this.LeaveRadioChannel(voiceClient, membership.RadioChannel);
            }
        }

        public void LeaveRadioChannel(VoiceClient voiceClient, RadioChannel radioChannel)
        {
            radioChannel.RemoveMember(voiceClient);

            if (radioChannel.Members.Length == 0)
            {
                lock (this._radioChannels)
                {
                    this._radioChannels.Remove(radioChannel);
                }
            }
        }
        #endregion

        #region Methods (Misc)
        internal void SetStateBagKey(string key, object value)
        {
            StateBag.Global.Set(key, value, true);
        }

        internal object GetStateBagKey(string key)
        {
            return StateBag.Global[key];
        }

        public string GetTeamSpeakName(Player player)
        {
            string name = this.Configuration.NamePattern;
            byte counter = 0;

            do
            {
                if (++counter > 5)
                    return null;

                name = Regex.Replace(name, @"(\{serverid\})", player.Handle.ToString());
                name = Regex.Replace(name, @"(\{playername\})", player.Name ?? String.Empty);
                name = Regex.Replace(name, @"(\{guid\})", Guid.NewGuid().ToString().Replace("-", ""));

                if (name.Length > 30)
                    name = name.Remove(29, name.Length - 30);
            }
            while (this._voiceClients.Values.Any(c => c.TeamSpeakName == name));

            return name;
        }

        public bool IsVersionAccepted(string version)
        {
            if (!String.IsNullOrWhiteSpace(this.Configuration.MinimumPluginVersion))
            {
                try
                {
                    string[] minimumVersionArray = this.Configuration.MinimumPluginVersion.Split('.');
                    string[] versionArray = version.Split('.');

                    int lengthCounter = 0;

                    if (versionArray.Length >= minimumVersionArray.Length)
                        lengthCounter = minimumVersionArray.Length;
                    else
                        lengthCounter = versionArray.Length;

                    for (int i = 0; i < lengthCounter; i++)
                    {
                        int min = Convert.ToInt32(minimumVersionArray[i]);
                        int cur = 0;

                        // regex match so we can have versions like 2.2.6p1
                        Match match = Regex.Match(versionArray[i], "^(\\d+)");

                        if (match.Success)
                            cur = Convert.ToInt32(match.Value);

                        if (cur > min)
                            return true;
                        else if (min > cur)
                            return false;
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
