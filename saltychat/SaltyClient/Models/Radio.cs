using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaltyClient
{
    /// <summary>
    /// Used for <see cref="Command.RadioTowerUpdate"/>
    /// </summary>
    public class RadioTower
    {
        public Tower[] Towers { get; set; }

        public RadioTower(Tower[] towers)
        {
            this.Towers = towers;
        }
    }

    public class Tower
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Range { get; set; }

        public Tower(float x, float y, float z, float range = 8000f)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.Range = range;
        }
    }

    /// <summary>
    /// Used for <see cref="Command.RadioCommunicationUpdate"/>
    /// </summary>
    public class RadioCommunication
    {
        #region Properties
        public string Name { get; set; }
        public RadioType SenderRadioType { get; set; }
        public RadioType OwnRadioType { get; set; }
        public bool PlayMicClick { get; set; }
        public float? Volume { get; set; }

        public bool Direct { get; set; }
        public bool Secondary { get; set; }
        public string[] RelayedBy { get; set; }
        #endregion

        #region CTOR
        public RadioCommunication(string name, RadioType senderRadioType, RadioType ownRadioType, bool playMicClick, bool direct, bool isSecondary)
        {
            this.Name = name;
            this.SenderRadioType = senderRadioType;
            this.OwnRadioType = ownRadioType;
            this.PlayMicClick = playMicClick;

            this.Direct = direct;
            this.Secondary = isSecondary;
        }

        public RadioCommunication(string name, RadioType senderRadioType, RadioType ownRadioType, bool playMicClick, bool direct, bool isSecondary, string[] relayedBy, float volume)
        {
            this.Name = name;
            this.SenderRadioType = senderRadioType;
            this.OwnRadioType = ownRadioType;
            this.PlayMicClick = playMicClick;

            this.Direct = direct;
            this.Secondary = isSecondary;
            this.RelayedBy = relayedBy;

            if (volume != 1f)
                this.Volume = volume;
        }
        #endregion

        #region Conditional Property Serialization
        public bool ShouldSerializePlayMicClick() => this.PlayMicClick;

        public bool ShouldSerializeVolume() => this.Volume.HasValue;

        public bool ShouldSerializeDirect() => this.Direct;

        public bool ShouldSerializeSecondary() => this.Secondary;

        public bool ShouldSerializeRelayedBy() => this.RelayedBy != null && this.RelayedBy.Length > 0;
        #endregion
    }

    /// <summary>
    /// User for <see cref="Command.AddRadioChannelMember"/> and <see cref="Command.RemoveRadioChannelMember"/>
    /// </summary>
    public class RadioChannelMember
    {
        public string PlayerName { get; set; }
        public bool IsPrimaryChannel { get; set; }
    }

    /// <summary>
    /// Used for <see cref="Command.UpdateRadioChannelMembers"/>
    /// </summary>
    public class RadioChannelMemberUpdate
    {
        public string[] PlayerNames { get; set; }
        public bool IsPrimaryChannel { get; set; }

        public RadioChannelMemberUpdate(string[] members, bool isPrimary)
        {
            this.PlayerNames = members;
            this.IsPrimaryChannel = isPrimary;
        }
    }

    /// <summary>
    /// Sent by the plugin through <see cref="Command.RadioTrafficState"/>
    /// </summary>
    public class RadioTrafficState
    {
        #region Props/Fields
        public string Name { get; set; }
        public bool IsSending { get; set; }
        public bool IsPrimaryChannel { get; set; }
        public string ActiveRelay { get; set; }
        #endregion
    }

    public class RadioTraffic
    {
        #region Props/Fields
        public string Name { get; set; }
        public bool IsSending { get; set; }
        public string RadioChannelName { get; set; }
        public RadioType SenderRadioType { get; set; }
        public RadioType ReceiverRadioType { get; set; }
        public string[] Relays { get; set; }
        #endregion

        #region CTOR
        public RadioTraffic(string playerName, bool isSending, string radioChannelName, RadioType senderType, RadioType receiverType, string[] relays)
        {
            this.Name = playerName;
            this.IsSending = isSending;
            this.RadioChannelName = radioChannelName;
            this.SenderRadioType = senderType;
            this.ReceiverRadioType = receiverType;
            this.Relays = relays;
        }
        #endregion
    }
}
