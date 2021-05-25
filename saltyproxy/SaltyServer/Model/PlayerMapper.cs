using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using Fleck;

namespace SaltyServer
{
    public class PlayerMapper
    {
        public Guid SessionId { get; set; }
        public Player Player { get; set; }
        public IWebSocketConnection PlayerConnection { get; set; }
        public IWebSocketConnection PluginConnection { get; set; }

        public PlayerMapper(Guid sessionId, Player player)
        {
            this.SessionId = sessionId;
            this.Player = player;
        }

        public async Task<bool> RelayAsync(IWebSocketConnection socket, string message)
        {
            if (this.PlayerConnection == socket)
                return await this.RelayToPluginAsync(message);
            else if (this.PluginConnection == socket)
                return await this.RelayToPlayerAsync(message);

            return false;
        }

        public async Task<bool> RelayToPlayerAsync(string message)
        {
            if (this.PlayerConnection != null && this.PlayerConnection.IsAvailable)
            {
                await this.PlayerConnection.Send(message);

                return true;
            }

            return false;
        }

        public async Task<bool> RelayToPluginAsync(string message)
        {
            if (this.PluginConnection != null && this.PluginConnection.IsAvailable)
            {
                await this.PluginConnection.Send(message);

                return true;
            }

            return false;
        }
    }
}
