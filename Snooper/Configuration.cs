using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Snooper
{
    [Serializable]
    public class Configuration: IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public float Opacity { get; set; } = 0.6f;
        public float FontScale { get; set; } = 1.0f;
    }
}
