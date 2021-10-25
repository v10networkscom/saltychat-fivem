using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaltyClient
{
    #region Megaphone
    /// <summary>
    /// Used for <see cref="Command.MegaphoneCommunicationUpdate"/>
    /// </summary>
    public class MegaphoneCommunication
    {
        #region Properties
        public string Name { get; set; }
        public float Range { get; set; }
        public float? Volume { get; set; }
        #endregion

        #region CTOR
        public MegaphoneCommunication(string name, float range)
        {
            this.Name = name;
            this.Range = range;
        }
        public MegaphoneCommunication(string name, float range, float volume)
        {
            this.Name = name;
            this.Range = range;
            this.Volume = volume;
        }
        #endregion

        #region Conditional Property Serialization
        public bool ShouldSerializeVolume() => this.Volume.HasValue;
        #endregion
    }
    #endregion
}
