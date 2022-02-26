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
            { XivChatType.Say, 0xf7f7f5 },
            { XivChatType.StandardEmote, 0x9af2d8 },
            { XivChatType.CustomEmote, 0x9af2d8 },
            { XivChatType.Shout, 0xffba7c },
            { XivChatType.Yell, 0xffff00 },
        };

        private readonly TargetManager targetManager;
        private readonly ChatLog chatLog;

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
            ImGui.SetNextWindowSizeConstraints(new Vector2(375, 330), new Vector2(float.MaxValue, float.MaxValue));

            var targetName = GetTargetName();
            // Window title changes, but the part after the ### is the unique identifier so window position stays constant
            var windowTitle = "Snooper: " + (targetName ?? "(No target player)") + "###Snooper";

            if (ImGui.Begin(windowTitle, ref this.visible))
            {
                if (targetName != null)
                {
                    foreach (var entry in chatLog.Get(targetName))
                    {
                        ShowMessage(targetName, entry.Message, entry.Type);
                    }
                }
            }
            ImGui.End();
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
            ImGui.TextWrapped($"{sender}{infixes[type]}{message}");
            ImGui.PopStyleColor();
        }
    }
}
