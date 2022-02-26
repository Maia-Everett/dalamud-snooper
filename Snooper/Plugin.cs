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
        private PluginUI PluginUi { get; init; }
        private readonly ChatListener chatListener;

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager,
            [RequiredVersion("1.0")] ChatGui chatGui,
            [RequiredVersion("1.0")] TargetManager targetManager)
        {
            var chatLog = new ChatLog();

            this.CommandManager = commandManager;
            this.PluginUi = new PluginUI(targetManager, chatLog);
            this.chatListener = new ChatListener(chatGui, chatLog);

            this.CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Shows a separate window that shows only the \"say\" and \"emote\" messages from the player you're currently targeting. Useful for keeping track of different conversations during crowded RP events."
            });

            pluginInterface.UiBuilder.Draw += DrawUI;
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
    }
}
