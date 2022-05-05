using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Text;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Snooper
{
    class PluginUI : IDisposable
    {
        private const int DefaultWidth = 650;
        private const int DefaultHeight = 500;

        private readonly IDictionary<XivChatType, string> infixes = new Dictionary<XivChatType, string>()
        {
            { XivChatType.Say, ": " },
            { XivChatType.StandardEmote, "" },
            { XivChatType.CustomEmote, "" },
            { XivChatType.Shout, " shouts: " },
            { XivChatType.Yell, " yells: " },
        };

        private readonly IDictionary<XivChatType, uint> chatColors = new Dictionary<XivChatType, uint>()
        {
            { XivChatType.Say, ToImGuiColor(0xf7f7f5) },
            { XivChatType.StandardEmote, ToImGuiColor(0x5ae0b9) },
            { XivChatType.CustomEmote, ToImGuiColor(0x5ae0b9) },
            { XivChatType.Shout, ToImGuiColor(0xffba7c) },
            { XivChatType.Yell, ToImGuiColor(0xffff00) },
            { XivChatType.Party, ToImGuiColor(0x42c8db) },
        };

        private static uint ToImGuiColor(uint rgb)
        {
            return (rgb & 0xff) << 16 | (rgb & 0xff00) | (rgb & 0xff0000) >> 16 | 0xff000000;
        }

        private readonly TargetManager targetManager;
        private readonly ChatLog chatLog;

        private string? lastTarget;
        private DateTime? lastChatUpdate;

        // this extra bool exists for ImGui, since you can't ref a property
        private bool visible = false;
        public bool Visible
        {
            get { return this.visible; }
            set { this.visible = value; }
        }

        // passing in the image here just for simplicity
        public PluginUI(TargetManager targetManager, ChatLog chatLog)
        {
            this.targetManager = targetManager;
            this.chatLog = chatLog;
        }

        public void Dispose()
        {
            // Do nothing
        }

        public void Draw()
        {
            DrawMainWindow();
        }

        public void DrawMainWindow()
        {
            if (!Visible)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(DefaultWidth, DefaultHeight), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(150, 100), new Vector2(float.MaxValue, float.MaxValue));
            ImGui.SetNextWindowBgAlpha(0.6f);

            var targetName = GetTargetName();
            // Window title changes, but the part after the ### is the unique identifier so window position stays constant
            var windowTitle = "Snooper: " + (targetName ?? "(No target player)") + "###Snooper";

            if (ImGui.Begin(windowTitle, ref this.visible))
            {
                if (targetName != null)
                {
                    var log = chatLog.Get(targetName);

                    foreach (var entry in chatLog.Get(targetName))
                    {
                        ShowMessage(targetName, entry.Message, entry.Type);
                    }

                    DateTime? chatUpdateTime = log.Last != null ? log.Last.Value.Time : null;

                    if (targetName != lastTarget || chatUpdateTime != lastChatUpdate)
                    {
                        ImGui.SetScrollHereY(1);
                    }

                    lastChatUpdate = chatUpdateTime;
                }                
            }
            ImGui.End();

            lastTarget = targetName;
        }

        private string? GetTargetName()
        {
            var target = targetManager.Target;

            if (target == null || target.ObjectKind != ObjectKind.Player)
            {
                return null;
            }

            return target.Name.ToString();
        }

        private void ShowMessage(string sender, string message, XivChatType type)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, chatColors[type] | 0xff000000);

            if (type == XivChatType.StandardEmote)
            {
                ImGui.TextWrapped(message);
            }
            else if (type == XivChatType.Party)
            {
                ImGui.TextWrapped($"({sender}) {message}");
            }
            else
            {
                ImGui.TextWrapped($"{sender}{infixes[type]}{message}");
            }

            ImGui.PopStyleColor();
        }
    }


}
