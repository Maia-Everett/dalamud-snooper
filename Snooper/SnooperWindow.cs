using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using ImGuiNET;

namespace Snooper;

class SnooperWindow : IDisposable
{
    private const int DefaultWidth = 650;
    private const int DefaultHeight = 500;

    private readonly Configuration configuration;
    private readonly IClientState clientState;
    private readonly PluginState pluginState;
    private readonly ITargetManager targetManager;
    private readonly ChatLog chatLog;
    private readonly DalamudPluginInterface pluginInterface;
    private readonly ConfigWindow configWindow;

    private string? lastTarget;
    private DateTime? lastChatUpdate;
    private string filterText = "";
    private bool wasWindowHovered = false;

    // passing in the image here just for simplicity
    public SnooperWindow(Configuration configuration, IClientState clientState, PluginState pluginState, ITargetManager targetManager,
        ChatLog chatLog, DalamudPluginInterface pluginInterface, ConfigWindow configWindow)
    {
        this.configuration = configuration;
        this.clientState = clientState;
        this.pluginState = pluginState;
        this.targetManager = targetManager;
        this.chatLog = chatLog;
        this.pluginInterface = pluginInterface;
        this.configWindow = configWindow;
    }

    public void Dispose()
    {
        // Do nothing
    }

    public void Draw()
    {
        if (!pluginState.visible)
        {
            return;
        }

        if (clientState.LocalPlayer == null)
        {
            return; // only draw if logged in
        }

        if (clientState.LocalPlayer.StatusFlags.HasFlag(StatusFlags.InCombat))
        {
            return; // only draw if out of combat
        }

        DrawWindow(null);

        var windowIds = new List<uint>(configuration.Windows.Keys);

        foreach (var id in windowIds)
        {
            Configuration.WindowConfiguration? windowConfig = configuration.Windows[(uint)id];

            if (windowConfig != null)
            {
                if (windowConfig.visible)
                {
                    DrawWindow(id);
                }
                else
                {
                    configuration.Windows.Remove((uint)id);
                    pluginInterface.SavePluginConfig(configuration);
                }
            }
        }
    }

    private void DrawWindow(uint? id)
    {
        Configuration.WindowConfiguration? windowConfig = id == null ? null : configuration.Windows[(uint)id];

        ImGui.SetNextWindowSize(ImGuiHelpers.ScaledVector2(DefaultWidth, DefaultHeight), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSizeConstraints(ImGuiHelpers.ScaledVector2(80, 80), new Vector2(float.MaxValue, float.MaxValue));
        ImGui.SetNextWindowBgAlpha(configuration.Opacity);

        ICollection<string> playerNames;
        string? targetName;

        if (windowConfig == null)
        {
            targetName = wasWindowHovered ? lastTarget : GetTargetName(configuration.HoverMode);
            playerNames = targetName == null ? Array.Empty<string>() : new string[] { targetName };
        }
        else
        {
            targetName = wasWindowHovered ? lastTarget : GetTargetName(Configuration.HoverModeType.Click);
            playerNames = windowConfig.PlayerNames;
        }

        // Window title changes, but the part after the ### is the unique identifier so window position stays constant
        var playerNamesString = playerNames.Count == 0 ? "(No target player)" : string.Join(", ", playerNames);
        var windowTitle = string.Format("Snooper{0}: {1}###Snooper{2}", id == null ? "" : "*", playerNamesString, id == null ? "" : id);

        bool visible;

        if (id == null)
        {
            visible = ImGui.Begin(windowTitle, ref pluginState.visible);
        }
        else
        {
            visible = ImGui.Begin(windowTitle, ref windowConfig!.visible);
        }

        wasWindowHovered = ImGui.IsWindowHovered();

        if (visible)
        {
            ImGui.SetWindowFontScale(configuration.FontScale);
            ImGui.BeginChild("ScrollRegion", ImGuiHelpers.ScaledVector2(0, -32));
            wasWindowHovered = wasWindowHovered || ImGui.IsWindowHovered();

            LinkedList<ChatEntry> log = ChatLog.EmptyList;

            if (playerNames.Count > 0)
            {
                log = chatLog.Get(playerNames);

                foreach (var entry in log)
                {
                    ShowMessage(entry);
                }

                DateTime? chatUpdateTime = log.Last?.Value.Time;
                DateTime? currentLastUpdate = id == null ? lastChatUpdate : windowConfig!.lastUpdate;

                if (configuration.Autoscroll && chatUpdateTime != currentLastUpdate)
                {
                    ImGui.SetScrollHereY(1);
                }

                if (id == null)
                {
                    lastChatUpdate = chatUpdateTime;
                }
                else
                {
                    windowConfig!.lastUpdate = chatUpdateTime;
                }
            }

            ImGui.EndChild();
            ImGuiHelpers.ScaledDummy(ImGuiHelpers.ScaledVector2(0, 3));

            // Toolbar
            if (playerNames.Count > 0)
            {
                ImGuiHelpers.ScaledDummy(ImGuiHelpers.ScaledVector2(2, 0));
                ImGui.SameLine();

                if (id == null)
                {
                    if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Plus))
                    {
                        var newWindowConfig = new Configuration.WindowConfiguration
                        {
                            PlayerNames = new SortedSet<string>(playerNames)
                        };
                        configuration.Windows.Add(configuration.NextWindowId, newWindowConfig);
                        configuration.NextWindowId++;
                        pluginInterface.SavePluginConfig(configuration);
                    }

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text("Open a new Snooper window for current target");
                        ImGui.EndTooltip();
                    }

                    ImGui.SameLine();

                    if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Cog))
                    {
                        configWindow.Visible = true;
                    }

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text("Snooper plugin configuration");
                        ImGui.EndTooltip();
                    }

                    ImGui.SameLine();
                }

                if (ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Copy))
                {
                    CopyToClipboard(log);
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("Copy log to clipboard");
                    ImGui.EndTooltip();
                }

                if (id != null && targetName != null && !playerNames.Contains(targetName))
                {
                    ImGui.SameLine();

                    if (ImGui.Button("Add target: " + targetName))
                    {
                        playerNames.Add(targetName);
                        pluginInterface.SavePluginConfig(configuration);
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text("Adds target to the current snooper window");
                        ImGui.EndTooltip();
                    }
                }

                if (configuration.EnableFilter && playerNames.Count > 0)
                {
                    ImGui.SameLine();
                    ImGuiHelpers.ScaledDummy(ImGuiHelpers.ScaledVector2(2, 0));
                    ImGui.SameLine();
                    ImGui.Text("Filter messages: ");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    ImGui.InputText("###FilterBar" + id, ref filterText, 100);
                }
            }

            ImGui.SetWindowFontScale(1);
        }

        ImGui.End();

        if (id == null && !wasWindowHovered)
        {
            lastTarget = playerNames.Count == 0 ? null : targetName;
        }
    }


    private string? GetTargetName(Configuration.HoverModeType hoverMode)
    {
        GameObject? target = null;

        if (hoverMode == Configuration.HoverModeType.Joint)
        {
            target = targetManager.Target;
            if (IsValidPlayer(target))
            {
                return target?.Name?.ToString();
            }
        }

        if (hoverMode == Configuration.HoverModeType.MouseOver || hoverMode == Configuration.HoverModeType.Joint)
        {
            target = targetManager.MouseOverTarget;
            if (IsValidPlayer(target))
            {
                return target?.Name?.ToString();
            }
        }

        target = targetManager.Target;
        return IsValidPlayer(target) ? target?.Name?.ToString() : null;
    }

    private bool IsValidPlayer(GameObject? obj)
    {
        return obj != null && obj.ObjectKind == ObjectKind.Player;
    }

    private void ShowMessage(ChatEntry entry)
    {
        var type = entry.Type;

        if (!configuration.AllowedChatTypes.Contains(type))
        {
            return;
        }

        string prefix = GetPrefix(entry);
        var content = entry.ToString();

        ImGui.PushStyleColor(ImGuiCol.Text, configuration.ChatColors[type] | 0xff000000);

        if (string.IsNullOrEmpty(filterText))
        {
            // Display the entire content if no filter is applied
            float wrapWidth = ImGui.GetContentRegionAvail().X;
            ImGui.PushTextWrapPos(wrapWidth);
            ImGui.TextUnformatted(prefix + content);
            ImGui.PopTextWrapPos();
        }
        else if (content.Contains(filterText, StringComparison.InvariantCultureIgnoreCase)) // TODO: Dynamic text wrapping on filter (I gave up)
        {
            int matchIndex;
            int startIndex = 0;
            bool isFirst = true;
            var highlightColor = new Vector4(1.0f, 0.0f, 0.0f, 1.0f); // Bright red;

            // Attempts to get rid of text item spacing
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 1));

            ImGui.TextUnformatted(prefix);
            ImGui.SameLine();

            while ((matchIndex = content.IndexOf(filterText, startIndex, StringComparison.InvariantCultureIgnoreCase)) != -1)
            {
                // Display content before the match
                var beforeMatch = content.Substring(startIndex, matchIndex - startIndex);
                if (isFirst)
                {
                    ImGui.TextUnformatted(beforeMatch);
                    isFirst = false;
                }
                else
                {
                    ImGui.SameLine(); // Same line after first
                    ImGui.TextUnformatted(beforeMatch);
                }


                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, highlightColor);
                ImGui.TextUnformatted(content.Substring(matchIndex,
                        Math.Min(filterText.Length, content.Length - matchIndex)));
                ImGui.PopStyleColor();

                // Move the starting point for the next search after this match
                startIndex = matchIndex + filterText.Length;
            }


            // Display any content after the last match
            if (startIndex < content.Length)
            {
                ImGui.SameLine();
                ImGui.TextUnformatted(content.Substring(startIndex));
            }

            ImGui.PopStyleVar();
        }

        ImGui.PopStyleColor();
    }

    private void CopyToClipboard(ICollection<ChatEntry> chatEntries)
    {
        var text = string.Join("", chatEntries.Select(entry => GetPrefix(entry) + entry.ToString() + "\n"));
        ImGui.SetClipboardText(text);
        pluginInterface.UiBuilder.AddNotification("Chat log copied to clipboard.", "Snooper");
    }

    private string GetPrefix(ChatEntry entry)
    {
        if (configuration.ShowTimestamps == Configuration.TimestampType.Off)
        {
            return "";
        }
        else
        {
            DateTime time = entry.Time;

            if (configuration.DisplayTimezone == Configuration.TimestampTimezone.Local)
            {
                time = time.ToLocalTime();
            }

            string timestamp = configuration.ShowTimestamps switch
            {
                Configuration.TimestampType.Use12Hour => time.ToString("h:mm tt"),
                Configuration.TimestampType.Use24Hour => time.ToString("H:mm"),
                _ => time.ToShortTimeString(),
            };
            return string.Format("[{0}] ", timestamp);
        }
    }
}
