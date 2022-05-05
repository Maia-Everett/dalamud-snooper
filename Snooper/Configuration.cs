using Dalamud.Configuration;
using Dalamud.Game.Text;
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
        private static readonly XivChatType[] AllAllowedChatTypes =
        {
            XivChatType.Say,
            XivChatType.TellIncoming,
            XivChatType.StandardEmote,
            XivChatType.CustomEmote,
            XivChatType.Shout,
            XivChatType.Yell,
            XivChatType.Party,
            XivChatType.CrossParty,
            XivChatType.Alliance,
            XivChatType.FreeCompany,
            XivChatType.Ls1,
            XivChatType.Ls2,
            XivChatType.Ls3,
            XivChatType.Ls4,
            XivChatType.Ls5,
            XivChatType.Ls6,
            XivChatType.Ls7,
            XivChatType.Ls8,
            XivChatType.CrossLinkShell1,
            XivChatType.CrossLinkShell2,
            XivChatType.CrossLinkShell3,
            XivChatType.CrossLinkShell4,
            XivChatType.CrossLinkShell5,
            XivChatType.CrossLinkShell6,
            XivChatType.CrossLinkShell7,
            XivChatType.CrossLinkShell8,
        };

        public int Version { get; set; } = 0;
        public float Opacity { get; set; } = 0.6f;
        public float FontScale { get; set; } = 1.0f;
        public ISet<XivChatType> AllowedChatTypes { get; set; } = new HashSet<XivChatType>(Configuration.AllAllowedChatTypes);
    }
}
