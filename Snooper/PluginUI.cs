using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Text;
using ImGuiNET;
using System;
using System.Numerics;

namespace Snooper
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    class PluginUI : IDisposable
    {
        private const int DefaultWidth = 650;
        private const int DefaultHeight = 500;

        private readonly Configuration configuration;
        private readonly TargetManager targetManager;
        private readonly ChatLog chatLog;

        // private ImGuiScene.TextureWrap goatImage;

        // this extra bool exists for ImGui, since you can't ref a property
        private bool visible = false;
        public bool Visible
        {
            get { return this.visible; }
            set { this.visible = value; }
        }

        private bool settingsVisible = false;
        public bool SettingsVisible
        {
            get { return this.settingsVisible; }
            set { this.settingsVisible = value; }
        }

        // passing in the image here just for simplicity
        public PluginUI(Configuration configuration, TargetManager targetManager, ChatLog chatLog)
        {
            this.configuration = configuration;
            this.targetManager = targetManager;
            this.chatLog = chatLog;
        }

        public void Dispose()
        {
            // this.goatImage.Dispose();
        }

        public void Draw()
        {
            // This is our only draw handler attached to UIBuilder, so it needs to be
            // able to draw any windows we might have open.
            // Each method checks its own visibility/state to ensure it only draws when
            // it actually makes sense.
            // There are other ways to do this, but it is generally best to keep the number of
            // draw delegates as low as possible.

            DrawMainWindow();
            DrawSettingsWindow();
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
            string infix;

            switch (type)
            {
                case XivChatType.Say:
                    infix = ": ";
                    break;
                case XivChatType.Shout:
                    infix = " shouts: ";
                    break;
                case XivChatType.Yell:
                    infix = " yells: ";
                    break;
                case XivChatType.CustomEmote:
                case XivChatType.StandardEmote:
                    infix = "";
                    break;
                default:
                    throw new Exception(); // Cannot happen
            }

            ImGui.TextWrapped($"{sender}{infix}{message}");
        }

        public void DrawSettingsWindow()
        {
            if (!SettingsVisible)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(232, 75), ImGuiCond.Always);
            if (ImGui.Begin("A Wonderful Configuration Window", ref this.settingsVisible,
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                // can't ref a property, so use a local copy
                var configValue = this.configuration.SomePropertyToBeSavedAndWithADefault;
                if (ImGui.Checkbox("Random Config Bool", ref configValue))
                {
                    this.configuration.SomePropertyToBeSavedAndWithADefault = configValue;
                    // can save immediately on change, if you don't want to provide a "Save and Close" button
                    this.configuration.Save();
                }
            }
            ImGui.End();
        }
    }
}
