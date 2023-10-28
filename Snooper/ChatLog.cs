using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Dalamud.Plugin;
using Snooper.Utils;

namespace Snooper;

internal class ChatLog
{
    internal static readonly LinkedList<ChatEntry> EmptyList = new();
    private const int MaxSenders = 100;
    private const int MaxOpenFiles = 100;
    private const int MaxMessagesPerSender = 300;

    private readonly Configuration configuration;
    private readonly DalamudPluginInterface pluginInterface;
    private readonly LruCache<string, LinkedList<ChatEntry>> entryCache = new(MaxSenders);
    private readonly LruCache<string, StreamWriter> appenderCache = new(MaxOpenFiles);

    internal ChatLog(Configuration configuration, DalamudPluginInterface pluginInterface)
    {
        this.configuration = configuration;
        this.pluginInterface = pluginInterface;
    }

    public void Add(string senderName, ChatEntry entry)
    {
        LinkedList<ChatEntry> senderLog = entryCache.GetOrLoad(senderName, _ => new LinkedList<ChatEntry>());

        // Evict earliest log entry if necessary
        if (senderLog.Count == MaxMessagesPerSender)
        {
            senderLog.RemoveFirst();
        }

        senderLog.AddLast(entry);

        if (configuration.EnableLogging)
        {
            LogToFile(entry);
        }
    }

    public LinkedList<ChatEntry> Get(string senderName)
    {
        return entryCache.GetCachedOrDefault(senderName, EmptyList);
    }

    public LinkedList<ChatEntry> Get(ICollection<string> senderNames)
    {
        if (senderNames.Count == 1)
        {
            // common case - just one character per window
            return Get(senderNames.First());
        }

        var aggregate = new List<ChatEntry>();

        foreach (var name in senderNames)
        {
            LinkedList<ChatEntry>? result = entryCache[name];

            if (result != null)
            {
                aggregate.AddRange(result);
            }
        }

        aggregate.Sort((e1, e2) =>
        {
            if (e1.Time < e2.Time)
            {
                return -1;
            }

            if (e1.Time > e2.Time)
            {
                return 1;
            }

            return 0;
        });

        return new LinkedList<ChatEntry>(aggregate);
    }

    private void LogToFile(ChatEntry entry)
    {
        string message = entry.ToTimedString();
        var senders = new HashSet<string>
        {
            entry.Sender,
            "global/" + DateTime.UtcNow.ToString("yyyy-MM-dd"),
        };

        foreach (var windowConfig in configuration.Windows.Values)
        {
            senders.Add(string.Join(", ", windowConfig.PlayerNames));
        }

        try {
            foreach (var sender in senders)
            {
                GetAppender(sender).WriteLine(message);
            }
        }
        catch (Exception e)
        {
            pluginInterface.UiBuilder.AddNotification("Cannot write to log: " + e.Message, "Snooper",
                    Dalamud.Interface.Internal.Notifications.NotificationType.Error);
        }
    }

    private StreamWriter GetAppender(string senderName)
    {
        return appenderCache.GetOrLoad(senderName, key => {
            Directory.CreateDirectory(configuration.LogDirectory);
            Directory.CreateDirectory(configuration.LogDirectory + "/global");

            string fileName = configuration.LogDirectory + "/" + senderName + ".log";
            return new StreamWriter(fileName, true, Encoding.UTF8)
            {
                AutoFlush = true
            };
        });
    }

    public void CloseAllAppenders()
    {
        appenderCache.Clear(appender => appender.Dispose());
    }
}
