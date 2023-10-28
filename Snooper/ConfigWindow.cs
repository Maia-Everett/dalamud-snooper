using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Numerics;

using Dalamud.Game.Text;
using Dalamud.Interface.Utility;
using Dalamud.Plugin;

using ImGuiNET;
using Snooper.SeFunctions;
using Dalamud.Utility;
using Snooper.Utils;

namespace Snooper;

class ConfigWindow : IDisposable
{
    private readonly Sounds[] ValidSounds =
        ((Sounds[])Enum.GetValues(typeof(Sounds))).Where(s => s != Sounds.Unknown).ToArray();


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
        internal int showTimestamps;
        internal int displayTimezone;
        internal Sounds soundAlert;
        internal bool autoscroll;
        internal int hoverMode;
        internal bool enableLogging;
        internal string logDirectory;
        internal IList<ChannelEntry> channels;

        internal LocalConfiguration(Configuration configuration)
        {
            this.configuration = configuration;
            opacity = configuration.Opacity;
            fontScale = configuration.FontScale;
            enableFilter = configuration.EnableFilter;
            showOnStart = configuration.ShowOnStart;
            showTimestamps = (int) configuration.ShowTimestamps;
            displayTimezone = (int) configuration.DisplayTimezone;
            soundAlert = configuration.GetEffectiveAlertSound();
            autoscroll = configuration.Autoscroll;
            hoverMode = (int) configuration.HoverMode;
            enableLogging = configuration.EnableLogging;
            logDirectory = configuration.LogDirectory;

            if (logDirectory.IsNullOrEmpty())
            {
                logDirectory = PlatformUtils.GetDefaultLogDirectory();
            }

            channels = new List<ChannelEntry>
            {
                new(XivChatType.Say, "Say"),
                new(XivChatType.TellIncoming, "Tell"),
                new(XivChatType.CustomEmote, "Emote"),
                new(XivChatType.Shout, "Shout"),
                new(XivChatType.Yell, "Yell"),
                new(XivChatType.Party, "Party"),
                new(XivChatType.Alliance, "Alliance"),
                new(XivChatType.FreeCompany, "Free Company"),
                new(XivChatType.Ls1, "Linkshell"),
                new(XivChatType.CrossLinkShell1, "Cross-World Linkshell"),
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
                    displayTimezone == other.displayTimezone &&
                    enableFilter == other.enableFilter &&
                    showOnStart == other.showOnStart &&
                    soundAlert == other.soundAlert &&
                    autoscroll == other.autoscroll &&
                    hoverMode == other.hoverMode &&
                    enableLogging == other.enableLogging &&
                    logDirectory == other.logDirectory &&
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
            configuration.ShowTimestamps = (Configuration.TimestampType) showTimestamps;
            configuration.DisplayTimezone = (Configuration.TimestampTimezone) displayTimezone;
            configuration.SoundAlerts = soundAlert;
            configuration.Autoscroll = autoscroll;
            configuration.HoverMode = (Configuration.HoverModeType) hoverMode;
            configuration.EnableLogging = enableLogging;
            configuration.LogDirectory = logDirectory;

            foreach (var channel in channels)
            {
                SaveChannelSettings(channel, channel.type);
            }

            configuration.NormalizeChannels();
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
    private const int DefaultHeight = 520;

    private readonly Configuration configuration;
    private readonly DalamudPluginInterface pluginInterface;
    private readonly PlaySound playSound;
    private readonly ChatLog chatLog;

    // this extra bool exists for ImGui, since you can't ref a property
    private bool visible = false;
    public bool Visible
    {
        get { return visible; }
        set { visible = value; }
    }

    // passing in the image here just for simplicity
    public ConfigWindow(Configuration configuration, ChatLog chatLog, DalamudPluginInterface pluginInterface, PlaySound playSound)
    {
        this.configuration = configuration;
        this.pluginInterface = pluginInterface;
        this.chatLog = chatLog;
        this.playSound = playSound;
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

        ImGui.SetNextWindowSize(ImGuiHelpers.ScaledVector2(DefaultWidth, DefaultHeight), ImGuiCond.Appearing);
        ImGui.SetNextWindowSizeConstraints(ImGuiHelpers.ScaledVector2(150, 100), new Vector2(float.MaxValue, float.MaxValue));
        ImGui.SetNextWindowBgAlpha(0.9f);

        if (ImGui.Begin("Snooper Configuration", ref this.visible))
        {
            // Controls

            ImGui.SliderFloat("Window opacity", ref localConfig.opacity, 0, 1);
            ImGui.SliderFloat("Font scale", ref localConfig.fontScale, 0.5f, 3);
            ImGui.Checkbox("Display filter box", ref localConfig.enableFilter);

            ImGui.Checkbox("Show on start", ref localConfig.showOnStart);
            ImGui.Checkbox("Autoscroll on new message", ref localConfig.autoscroll);

            ImGui.Text("Play sound on target message:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(150 * ImGuiHelpers.GlobalScale);

            if (ImGui.BeginCombo("##AlertSoundsCombo", localConfig.soundAlert.ToName()))
            {
                foreach (var sound in ValidSounds)
                {
                    if (ImGui.Selectable($"{sound.ToName()}##AlertSoundsCombo"))
                    {
                        localConfig.soundAlert = sound;
                        playSound.Play(sound);
                    }
                }
                
                ImGui.EndCombo();
            }

            ImGui.Text("Timestamps:");
            ImGui.SameLine();
            ImGui.RadioButton("Off", ref localConfig.showTimestamps, (int)Configuration.TimestampType.Off);
            ImGui.SameLine();
            ImGui.RadioButton("System", ref localConfig.showTimestamps, (int)Configuration.TimestampType.System);
            ImGui.SameLine();
            ImGui.RadioButton("12-hour", ref localConfig.showTimestamps,
                    (int)Configuration.TimestampType.Use12Hour);
            ImGui.SameLine();
            ImGui.RadioButton("24-hour", ref localConfig.showTimestamps, (int)Configuration.TimestampType.Use24Hour);

            ImGui.Text("Timestamp timezone:");
            ImGui.SameLine();
            ImGui.RadioButton("Server time (UTC)", ref localConfig.displayTimezone,
                    (int)Configuration.TimestampTimezone.Utc);
            ImGui.SameLine();
            ImGui.RadioButton("Local time", ref localConfig.displayTimezone,
                    (int)Configuration.TimestampTimezone.Local);

            ImGui.Text("Show player chat log on:");
            ImGui.SameLine();
            ImGui.RadioButton("Click", ref localConfig.hoverMode, (int)Configuration.HoverModeType.Click);
            ImGui.SameLine();
            ImGui.RadioButton("Mouse over", ref localConfig.hoverMode, (int)Configuration.HoverModeType.MouseOver);
            ImGui.SameLine();
            ImGui.RadioButton("Joint", ref localConfig.hoverMode, (int)Configuration.HoverModeType.Joint);

            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
                ImGui.SetTooltip("Shows log of selected target if there is one, otherwise of moused-over player");
            }
            
            ImGuiHelpers.ScaledDummy(new Vector2(0, 8));

            ImGui.Checkbox("Save log files to:", ref localConfig.enableLogging);
            ImGui.SameLine();
            ImGui.InputText("###logDirectory", ref localConfig.logDirectory, 260, ImGuiInputTextFlags.CallbackCompletion);

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
                chatLog.CloseAllAppenders();
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
