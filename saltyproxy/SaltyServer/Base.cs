using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fleck;
using CitizenFX.Core;
using Newtonsoft.Json;

namespace SaltyServer
{
    public class Base : BaseScript
    {
        #region Props/Fields
        public WebSocketServer Socket;

        public List<PlayerMapper> RegisteredPlayers = new List<PlayerMapper>();
        public Dictionary<Guid, PlayerMapper> SocketMapper = new Dictionary<Guid, PlayerMapper>();
        #endregion

        #region CTOR
        public Base()
        {
            this.Socket = new WebSocketServer("ws://0.0.0.0:8080");
        }
        #endregion

        #region Event Handler
        [EventHandler("SaltyChat_Initialize")]
        public async void OnUserInitializedAsync([FromSource] Player player)
        {
            if (!Int32.TryParse(player.Handle, out int serverId))
                return;

            Debug.WriteLine($"Player {player.Handle} is ready, registering game session for {player.EndPoint}");

            Guid sessionId = await v10networksApi.RegisterGameSession(player.EndPoint, "NMjxHW5psWaLNmFh0+kjnQik7Qc=", "ws://127.0.0.1:8080");

            if (sessionId != Guid.Empty)
            {
                Debug.WriteLine($"Player {player.Handle} is ready and registered, session id: {sessionId}");

                PlayerMapper mapper = this.RegisteredPlayers.FirstOrDefault(p => p.Player == player);

                if (mapper == null)
                {
                    lock (this.RegisteredPlayers)
                    {
                        this.RegisteredPlayers.Add(new PlayerMapper(sessionId, player));
                    }
                }
                else
                {
                    mapper.SessionId = sessionId;
                }
            }
            else
            {
                Debug.WriteLine($"Player {player.Handle} is ready but couldn't register");
            }
        }

        [EventHandler("playerDropped")]
        public void OnPlayerDisconnected([FromSource] Player player, string reason)
        {
            PlayerMapper mapper = this.RegisteredPlayers.FirstOrDefault(p => p.Player == player);

            if (mapper == null)
                return;

            lock (this.RegisteredPlayers)
            {
                this.RegisteredPlayers.Remove(mapper);
            }

            if (mapper.PlayerConnection != null)
                mapper.PlayerConnection.Close();

            if (mapper.PluginConnection != null)
                mapper.PluginConnection.Close();

            Debug.WriteLine($"Cleaned up player {player.Handle}");
        }
        #endregion

        #region Tick
        [Tick]
        private async Task OnFirstTick()
        {
            this.Socket.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    Debug.WriteLine($"socket {socket.ConnectionInfo.Id} opened by {socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort}");

                    if (socket.ConnectionInfo.Origin != null && socket.ConnectionInfo.Origin != "nui://saltychat")
                    {
                        socket.Close();

                        Debug.WriteLine($"socket ({socket.ConnectionInfo.Id}) closed, don't accept connections with origins");
                    }
                };

                socket.OnMessage = async message =>
                {
                    Debug.WriteLine($"message received on socket {socket.ConnectionInfo.Id}: {message}");

                    if (this.SocketMapper.ContainsKey(socket.ConnectionInfo.Id))
                    {
                        bool success = await this.SocketMapper[socket.ConnectionInfo.Id].RelayAsync(socket, message);

                        await BaseScript.Delay(0);

                        Debug.WriteLine($"Message relay on socket {socket.ConnectionInfo.Id} was {(success ? "successful" : "unsuccessful")}");
                    }
                    else
                    {
                        Debug.WriteLine($"Don't have a mapping for socket {socket.ConnectionInfo.Id} yet");

                        ProxyHandshake proxyHandshake;

                        try
                        {
                            proxyHandshake = JsonConvert.DeserializeObject<ProxyHandshake>(message);
                        }
                        catch(Exception e)
                        {
                            Debug.WriteLine($"Don't have a mapping for socket {socket.ConnectionInfo.Id} yet, message wasn't a handshake{Environment.NewLine}{e}");

                            socket.Close();

                            return;
                        }

                        if (proxyHandshake.Type == ProxyHandshake.ClientType.Game)
                        {
                            Player player = this.Players[proxyHandshake.ServerId];

                            if (player != null && player.EndPoint == socket.ConnectionInfo.ClientIpAddress)
                            {
                                PlayerMapper mapper = this.RegisteredPlayers.FirstOrDefault(p => p.Player == player);

                                if (mapper != null)
                                {
                                    mapper.PlayerConnection = socket;

                                    lock (this.SocketMapper)
                                    {
                                        this.SocketMapper.Add(socket.ConnectionInfo.Id, mapper);
                                    }

                                    Debug.WriteLine($"Mapped socket {socket.ConnectionInfo.Id} to player {player.Handle} with session ID {mapper.SessionId}");
                                }
                                else
                                {
                                    Debug.WriteLine($"Don't have a mapping for socket {socket.ConnectionInfo.Id} yet, player {player.Handle} wasn't registered yet");
                                }

                                return;
                            }

                            socket.Close();

                            Debug.WriteLine($"Don't have a mapping for socket {socket.ConnectionInfo.Id} yet, server ID was invalid or endpoint doesn't match");
                        }
                    }
                };

                socket.OnClose = () =>
                {
                    Debug.WriteLine($"socket {socket.ConnectionInfo.Id} closed");

                    if (this.SocketMapper.ContainsKey(socket.ConnectionInfo.Id))
                    {
                        lock (this.SocketMapper)
                        {
                            this.SocketMapper.Remove(socket.ConnectionInfo.Id);
                        }

                        Debug.WriteLine($"socket {socket.ConnectionInfo.Id} was cleaned up");
                    }
                };
            });

            this.Tick -= this.OnFirstTick;

            await Task.FromResult(0);
        }
        #endregion
    }
}
