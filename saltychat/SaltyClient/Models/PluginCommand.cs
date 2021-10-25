using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaltyClient
{
    public class PluginCommand
    {
        #region Properties
        public Command Command { get; set; }
        public string ServerUniqueIdentifier { get; set; }
        public Newtonsoft.Json.Linq.JObject Parameter { get; set; }
        #endregion

        #region CTOR
        /// <summary>
        /// For deserialization only
        /// </summary>
        [Newtonsoft.Json.JsonConstructor]
        internal PluginCommand()
        {

        }

        /// <summary>
        /// Use this for <see cref="Command.Pong"/>
        /// </summary>
        /// <param name="command"></param>
        /// <param name="parameter"></param>
        internal PluginCommand(string serverUniqueIdentifier)
        {
            this.Command = Command.Pong;
            this.ServerUniqueIdentifier = serverUniqueIdentifier;
        }

        /// <summary>
        /// Use this with <see cref="Command.Initiate"/>
        /// </summary>
        /// <param name="command"></param>
        /// <param name="parameter"></param>
        internal PluginCommand(Command command, object parameter)
        {
            this.Command = command;
            this.Parameter = Newtonsoft.Json.Linq.JObject.FromObject(parameter);
        }

        internal PluginCommand(Command command, string serverUniqueIdentifier, object parameter)
        {
            this.Command = command;
            this.ServerUniqueIdentifier = serverUniqueIdentifier;
            this.Parameter = Newtonsoft.Json.Linq.JObject.FromObject(parameter);
        }
        #endregion

        #region Methods
        public string Serialize()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }

        public static PluginCommand Deserialize(dynamic obj)
        {
            return new PluginCommand()
            {
                Command = (Command)obj.Command,
                ServerUniqueIdentifier = obj.ServerUniqueIdentifier,
                Parameter = obj.Parameter == null ? null : Newtonsoft.Json.Linq.JObject.FromObject(obj.Parameter)
            };
        }

        public bool TryGetPayload<T>(out T payload)
        {
            try
            {
                payload = this.Parameter.ToObject<T>();

                return true;
            }
            catch
            {
                // do nothing
            }

            payload = default;
            return false;
        }
        #endregion

        #region Conditional Property Serialization
        public bool ShouldSerializeParameter() => this.Parameter != null;
        #endregion
    }
}
