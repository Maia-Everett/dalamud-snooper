using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using Dalamud.Plugin;

namespace Snooper;

internal class ChatLog
{
    internal static readonly LinkedList<ChatEntry> EmptyList = new();
    private const int MaxSenders = 100;
    private const int MaxOpenFiles = 100;
    private const int MaxMessagesPerSender = 300;

    private readonly Configuration configuration;
    private readonly DalamudPluginInterface pluginInterface;
    private readonly Dictionary<string, LinkedList<ChatEntry>> entryCache = new();
    private readonly LinkedList<string> lruList = new();
    private readonly Dictionary<string, StreamWriter> appenderCache = new();
    private readonly LinkedList<string> appenderLruList = new();

    internal ChatLog(Configuration configuration, DalamudPluginInterface pluginInterface)
    {
        this.configuration = configuration;
        this.pluginInterface = pluginInterface;
    }

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

        if (configuration.EnableLogging)
        {
            LogToFile(entry);
        }
    }

    public LinkedList<ChatEntry> Get(string senderName)
    {
        entryCache.TryGetValue(senderName, out LinkedList<ChatEntry>? result);
        return result ?? EmptyList;
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
            entryCache.TryGetValue(name, out LinkedList<ChatEntry>? result);

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
        string message = string.Format("[{0} ST] {1}",
                entry.Time.ToUniversalTime().ToString("H:mm:ss"), entry.ToString());
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
        appenderCache.TryGetValue(senderName, out StreamWriter? appender);

        if (appender == null)
        {
            // Evict earliest sender if necessary
            if (appenderCache.Count == MaxOpenFiles)
            {
                string oldSender = appenderLruList.First!.Value;
                appenderCache.TryGetValue(oldSender, out StreamWriter? oldAppender);

                if (oldAppender != null)
                {
                    oldAppender.Dispose();
                }

                appenderCache.Remove(oldSender);
                appenderLruList.RemoveFirst();
            }

            Directory.CreateDirectory(configuration.LogDirectory);
            Directory.CreateDirectory(configuration.LogDirectory + "/global");

            string fileName = configuration.LogDirectory + "/" + senderName + ".log";
            appender = new StreamWriter(fileName, true);
            appenderCache[senderName] = appender;
            appenderLruList.AddLast(senderName);
        }
        else
        {
            appenderLruList.Remove(senderName);
            appenderLruList.AddLast(senderName);
        }

        appender.AutoFlush = true;
        return appender;
    }

    public void CloseAllAppenders()
    {
        foreach (var appender in appenderCache.Values)
        {
            appender.Dispose();
        }

        appenderCache.Clear();
        appenderLruList.Clear();
    }
}
