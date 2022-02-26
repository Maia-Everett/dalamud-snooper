using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using System.Reflection;

namespace Snooper
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Snooper";

        private const string commandName = "/snooper";

        private CommandManager CommandManager { get; init; }

        private Configuration Configuration { get; init; }
        private PluginUI PluginUi { get; init; }

        private readonly ChatListener chatListener;

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager,
            [RequiredVersion("1.0")] ChatGui chatGui,
            [RequiredVersion("1.0")] TargetManager targetManager)
        {
            this.CommandManager = commandManager;

            this.Configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(pluginInterface);

            // you might normally want to embed resources and load them from the manifest stream
            // var imagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");
            // var goatImage = this.PluginInterface.UiBuilder.LoadImage(imagePath);
            var chatLog = new ChatLog();

            this.PluginUi = new PluginUI(this.Configuration, targetManager, chatLog);

            this.CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Shows a separate window that shows only the \"say\" and \"emote\" messages from the player you're currently targeting. Useful for keeping track of different conversations during crowded RP events."
            });

            pluginInterface.UiBuilder.Draw += DrawUI;
            // pluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            chatListener = new ChatListener(chatGui, chatLog);
        }

        public void Dispose()
        {
            PluginUi.Dispose();
            CommandManager.RemoveHandler(commandName);
            chatListener.Dispose();
        }

        private void OnCommand(string command, string args)
        {
            // in response to the slash command, just display our main ui
            this.PluginUi.Visible = true;
        }

        private void DrawUI()
        {
            this.PluginUi.Draw();
        }

        private void DrawConfigUI()
        {
            this.PluginUi.SettingsVisible = true;
        }
    }
}
