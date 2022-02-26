using Dalamud.Game.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Snooper
{
    public class ChatEntry
    {
        public string Message { get; init; }
        public XivChatType Type { get; init; }

        public ChatEntry(string message, XivChatType type)
        {
            this.Message = message;
            this.Type = type;
        }
    }

    internal class ChatLog
    {
        private static readonly ICollection<ChatEntry> EmptyList = Array.Empty<ChatEntry>();
        private const int MaxSenders = 100;
        private const int MaxMessagesPerSender = 100;

        private readonly Dictionary<string, LinkedList<ChatEntry>> entryCache = new();
        private readonly LinkedList<string> lruList = new();

        public void Add(string senderName, ChatEntry entry)
        {
            entryCache.TryGetValue(senderName, out LinkedList<ChatEntry>? senderLog);

            if (senderLog == null)
            {
                // Evict earliest sender if necessary
                if (entryCache.Count == MaxSenders)
                {
                    entryCache.Remove(lruList.First!.Value);
                    lruList.RemoveFirst();
                }

                senderLog = new LinkedList<ChatEntry>();
                entryCache[senderName] = senderLog;
                lruList.AddLast(senderName);
            }
            else
            {
                lruList.Remove(senderName);
                lruList.AddLast(senderName);
            }

            // Evict earliest log entry if necessary
            if (senderLog.Count == MaxMessagesPerSender)
            {
                senderLog.RemoveFirst();
            }

            senderLog.AddLast(entry);
        }

        public ICollection<ChatEntry> Get(string senderName)
        {
            entryCache.TryGetValue(senderName, out LinkedList<ChatEntry>? result);
            return result ?? EmptyList;
        }
    }
}
