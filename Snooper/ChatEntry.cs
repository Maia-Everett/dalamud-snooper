using System;
using System.Collections.Generic;
using Dalamud.Game.Text;

namespace Snooper;

public class ChatEntry
{
	private static readonly IDictionary<XivChatType, string> formats = new Dictionary<XivChatType, string>()
    {
        { XivChatType.Say, "{0}: {1}" },
        { XivChatType.TellIncoming, "{0} >> {1}" },
        { XivChatType.StandardEmote, "{1}" },
        { XivChatType.CustomEmote, "{0} {1}" },
        { XivChatType.Shout, "{0} shouts: {1}" },
        { XivChatType.Yell, "{0} yells: {1}" },
        { XivChatType.Party, "({0}) {1}" },
        { XivChatType.CrossParty, "({0}) {1}" },
        { XivChatType.Alliance, "(({0})) {1}" },
        { XivChatType.FreeCompany, "[FC]<{0}> {1}" },
    };

    static ChatEntry()
    {
        for (int i = 1; i <= 8; i++)
        {
            var lsChannel = (XivChatType)((ushort)XivChatType.Ls1 + i - 1);
            formats.Add(lsChannel, string.Format("[LS{0}]{1}", i, "<{0}> {1}"));

            var cwlsChannel = i == 1 ? XivChatType.CrossLinkShell1 : (XivChatType)((ushort)XivChatType.CrossLinkShell2 + i - 2);
            formats.Add(cwlsChannel, string.Format("[CWLS{0}]{1}", i, "<{0}> {1}"));
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

	public override string ToString()
	{
		return string.Format(formats[Type], Sender, Message);
	}
}
