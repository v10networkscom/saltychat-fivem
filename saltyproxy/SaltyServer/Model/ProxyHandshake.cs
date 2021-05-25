using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaltyServer
{
    public class ProxyHandshake
    {
        public enum ClientType
        {
            None = 0,
            Game = 1,
            Plugin = 2
        }

        public Guid SessionId { get; set; }
        public ClientType Type { get; set; }
        public int ServerId { get; set; }

        public ProxyHandshake()
        {

        }

        public ProxyHandshake(int serverId)
        {
            this.Type = ClientType.Game;
            this.ServerId = serverId;
        }

        public bool ShouldSerializeSessionId() => this.SessionId != Guid.Empty;
        public bool ShouldSerializeType() => this.Type != ClientType.None;
        public bool ShouldSerializeServerId() => this.ServerId > 0;
    }
}
