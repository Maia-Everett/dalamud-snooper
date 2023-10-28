using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Dalamud.Game.Text;

namespace Snooper;

public class ChatEntry
{
    private const string TimeFormat = "yyyy-MM-dd HH:mm:ss";
    private static readonly Regex TimedStringRegex = new(
        @"^\[(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}) ST\] (.+)$", RegexOptions.Compiled);

	private static readonly IDictionary<XivChatType, string> formats = new Dictionary<XivChatType, string>()
    {
        { XivChatType.Say, "{0}: {1}" },
        { XivChatType.TellIncoming, "{0} >> {1}" },
        { XivChatType.TellOutgoing, ">> {0}: {1}" },
        { XivChatType.StandardEmote, "{1}" },
        { XivChatType.CustomEmote, "{0} {1}" },
        { XivChatType.Shout, "{0} shouts: {1}" },
        { XivChatType.Yell, "{0} yells: {1}" },
        { XivChatType.Party, "({0}) {1}" },
        { XivChatType.CrossParty, "*({0}) {1}" },
        { XivChatType.Alliance, "(({0})) {1}" },
        { XivChatType.FreeCompany, "[FC]<{0}> {1}" },
    };

    private static readonly List<KeyValuePair<XivChatType, Regex>> parseRegexes = new XivChatType[] {
        XivChatType.FreeCompany,
        XivChatType.Alliance,
        XivChatType.Party,
        XivChatType.CrossParty,
        XivChatType.Yell,
        XivChatType.Shout,
        XivChatType.TellIncoming,
        XivChatType.TellOutgoing,
        XivChatType.Say,
        XivChatType.CustomEmote,
    }.Select(ToParseRegex).ToList();

    private static KeyValuePair<XivChatType, Regex> ToParseRegex(XivChatType xivChatType)
    {
        string format = formats[xivChatType];
        string pattern = "^" + format
                .Replace("[", @"\[")
                .Replace("]", @"\]")
                .Replace("(", @"\(")
                .Replace(")", @"\)")
                .Replace("*", @"\*")
                .Replace("{0}", @"([\w']+ [\w']+)")
                .Replace("{1}", "(.+)") + "$";
        return new KeyValuePair<XivChatType, Regex>(xivChatType, new Regex(pattern, RegexOptions.Compiled));
    }

    static ChatEntry()
    {
        for (int i = 1; i <= 8; i++)
        {
            var lsChannel = (XivChatType)((ushort)XivChatType.Ls1 + i - 1);
            formats.Add(lsChannel, string.Format("[LS{0}]{1}", i, "<{0}> {1}"));

            var cwlsChannel = i == 1 ? XivChatType.CrossLinkShell1 : (XivChatType)((ushort)XivChatType.CrossLinkShell2 + i - 2);
            formats.Add(cwlsChannel, string.Format("[CWLS{0}]{1}", i, "<{0}> {1}"));

            parseRegexes.Add(ToParseRegex(lsChannel));
            parseRegexes.Add(ToParseRegex(cwlsChannel));
        }
    }

	public string Sender { get; init; }
	public string Message { get; init; }
	public XivChatType Type { get; init; }
	public DateTime Time { get; init; }

	public ChatEntry(string sender, string message, XivChatType type, DateTime time)
	{
		this.Sender = sender;
		this.Message = message;
		this.Type = type;
		this.Time = time;
	}

    public string ToTimedString()
    {
        return string.Format("[{0} ST] {1}", Time.ToString(TimeFormat), ToString());
    }

	public override string ToString()
	{
		return string.Format(formats[Type], Sender, Message);
	}

    public static ChatEntry? TryParseTimedString(string timedString)
    {
        Match match = TimedStringRegex.Match(timedString);

        if (!match.Success)
        {
            return null;
        }

        string timestamp = match.Groups[1].Value;
        string text = match.Groups[2].Value;
        var flags = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;

        if (!DateTime.TryParseExact(timestamp, TimeFormat, null, flags, out DateTime time))
        {
            return null;
        }

        foreach (KeyValuePair<XivChatType, Regex> kvp in parseRegexes)
        {
            Match textMatch = kvp.Value.Match(text);

            if (textMatch.Success)
            {
                string sender = textMatch.Groups[1].Value;
                string message = textMatch.Groups[2].Value;
                return new ChatEntry(sender, message, kvp.Key, time);
            }
        }

        return new ChatEntry("", text, XivChatType.CustomEmote, time);
    }
}
