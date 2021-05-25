using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaltyServer
{
    public static class v10networksApi
    {
        public const string BaseUrl = "https://saltmine.de/saltychat/api/v2/";

        public static async Task<Guid> RegisterGameSession(string ipAddress, string teamSpeakUid, string webSocketEndpoint)
        {
            try
            {
                string repsone = await HttpWrapper.PostAsnyc($"{v10networksApi.BaseUrl}gameSession", new GameSession(ipAddress, teamSpeakUid, webSocketEndpoint));

                if (Guid.TryParse(repsone, out Guid sessionId))
                    return sessionId;
            }
            catch (Exception e)
            {
                CitizenFX.Core.Debug.WriteLine($"SaltyProxy - RegisterGameSession: Received exception while registering game session for {ipAddress}:{Environment.NewLine}{e}");
            }

            return Guid.Empty;
        }

        public static async Task<bool> VerifyGameSession(Guid sessionId, string ipAddress, string teamSpeakUid, string webSocketEndpoint)
        {
            try
            {
                await HttpWrapper.PostAsnyc($"{v10networksApi.BaseUrl}gameSessionVerify", new GameSession(sessionId, ipAddress, teamSpeakUid, webSocketEndpoint));

                return true;
            }
            catch (Exception e)
            {
                CitizenFX.Core.Debug.WriteLine($"SaltyProxy - VerifyGameSession: Received exception while verifying game session ({sessionId}) for {ipAddress}:{Environment.NewLine}{e}");
            }

            return false;
        }
    }
}
