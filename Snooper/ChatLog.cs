using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
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
    private readonly ISet<string> nonLogged = new HashSet<string>();
    private readonly IPluginLog pluginLog;

    internal ChatLog(Configuration configuration, DalamudPluginInterface pluginInterface, IPluginLog pluginLog)
    {
        this.configuration = configuration;
        this.pluginInterface = pluginInterface;
        this.pluginLog = pluginLog;
    }

    public void Add(string senderName, ChatEntry entry)
    {
        LinkedList<ChatEntry> senderLog = entryCache.GetOrLoad(senderName, LoadLog);
        nonLogged.Remove(senderName);

        // Evict earliest log entry if necessary
        if (senderLog.Count == MaxMessagesPerSender)
        {
            senderLog.RemoveFirst();
        }

        senderLog.AddLast(entry);

        if (configuration.EnableLogging)
        {
            LogToFile(senderName, entry);
        }
    }

    public LinkedList<ChatEntry> Get(string senderName)
    {
        if (nonLogged.Contains(senderName))
        {
            return EmptyList;
        }

        LinkedList<ChatEntry>? cachedLog = entryCache[senderName];

        if (cachedLog == null)
        {
            LinkedList<ChatEntry> loadedLog = LoadLog(senderName);

            if (loadedLog.Count > 0)
            {
                entryCache.Set(senderName, loadedLog);
            }

            return loadedLog;
        }

        return cachedLog;
    }

    public LinkedList<ChatEntry> Get(ICollection<string> senderNames)
    {
        if (senderNames.Count == 1)
        {
            // common case - just one character per window
            return Get(senderNames.First());
        }

        var aggregate = new SortedSet<ChatEntry>(Comparer<ChatEntry>.Create((e1, e2) =>
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
        }));

        foreach (var name in senderNames)
        {
            foreach (var entry in Get(name))
            {
                aggregate.Add(entry);
            }
        }

        return new LinkedList<ChatEntry>(aggregate);
    }

    private void LogToFile(string senderName, ChatEntry entry)
    {
        string message = entry.ToTimedString();
        var senders = new HashSet<string>
        {
            senderName,
            "global/" + DateTime.UtcNow.ToString("yyyy-MM-dd"),
        };

        foreach (var windowConfig in configuration.Windows.Values)
        {
            senders.Add(string.Join(", ", windowConfig.PlayerNames));
        }

        try
        {
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
        return appenderCache.GetOrLoad(senderName, key =>
        {
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

    private LinkedList<ChatEntry> LoadLog(string senderName)
    {
        LinkedList<ChatEntry> lines = new();

        if (!configuration.EnableLogging || nonLogged.Contains(senderName))
        {
            return lines;
        }

        string fileName = configuration.LogDirectory + "/" + senderName + ".log";

        try
        {
            using (var reader = new ReverseStreamReader(fileName))
            {
                while (lines.Count < MaxMessagesPerSender)
                {
                    string? line = reader.ReadLine();

                    if (line == null)
                    {
                        // Reached the beginning of file
                        return lines;
                    }

                    if (line == "")
                    {
                        continue;
                    }

                    ChatEntry? chatEntry = ChatEntry.TryParseTimedString(line);

                    if (chatEntry != null)
                    {
                        lines.AddFirst(chatEntry);
                    }
                }
            }
        }
        catch (Exception e)
        {
            pluginLog.Error(e, "Cannot retrieve logs for {0}", senderName);
            // Ignore - not critical
            nonLogged.Add(senderName);
        }

        return lines;
    }
}
