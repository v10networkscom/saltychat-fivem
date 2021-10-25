using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaltyClient
{
    public class PluginError
    {
        public Error Error { get; set; }
        public string Message { get; set; }
        public string ServerIdentifier { get; set; }

        public static PluginError Deserialize(dynamic obj)
        {
            return new PluginError()
            {
                Error = (Error)obj.Error,
                Message = obj.Message,
                ServerIdentifier = obj.ServerIdentifier
            };
        }
    }
}
