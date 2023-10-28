using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Dalamud.Configuration;
using Dalamud.Game.Text;
using Snooper.SeFunctions;
using Snooper.Utils;

namespace Snooper;

[Serializable]
public class Configuration: IPluginConfiguration
{
    private static readonly XivChatType[] AllAllowedChatTypes =
    {
        XivChatType.Say,
        XivChatType.TellIncoming,
        XivChatType.TellOutgoing,
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
    
    private const Sounds DefaultSound = Sounds.Sound16;

    private static uint ToImGuiColor(uint rgb)
    {
        return (rgb & 0xff) << 16 | (rgb & 0xff00) | (rgb & 0xff0000) >> 16 | 0xff000000;
    }

    public static readonly IDictionary<XivChatType, uint> DefaultChatColors = new Dictionary<XivChatType, uint>()
    {
        { XivChatType.Say, ToImGuiColor(0xf7f7f5) },
        { XivChatType.TellIncoming, ToImGuiColor(0xffc8ed) },
        { XivChatType.StandardEmote, ToImGuiColor(0x5ae0b9) },
        { XivChatType.CustomEmote, ToImGuiColor(0x5ae0b9) },
        { XivChatType.Shout, ToImGuiColor(0xffba7c) },
        { XivChatType.Yell, ToImGuiColor(0xffff00) },
        { XivChatType.Party, ToImGuiColor(0x42c8db) },
        { XivChatType.CrossParty, ToImGuiColor(0x42c8db) },
        { XivChatType.Alliance, ToImGuiColor(0xff9d20) },
        { XivChatType.FreeCompany, ToImGuiColor(0x9fd0d6) },
    };
    
    static Configuration()
    {
        // Initialize LS and CWLS colors
        var lsColor = ToImGuiColor(0xdcf56e);

        for (int i = 1; i <= 8; i++)
        {
            var lsChannel = (XivChatType)((ushort)XivChatType.Ls1 + i - 1);
            DefaultChatColors.Add(lsChannel, lsColor);

            var cwlsChannel = i == 1 ? XivChatType.CrossLinkShell1 : (XivChatType)((ushort)XivChatType.CrossLinkShell2 + i - 2);
            DefaultChatColors.Add(cwlsChannel, lsColor);
        }
    }
    
    public enum HoverModeType
    {
        Click = 0,
        MouseOver = 1,
        Joint = 2,
    }

    public enum TimestampType {
        Off = 0,
        System = 1,
        Use12Hour  = 2,
        Use24Hour = 3,
    }

    public enum TimestampTimezone {
        Utc = 0,
        Local = 1,
    }

    public int Version { get; set; } = 0;
    public float Opacity { get; set; } = 0.6f;
    public float FontScale { get; set; } = 1.0f;
    public bool EnableFilter { get; set; } = true;
    public bool ShowOnStart { get; set; } = true;
    public TimestampType ShowTimestamps { get; set; } = TimestampType.Off;
    public TimestampTimezone DisplayTimezone { get; set; } = TimestampTimezone.Utc;
    public HoverModeType HoverMode { get; set; } = HoverModeType.MouseOver;
    public Sounds SoundAlerts { get; set; } = Sounds.Sound16;
    public bool Autoscroll { get; set; } = true;
    public bool EnableLogging { get; set; } = true;
    public string LogDirectory { get; set; } = PlatformUtils.GetDefaultLogDirectory();
    public ISet<XivChatType> AllowedChatTypes { get; set; } = new HashSet<XivChatType>(AllAllowedChatTypes);
    public IDictionary<XivChatType, uint> ChatColors { get; set; } = new Dictionary<XivChatType, uint>(DefaultChatColors);
    public IDictionary<uint, WindowConfiguration> Windows { get; set; } = new Dictionary<uint, WindowConfiguration>();
    public uint NextWindowId { get; set; } = 0;

    [OnDeserialized]
    protected void OnDeserialized(StreamingContext streamingContext)
    {
        NormalizeChannels();
    }

    public void NormalizeChannels()
    {
        CopyChannelSettings(XivChatType.TellIncoming, XivChatType.TellOutgoing);
        CopyChannelSettings(XivChatType.CustomEmote, XivChatType.StandardEmote);
        CopyChannelSettings(XivChatType.Party, XivChatType.CrossParty);

        for (int i = 2; i <= 8; i++)
        {
            CopyChannelSettings(XivChatType.Ls1, (XivChatType)((ushort)XivChatType.Ls2 + i - 2));
            CopyChannelSettings(XivChatType.CrossLinkShell1,
                    (XivChatType)((ushort)XivChatType.CrossLinkShell2 + i - 2));
        }
    }

    private void CopyChannelSettings(XivChatType source, XivChatType dest)
    {
        if (AllowedChatTypes.Contains(source))
        {
            AllowedChatTypes.Add(dest);
        }
        else
        {
            AllowedChatTypes.Remove(dest);
        }

        ChatColors[dest] = ChatColors[source];
    }

    public Sounds GetEffectiveAlertSound() {
        if (SoundAlerts == Sounds.None || (SoundAlerts >= Sounds.Sound01 && SoundAlerts <= Sounds.Sound16)) {
            return SoundAlerts;
        }

        return DefaultSound;
    }

    [Serializable]
    public class WindowConfiguration
    {
        public ISet<string> PlayerNames { get; set; } = new SortedSet<string>();

        [NonSerialized]
        public DateTime? lastUpdate = null;

        [NonSerialized]
        public bool visible = true;
    }    
}
