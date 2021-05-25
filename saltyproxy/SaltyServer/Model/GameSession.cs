using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaltyServer
{
    public class GameSession
    {
        public Guid SessionId { get; set; }
        public string IpAddress { get; set; }
        public string TeamSpeakUid { get; set; }
        public string WebSocketEndpoint { get; set; }

        public GameSession(string ipAdress, string teamSpeakUid, string webSocketEndpoint)
        {
            this.SessionId = Guid.Empty;
            this.IpAddress = ipAdress;
            this.TeamSpeakUid = teamSpeakUid;
            this.WebSocketEndpoint = webSocketEndpoint;
        }

        public GameSession(Guid sessionId, string ipAdress, string teamSpeakUid, string webSocketEndpoint)
        {
            this.SessionId = sessionId;
            this.IpAddress = ipAdress;
            this.TeamSpeakUid = teamSpeakUid;
            this.WebSocketEndpoint = webSocketEndpoint;
        }

        public bool ShouldSerializeSessionId() => this.SessionId != Guid.Empty;
    }
}
