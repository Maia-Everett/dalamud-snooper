using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Text;
using Dalamud.Interface.Components;
using Dalamud.Plugin;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Snooper
{
    class ConfigWindow : IDisposable
    {
        internal class LocalConfiguration
        {
            private readonly Configuration configuration;

            internal float opacity;
            internal float fontScale;

            internal LocalConfiguration(Configuration configuration)
            {
                this.configuration = configuration;
                opacity = configuration.Opacity;
                fontScale = configuration.FontScale;
            }

            internal bool IsChanged()
            {
                return opacity != configuration.Opacity
                    || fontScale != configuration.FontScale;
            }

            internal void Save()
            {
                configuration.Opacity = opacity;
                configuration.FontScale = fontScale;
            }
        }

        private const int DefaultWidth = 650;
        private const int DefaultHeight = 300;

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

            ImGui.SetNextWindowSize(new Vector2(DefaultWidth, DefaultHeight), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(150, 100), new Vector2(float.MaxValue, float.MaxValue));
            ImGui.SetNextWindowBgAlpha(0.9f);

            if (ImGui.Begin("Snooper Configuration", ref this.visible))
            {
                // Controls

                ImGui.SliderFloat("Window opacity", ref localConfig.opacity, 0, 1);
                ImGui.SliderFloat("Font scale", ref localConfig.fontScale, 0.5f, 3);

                // Apply changes, if any

                if (localConfig.IsChanged())
                {
                    localConfig.Save();
                    pluginInterface.SavePluginConfig(configuration);
                }

                // Close button

                ImGui.Dummy(new Vector2(0, 8));

                if (ImGui.Button("Close"))
                {
                    visible = false;
                }
            }

            ImGui.End();
        }
    }


}
