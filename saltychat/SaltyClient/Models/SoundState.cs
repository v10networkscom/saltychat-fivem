using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaltyClient
{
    /// <summary>
    /// Will be received from the WebSocket on every microphone and sound state change
    /// </summary>
    public class SoundState
    {
        public bool IsMicrophoneMuted { get; set; }
        public bool IsMicrophoneEnabled { get; set; }
        public bool IsSoundMuted { get; set; }
        public bool IsSoundEnabled { get; set; }
    }
}
