using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Text;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;

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
        public SnooperWindow(Configuration configuration, TargetManager targetManager, ChatLog chatLog)
        {
            this.configuration = configuration;
            this.targetManager = targetManager;
            this.chatLog = chatLog;
        }

        public void Dispose()
        {
            // Do nothing
        }

        public void Draw()
        {            
            if (!Visible)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(DefaultWidth, DefaultHeight), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(150, 100), new Vector2(float.MaxValue, float.MaxValue));
            ImGui.SetNextWindowBgAlpha(configuration.Opacity);

            var targetName = GetTargetName();
            // Window title changes, but the part after the ### is the unique identifier so window position stays constant
            var windowTitle = "Snooper: " + (targetName ?? "(No target player)") + "###Snooper";

            if (ImGui.Begin(windowTitle, ref this.visible))
            {
                ImGui.SetWindowFontScale(configuration.FontScale);

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

                ImGui.SetWindowFontScale(1);
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
            if (!configuration.AllowedChatTypes.Contains(type))
            {
                return;
            }

            ImGui.PushStyleColor(ImGuiCol.Text, configuration.ChatColors[type] | 0xff000000);
            ImGui.TextWrapped(string.Format(formats[type], sender, message));
            ImGui.PopStyleColor();
        }
    }


}
