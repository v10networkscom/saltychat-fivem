using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaltyClient
{
    /// <summary>
    /// Used for <see cref="Command.PlaySound"/>
    /// </summary>
    public class Sound
    {
        #region Properties
        public string Filename { get; set; }
        public bool IsLoop { get; set; }
        public string Handle { get; set; }
        #endregion

        #region CTOR
        public Sound(string filename)
        {
            this.Filename = filename;
            this.Handle = filename;
        }

        public Sound(string filename, bool loop)
        {
            this.Filename = filename;
            this.IsLoop = loop;
            this.Handle = filename;
        }

        public Sound(string filename, bool loop, string handle)
        {
            this.Filename = filename;
            this.IsLoop = loop;
            this.Handle = handle;
        }
        #endregion
    }
}
