using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaltyClient
{
    #region SelfState
    /// <summary>
    /// Used for <see cref="Command.SelfStateUpdate"/>
    /// </summary>
    public class SelfState
    {
        #region Sub Classes
        public class EchoEffect
        {
            public int Duration { get; set; }
            public float Rolloff { get; set; }
            public int Delay { get; set; }

            public EchoEffect(int duration = 100, float rolloff = 0.3f, int delay = 250)
            {
                this.Duration = duration;
                this.Rolloff = rolloff;
                this.Delay = delay;
            }
        }
        #endregion

        #region Properties
        public TSVector Position { get; set; }
        public float Rotation { get; set; }
        public float VoiceRange { get; set; }
        public bool IsAlive { get; set; }
        public EchoEffect Echo { get; set; }
        #endregion

        #region CTOR
        public SelfState(CitizenFX.Core.Vector3 position, float rotation, float voiceRange, bool isAlive, bool echo = false)
        {
            this.Position = new TSVector(position.X, position.Y, position.Z);
            this.Rotation = rotation;
            this.VoiceRange = voiceRange;
            this.IsAlive = isAlive;

            if (echo)
                this.Echo = new EchoEffect();
        }
        #endregion

        #region Conditional Property Serialization
        public bool ShouldSerializeIsAlive() => !this.IsAlive;
        public bool ShouldSerializeEcho() => this.Echo != null;
        #endregion
    }
    #endregion

    #region PlayerState
    /// <summary>
    /// Used for <see cref="Command.PlayerStateUpdate"/>
    /// </summary>
    public class PlayerState
    {
        #region Sub Classes
        public class MuffleEffect
        {
            public int Intensity { get; set; }

            public MuffleEffect(int intensity)
            {
                this.Intensity = intensity;
            }
        }
        #endregion

        #region Properties
        public string Name { get; set; }
        public TSVector Position { get; set; }
        public float VoiceRange { get; set; }
        public bool IsAlive { get; set; }
        public float? VolumeOverride { get; set; }
        public bool DistanceCulled { get; set; }
        public MuffleEffect Muffle { get; set; }
        #endregion

        #region CTOR
        /// <summary>
        /// Used for <see cref="Command.RemovePlayer"/>
        /// </summary>
        /// <param name="name"></param>
        public PlayerState(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// Used for <see cref="Command.PlayerStateUpdate"/>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="position"></param>
        /// <param name="voiceRange"></param>
        /// <param name="isAlive"></param>
        /// <param name="distanceCulled"></param>
        /// <param name="muffleIntensity"></param>
        public PlayerState(string name, CitizenFX.Core.Vector3 position, float voiceRange, bool isAlive, bool distanceCulled = false, int? muffleIntensity = null)
        {
            this.Name = name;
            this.Position = new TSVector(position.X, position.Y, position.Z);
            this.VoiceRange = voiceRange;
            this.IsAlive = isAlive;
            this.DistanceCulled = distanceCulled;

            if (muffleIntensity.HasValue)
                this.Muffle = new MuffleEffect(muffleIntensity.Value);
        }

        /// <summary>
        /// Used for <see cref="Command.PlayerStateUpdate"/> with volume override
        /// </summary>
        /// <param name="name"></param>
        /// <param name="position"></param>
        /// <param name="voiceRange"></param>
        /// <param name="isAlive"></param>
        /// <param name="volumeOverride">Overrides the volume (phone, radio and proximity) - from 0 (0%) to 1.6 (160%)</param>
        public PlayerState(string name, CitizenFX.Core.Vector3 position, float voiceRange, bool isAlive, float volumeOverride)
        {
            this.Name = name;
            this.Position = new TSVector(position.X, position.Y, position.Z);
            this.VoiceRange = voiceRange;
            this.IsAlive = isAlive;

            if (volumeOverride > 1.6f)
                this.VolumeOverride = 1.6f;
            else if (volumeOverride < 0f)
                this.VolumeOverride = 0f;
            else
                this.VolumeOverride = volumeOverride;
        }
        #endregion

        #region Conditional Property Serialization
        public bool ShouldSerializeName() => !String.IsNullOrEmpty(this.Name);

        public bool ShouldSerializeIsAlive() => !this.IsAlive;

        public bool ShouldSerializeVolumeOverride() => this.VolumeOverride.HasValue;

        public bool ShouldSerializeDistanceCulled() => this.DistanceCulled;

        public bool ShouldSerializeMuffle() => this.Muffle != null;
        #endregion
    }
    #endregion

    #region BulkUpdate
    /// <summary>
    /// Used for <see cref="Command.BulkUpdate"/>
    /// </summary>
    public class BulkUpdate
    {
        public ICollection<PlayerState> PlayerStates { get; set; }
        public SelfState SelfState { get; set; }

        public BulkUpdate(ICollection<PlayerState> playerStates, SelfState selfState)
        {
            this.PlayerStates = playerStates;
            this.SelfState = selfState;
        }
    }
    #endregion
}
