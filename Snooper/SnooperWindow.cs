using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text;
using Dalamud.Interface;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using Dalamud.Game.ClientState;
using Dalamud.Plugin;

namespace Snooper
{
    class SnooperWindow : IDisposable
    {
        private const int DefaultWidth = 650;
        private const int DefaultHeight = 500;        

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

        static SnooperWindow()
        {
            for (int i = 1; i <= 8; i++)
            {
                var lsChannel = (XivChatType)((ushort)XivChatType.Ls1 + i - 1);
                formats.Add(lsChannel, string.Format("[LS{0}]{1}", i, "<{0}> {1}"));

                var cwlsChannel = i == 1 ? XivChatType.CrossLinkShell1 : (XivChatType)((ushort)XivChatType.CrossLinkShell2 + i - 2);
                formats.Add(cwlsChannel, string.Format("[CWLS{0}]{1}", i, "<{0}> {1}"));
            }
        }

        private readonly Configuration configuration;
        private readonly ClientState clientState;
        private readonly PluginState pluginState;
        private readonly TargetManager targetManager;
        private readonly ChatLog chatLog;
        private readonly DalamudPluginInterface pluginInterface;

        private string? lastTarget;
        private DateTime? lastChatUpdate;
        private string filterText = "";
        private bool wasWindowHovered = false;

        // passing in the image here just for simplicity
        public SnooperWindow(Configuration configuration, ClientState clientState, PluginState pluginState, TargetManager targetManager,
            ChatLog chatLog, DalamudPluginInterface pluginInterface)
        {
            this.configuration = configuration;
            this.clientState = clientState;
            this.pluginState = pluginState;
            this.targetManager = targetManager;
            this.chatLog = chatLog;
            this.pluginInterface = pluginInterface;
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
            Configuration.WindowConfiguration? windowConfig = id == null ? null : configuration.Windows[(uint) id];

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
                targetName = wasWindowHovered ? lastTarget : GetTargetName(Configuration.HoverModes.Click);
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
                if (id == null && playerNames.Count > 0)
                {
                    if (ImGui.Button("+"))
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
                        ImGui.Text("Opens a new snooper window for current target.");
                        ImGui.EndTooltip();
                    }
                }
                else if (id != null && targetName != null && !playerNames.Contains(targetName))
                {
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
                
                ImGui.SetWindowFontScale(configuration.FontScale);
                ImGui.BeginChild("ScrollRegion", ImGuiHelpers.ScaledVector2(0, -32));
                wasWindowHovered = wasWindowHovered || ImGui.IsWindowHovered();

                if (playerNames.Count > 0)
                {
                    var log = chatLog.Get(playerNames);

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

                if (configuration.EnableFilter)
                {
                    ImGui.InputText("Filter Messages", ref filterText, 100);
                }
            
                ImGui.SetWindowFontScale(1);
            }
            
            ImGui.End();

            if (id == null && !wasWindowHovered)
            {
                lastTarget = playerNames.Count == 0 ? null : targetName;
            }
        }


        private string? GetTargetName(Configuration.HoverModes hoverMode)
        {
            GameObject? target = null;

            if (hoverMode == Configuration.HoverModes.Joint)
            {
                target = targetManager.Target;
                if (IsValidPlayer(target))
                {
                    return target?.Name?.ToString();
                }
            }

            if (hoverMode == Configuration.HoverModes.MouseOver || hoverMode == Configuration.HoverModes.Joint)
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
            var sender = entry.Sender;
            var type = entry.Type;

            if (!configuration.AllowedChatTypes.Contains(type))
            {
                return;
            }

            var prefix = configuration.ShowTimestamps ? string.Format("[{0}] ", entry.Time.ToShortTimeString()) : "";
            var content = string.Format(formats[type], sender, entry.Message);
            
            ImGui.PushStyleColor(ImGuiCol.Text, configuration.ChatColors[type] | 0xff000000);
            
            if (string.IsNullOrEmpty(filterText))
            {
                // Display the entire content if no filter is applied
                float wrapWidth = ImGui.GetContentRegionAvail().X;
                ImGui.PushTextWrapPos(wrapWidth);
                ImGui.TextUnformatted(prefix + content);
                ImGui.PopTextWrapPos();
            }
            else if (content.Contains(filterText)) // TODO: Dynamic text wrapping on filter (I gave up)
            {
                int matchIndex;
                int startIndex = 0;
                bool isFirst = true;
                var highlightColor = new Vector4(1.0f, 0.0f, 0.0f, 1.0f); // Bright red;

                // Attempts to get rid of text item spacing
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 1)); 
                
                while ((matchIndex = content.IndexOf(filterText, startIndex, StringComparison.OrdinalIgnoreCase)) != -1)
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
                    ImGui.TextUnformatted(filterText);
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
    }
}
