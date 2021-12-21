using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SaltyShared;
using Newtonsoft.Json;

namespace SaltyServer
{
    public class RadioChannel
    {
        #region Props/Fields
        internal string Name { get; }

        internal RadioChannelMember[] Members => this._members.ToArray();
        private List<RadioChannelMember> _members = new List<RadioChannelMember>();

        private object _memberLock = new object();
        #endregion

        #region CTOR
        internal RadioChannel(string name, params RadioChannelMember[] members)
        {
            this.Name = name;

            if (members != null)
                this._members.AddRange(members);
        }
        #endregion

        #region Methods
        internal bool IsMember(VoiceClient voiceClient)
        {
            return this.Members.Any(m => m.VoiceClient == voiceClient);
        }

        internal void AddMember(VoiceClient voiceClient, bool isPrimary)
        {
            lock (this._memberLock)
            {
                if (!this._members.Any(m => m.VoiceClient == voiceClient))
                {
                    this._members.Add(new RadioChannelMember(this, voiceClient, isPrimary));

                    voiceClient.TriggerEvent(Event.SaltyChat_SetRadioChannel, this.Name, isPrimary);

                    this.UpdateMemberStateBag();
                }
            }
        }

        internal void RemoveMember(VoiceClient voiceClient)
        {
            lock (this._memberLock)
            {
                RadioChannelMember member = this._members.FirstOrDefault(m => m.VoiceClient == voiceClient);

                if (member != null)
                {
                    this._members.Remove(member);

                    voiceClient.TriggerEvent(Event.SaltyChat_SetRadioChannel, null, member.IsPrimary);

                    if (member.IsSending)
                        this.UpdateSenderStateBag();

                    this.UpdateMemberStateBag();
                }
            }
        }

        internal void Send(VoiceClient voiceClient, bool isSending)
        {
            if (!this.TryGetMember(voiceClient, out RadioChannelMember radioChannelMember))
                return;

            if (VoiceManager.Instance.Configuration.EnableRadioHardcoreMode && isSending && this.Members.Any(m => m.VoiceClient != voiceClient && m.IsSending))
            {
                voiceClient.TriggerEvent(Event.SaltyChat_ChannelInUse, this.Name);

                return;
            }

            if (!voiceClient.IsAlive && isSending)
                return;

            radioChannelMember.IsSending = isSending;

            this.UpdateSenderStateBag();
        }
        #endregion

        #region Helper
        internal bool TryGetMember(VoiceClient voiceClient, out RadioChannelMember radioChannelMember)
        {
            radioChannelMember = this.Members.FirstOrDefault(m => m.VoiceClient == voiceClient);

            return radioChannelMember != null;
        }

        private void UpdateMemberStateBag()
        {
            VoiceManager.Instance.SetStateBagKey($"{State.SaltyChat_RadioChannelMember}:{this.Name}", this.Members.Select(m => m.VoiceClient.TeamSpeakName).ToArray());
        }

        private void UpdateSenderStateBag()
        {
            List<object> sender = new List<object>();

            foreach (RadioChannelMember sendingMember in this.Members.Where(m => m.IsSending))
            {
                sender.Add(
                    new
                    {
                        ServerId = sendingMember.VoiceClient.Player.GetServerId(),
                        Name = sendingMember.VoiceClient.TeamSpeakName,
                        Position = sendingMember.VoiceClient.Player.GetPosition()
                    }
                );
            }

            VoiceManager.Instance.SetStateBagKey($"{State.SaltyChat_RadioChannelSender}:{this.Name}", sender);
        }

        private void BroadcastEvent(string eventName, params object[] eventParams)
        {
            foreach (RadioChannelMember member in this.Members)
            {
                member.VoiceClient.TriggerEvent(eventName, eventParams);
            }
        }
        #endregion
    }

    public class RadioChannelMember
    {
        internal RadioChannel RadioChannel { get; }
        internal VoiceClient VoiceClient { get; }
        internal bool IsPrimary { get; }
        internal bool IsSending { get; set; }

        internal RadioChannelMember(RadioChannel radioChannel, VoiceClient voiceClient, bool isPrimary)
        {
            this.RadioChannel = radioChannel;
            this.VoiceClient = voiceClient;
            this.IsPrimary = isPrimary;
        }
    }
}
