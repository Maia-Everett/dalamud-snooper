using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Snooper
{
    internal class PluginState
    {
        public bool visible = false;
        public bool Visible
        {
            get { return this.visible; }
            set { this.visible = value; }
        }
    }
}
