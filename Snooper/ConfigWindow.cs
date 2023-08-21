using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Text;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Plugin;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace Snooper
{
    class ConfigWindow : IDisposable
    {
        internal class ChannelEntry
        {
            internal readonly string name;
            internal readonly XivChatType type;
            internal bool enabled;
            internal Vector3 color;

            internal ChannelEntry(XivChatType type, string name)
            {
                this.name = name;
                this.type = type;
            }

            public override bool Equals(object? obj)
            {
                return obj is ChannelEntry entry &&
                       name == entry.name &&
                       type == entry.type &&
                       enabled == entry.enabled &&
                       color == entry.color;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(name, type, enabled, color);
            }
        }

        internal static Vector3 ToVector3(uint color)
        {
            var color4 = ImGui.ColorConvertU32ToFloat4(color);
            return new Vector3(color4.X, color4.Y, color4.Z);
        }

        internal class LocalConfiguration
        {
            private readonly Configuration configuration;

            internal float opacity;
            internal float fontScale;
            internal bool enableFilter;
            internal bool showOnStart;
            internal bool showTimestamps;
            internal bool soundAlerts;
            internal bool autoscroll;
            internal int hoverMode;
            internal IList<ChannelEntry> channels;

            internal LocalConfiguration(Configuration configuration)
            {
                this.configuration = configuration;
                opacity = configuration.Opacity;
                fontScale = configuration.FontScale;
                enableFilter = configuration.EnableFilter;
                showOnStart = configuration.ShowOnStart;
                showTimestamps = configuration.ShowTimestamps;
                soundAlerts = configuration.SoundAlerts;
                autoscroll = configuration.Autoscroll;
                hoverMode = (int) configuration.HoverMode;

                channels = new List<ChannelEntry>
                {
                    new ChannelEntry(XivChatType.Say, "Say"),
                    new ChannelEntry(XivChatType.TellIncoming, "Tell"),
                    new ChannelEntry(XivChatType.CustomEmote, "Emote"),
                    new ChannelEntry(XivChatType.Shout, "Shout"),
                    new ChannelEntry(XivChatType.Yell, "Yell"),
                    new ChannelEntry(XivChatType.Party, "Party"),
                    new ChannelEntry(XivChatType.Alliance, "Alliance"),
                    new ChannelEntry(XivChatType.FreeCompany, "Free Company"),
                    new ChannelEntry(XivChatType.Ls1, "Linkshell"),
                    new ChannelEntry(XivChatType.CrossLinkShell1, "Cross-World Linkshell"),
                };

                foreach (var channel in channels)
                {
                    channel.enabled = configuration.AllowedChatTypes.Contains(channel.type);

                    if (!configuration.ChatColors.TryGetValue(channel.type, out uint intColor))
                    {
                        configuration.ChatColors[channel.type] = Configuration.DefaultChatColors[channel.type];
                    }

                    channel.color = ToVector3(configuration.ChatColors[channel.type]);
                }
            }

            internal bool IsChanged()
            {
                return this != new LocalConfiguration(configuration);
            }

            public override bool Equals(object? obj)
            {
                return obj is LocalConfiguration other &&
                       opacity == other.opacity &&
                       fontScale == other.fontScale &&
                       showTimestamps == other.showTimestamps &&
                       channels.Equals(other.channels);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(opacity, fontScale, showTimestamps, channels);
            }

            internal void Save()
            {
                configuration.Opacity = opacity;
                configuration.FontScale = fontScale;
                configuration.EnableFilter = enableFilter;
                configuration.ShowOnStart = showOnStart;
                configuration.ShowTimestamps = showTimestamps;
                configuration.SoundAlerts = soundAlerts;
                configuration.Autoscroll = autoscroll;
                configuration.HoverMode = (Configuration.Hovermodes) hoverMode;

                foreach (var channel in channels)
                {
                    SaveChannelSettings(channel, channel.type);

                    if (channel.type == XivChatType.CustomEmote)
                    {
                        SaveChannelSettings(channel, XivChatType.StandardEmote);
                    }
                    else if (channel.type == XivChatType.Party)
                    {
                        SaveChannelSettings(channel, XivChatType.CrossParty);
                    }
                    else if (channel.type == XivChatType.Ls1)
                    {
                        for (int i = 2; i <= 8; i++)
                        {
                            SaveChannelSettings(channel, (XivChatType)((ushort)XivChatType.Ls2 + i - 2));
                        }
                    }
                    else if (channel.type == XivChatType.CrossLinkShell1)
                    {
                        for (int i = 2; i <= 8; i++)
                        {
                            SaveChannelSettings(channel, (XivChatType)((ushort)XivChatType.CrossLinkShell2 + i - 2));
                        }
                    }
                }
            }

            private void SaveChannelSettings(ChannelEntry channel, XivChatType type)
            {
                if (channel.enabled)
                {
                    configuration.AllowedChatTypes.Add(type);
                }
                else
                {
                    configuration.AllowedChatTypes.Remove(type);
                }

                var color = channel.color;
                var color4 = new Vector4(color.X, color.Y, color.Z, 1);
                configuration.ChatColors[type] = ImGui.ColorConvertFloat4ToU32(color4);
            }
        }

        private const int DefaultWidth = 450;
        private const int DefaultHeight = 420;

        private readonly Configuration configuration;
        private readonly DalamudPluginInterface pluginInterface;

        // this extra bool exists for ImGui, since you can't ref a property
        private bool visible = false;
        public bool Visible
        {
            get { return this.visible; }
            set { this.visible = value; }
        }

        // passing in the image here just for simplicity
        public ConfigWindow(Configuration configuration, DalamudPluginInterface pluginInterface)
        {
            this.configuration = configuration;
            this.pluginInterface = pluginInterface;
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

            var localConfig = new LocalConfiguration(configuration);

            ImGui.SetNextWindowSize(ImGuiHelpers.ScaledVector2(DefaultWidth, DefaultHeight), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(ImGuiHelpers.ScaledVector2(150, 100), new Vector2(float.MaxValue, float.MaxValue));
            ImGui.SetNextWindowBgAlpha(0.9f);

            if (ImGui.Begin("Snooper Configuration", ref this.visible))
            {
                // Controls

                ImGui.SliderFloat("Window opacity", ref localConfig.opacity, 0, 1);
                ImGui.SliderFloat("Font scale", ref localConfig.fontScale, 0.5f, 3);
                ImGui.Checkbox("Display filter box", ref localConfig.enableFilter);
                ImGui.Checkbox("Show timestamps", ref localConfig.showTimestamps);
                ImGui.Checkbox("Show on start", ref localConfig.showOnStart);
                ImGui.Checkbox("Autoscroll on new message", ref localConfig.autoscroll);
                ImGui.Checkbox("Play a sound when your target posts a message", ref localConfig.soundAlerts);

                ImGui.Text("Show player chat log on");
                ImGui.SameLine();
                ImGui.RadioButton("Click", ref localConfig.hoverMode, (int)Configuration.Hovermodes.Click);
                ImGui.SameLine();
                ImGui.RadioButton("Mouse over", ref localConfig.hoverMode, (int)Configuration.Hovermodes.MouseOver);
                ImGui.SameLine();
                ImGui.RadioButton("Joint", ref localConfig.hoverMode, (int)Configuration.Hovermodes.Joint);
                
                ImGuiHelpers.ScaledDummy(new Vector2(0, 8));

                ImGui.Text("Show channels:");

                foreach (var channel in localConfig.channels)
                {
                    ImGui.Checkbox("##enable_" + channel.name, ref channel.enabled);
                    ImGui.SameLine();
                    ImGuiHelpers.ScaledDummy(new Vector2(4, 0));
                    ImGui.SameLine();
                    ImGui.ColorEdit3("##color_" + channel.name, ref channel.color, ImGuiColorEditFlags.NoInputs);
                    ImGui.SameLine();

                    if (ImGui.Button("Reset Color##resetcolor_" + channel.name))
                    {
                        channel.color = ToVector3(Configuration.DefaultChatColors[channel.type]);
                    }

                    ImGui.SameLine();
                    ImGuiHelpers.ScaledDummy(new Vector2(4, 0));
                    ImGui.SameLine();
                    ImGui.Text(channel.name);
                }

                // Apply changes, if any

                if (localConfig.IsChanged())
                {
                    localConfig.Save();
                    pluginInterface.SavePluginConfig(configuration);
                }

                // Close button

                ImGuiHelpers.ScaledDummy(new Vector2(0, 8));

                if (ImGui.Button("Close"))
                {
                    visible = false;
                }

                // Donation button
                var donationText = "Buy Vielle a tea";
                var buttonWidth = ImGuiHelpers.GetButtonSize(donationText).X;

                ImGui.SameLine(ImGui.GetWindowWidth() - buttonWidth - ImGuiHelpers.ScaledVector2(6, 0).X);
                ImGui.PushStyleColor(ImGuiCol.Button, 0xFF000000 | 0x005E5BFF);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xDD000000 | 0x005E5BFF);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xAA000000 | 0x005E5BFF);

                if (ImGui.Button(donationText))
                {
                    Process.Start(new ProcessStartInfo { FileName = "https://ko-fi.com/vielle_janlenoux", UseShellExecute = true });
                }

                ImGui.PopStyleColor(3);
            }

            ImGui.End();
        }
    }
}
