using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaltyClient
{
    #region Phone
    /// <summary>
    /// Used for <see cref="Command.PhoneCommunicationUpdate"/> and <see cref="Command.StopPhoneCommunication"/>
    /// </summary>
    public class PhoneCommunication
    {
        #region Properties
        public string Name { get; set; }
        public int? SignalStrength { get; set; }
        public float? Volume { get; set; }

        public bool Direct { get; set; }
        public string[] RelayedBy { get; set; }
        #endregion

        #region CTOR
        public PhoneCommunication(string name)
        {
            this.Name = name;
        }

        public PhoneCommunication(string name, int signalStrength)
        {
            this.Name = name;
            this.SignalStrength = signalStrength;

            this.Direct = true;
        }

        public PhoneCommunication(string name, int signalStrength, float volume)
        {
            this.Name = name;
            this.SignalStrength = signalStrength;
            this.Volume = volume;

            this.Direct = true;
        }

        public PhoneCommunication(string name, int signalStrength, bool direct, string[] relayedBy)
        {
            this.Name = name;
            this.SignalStrength = signalStrength;

            this.Direct = direct;
            this.RelayedBy = relayedBy;
        }

        public PhoneCommunication(string name, int signalStrength, float volume, bool direct, string[] relayedBy)
        {
            this.Name = name;
            this.SignalStrength = signalStrength;
            this.Volume = volume;

            this.Direct = direct;
            this.RelayedBy = relayedBy;
        }
        #endregion

        #region Conditional Property Serialization
        public bool ShouldSerializeSignalStrength() => this.SignalStrength.HasValue;

        public bool ShouldSerializeVolume() => this.Volume.HasValue;

        public bool ShouldSerializeDirect() => this.Direct;

        public bool ShouldSerializeRelayedBy() => this.RelayedBy != null && this.RelayedBy.Length > 0;
        #endregion
    }
    #endregion
}
