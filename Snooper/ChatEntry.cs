using System;
using Dalamud.Game.Text;

namespace Snooper;

public class ChatEntry
{
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
}
