using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Newtonsoft.Json;

namespace SaltyServer
{
	// FiveM doesn't allow you to use HttpWebRequest/WebClient/etc on the server.
	// You instead have to use the PerformHttpRequestInternal native.
	// Also C# doesn't have a helper implementation like lua(lua has https://wiki.fivem.net/wiki/PerformHttpRequest).
	// https://gist.github.com/bladecoding/368f6a57108696c350fa7408f387a482
	// https://discord.com/channels/192358910387159041/553225456296525833/792847399550189608 (cfx.re Discord)
	public class HttpWrapper : BaseScript
	{
		static readonly Dictionary<int, PendingRequest> _pendingRequests = new Dictionary<int, PendingRequest>();

		public HttpWrapper()
		{
			
		}

		[EventHandler("__cfx_internal:httpResponse")]
		private void OnHttpResponse(int token, int statusCode, string body, dynamic headers)
		{
			if (HttpWrapper._pendingRequests.TryGetValue(token, out PendingRequest request))
			{
				if (statusCode >= 200 && statusCode <= 299)
                {
					if (body.StartsWith("\""))
						body = body.Remove(0, 1);

					if (body.EndsWith("\""))
						body = body.Remove(body.Length - 1, 1);

					request.SetResult(body);
				}
				else
					request.SetException(new Exception("Server returned status code: " + statusCode));

				HttpWrapper._pendingRequests.Remove(token);
			}
		}

		public static async Task<string> GetAsync(string url)
		{
			Dictionary<string, object> args = new Dictionary<string, object>() {
				{ "url", url }
			};

			string argsJson = JsonConvert.SerializeObject(args);
			int id = API.PerformHttpRequestInternal(argsJson, argsJson.Length);
			PendingRequest req = HttpWrapper._pendingRequests[id] = new PendingRequest(id);

			return await req.Task;
		}

		public static async Task<string> PostAsnyc(string url, object obj)
		{
			Dictionary<string, object> args = new Dictionary<string, object>() {
				{ "url", url },
				{ "method", "POST" },
				{ "data", obj.GetType() == typeof(string) ? obj : JsonConvert.SerializeObject(obj) },
				{ "headers", new Dictionary<string, string> { { "Content-Type", "application/json" } } }
			};

			string argsJson = JsonConvert.SerializeObject(args);
			int id = API.PerformHttpRequestInternal(argsJson, argsJson.Length);
			PendingRequest request = HttpWrapper._pendingRequests[id] = new PendingRequest(id);

			return await request.Task;
		}

		private class PendingRequest : TaskCompletionSource<string>
		{
			public int Token;

			public PendingRequest(int token)
			{
				this.Token = token;
			}
		}
	}
}
