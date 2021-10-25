using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaltyClient
{
    /// <summary>
    /// Will be received from the WebSocket if the player (dis-)connects to the specified TeamSpeak server or channel
    /// </summary>
    public class InstanceState
    {
        [Obsolete]
        public bool IsConnectedToServer { get; set; }
        [Obsolete]
        public bool IsReady { get; set; }
        public GameInstanceState State { get; set; }
    }

    public enum GameInstanceState
    {
        NotInitiated = -1,
        NotConnected = 0,
        Connected = 1,
        Ingame = 2,
        InSwissChannel = 3,
    }
}
