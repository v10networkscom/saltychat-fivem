using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaltyClient
{
    /// <summary>
    /// Used for <see cref="Command.Initiate"/>
    /// </summary>
    public class GameInstance
    {
        #region Properties
        /// <summary>
        /// Unique id of the server the player must be connected to
        /// </summary>
        public string ServerUniqueIdentifier { get; set; }

        /// <summary>
        /// TeamSpeak name that should be set (max length is 30)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Id of the TeamSpeak channel the player should be moved to
        /// </summary>
        public ulong ChannelId { get; set; }

        /// <summary>
        /// Password of the TeamSpeak channel
        /// </summary>
        public string ChannelPassword { get; set; }

        /// <summary>
        /// Foldername of the sound pack that will be used (%AppData%\TS3Client\Plugins\SaltyChat\{SoundPack}\)
        /// </summary>
        public string SoundPack { get; set; }

        /// <summary>
        /// IDs of channels which the player can join, while the game instace is running
        /// </summary>
        public ulong[] SwissChannelIds { get; set; }

        /// <summary>
        /// <see cref="false"/> if TalkState's shouldn't be send for other players to reduce events
        /// </summary>
        public bool SendTalkStates { get; set; }

        /// <summary>
        /// <see cref="true"/> to receive events for radio traffic state changes
        /// </summary>
        public bool SendRadioTrafficStates { get; set; }

        /// <summary>
        /// Maximum range of USR radio mode
        /// </summary>
        public float UltraShortRangeDistance { get; set; }

        /// <summary>
        /// Maximum range of SR radio mode
        /// </summary>
        public float ShortRangeDistance { get; set; }

        /// <summary>
        /// Maximum range of LR radio mode
        /// </summary>
        public float LongRangeDistace { get; set; }
        #endregion

        #region CTOR
        public GameInstance(string serverUniqueIdentifier, string name, ulong channelId, string channelPassword, string soundPack, ulong[] swissChannels, bool sendTalkStates, bool sendRadioTrafficStates, float ultraShortRangeDistance, float shortRangeDistance, float longRangeDistace)
        {
            this.ServerUniqueIdentifier = serverUniqueIdentifier;
            this.Name = name;
            this.ChannelId = channelId;
            this.ChannelPassword = channelPassword;
            this.SoundPack = soundPack;
            this.SwissChannelIds = swissChannels;
            this.SendTalkStates = sendTalkStates;
            this.SendRadioTrafficStates = sendRadioTrafficStates;
            this.UltraShortRangeDistance = ultraShortRangeDistance;
            this.ShortRangeDistance = shortRangeDistance;
            this.LongRangeDistace = longRangeDistace;
        }
        #endregion
    }
}
