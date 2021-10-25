using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaltyClient
{
    /// <summary>
    /// Will be received from the WebSocket if after starting a new instance
    /// </summary>
    public class PluginState
    {
        public string Version { get; set; }
        public int ActiveInstances { get; set; }
    }
}
