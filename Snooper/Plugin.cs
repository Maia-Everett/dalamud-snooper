using Dalamud.Game.ClientState;
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

        private readonly CommandManager commandManager;
        private readonly SnooperWindow snooperWindow;
        private readonly ConfigWindow configWindow;
        private readonly ChatListener chatListener;

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager,
            [RequiredVersion("1.0")] ClientState clientState,
            [RequiredVersion("1.0")] ChatGui chatGui,
            [RequiredVersion("1.0")] TargetManager targetManager)
        {
            var chatLog = new ChatLog();
            var configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

            this.commandManager = commandManager;
            snooperWindow = new SnooperWindow(configuration, targetManager, chatLog);
            configWindow = new ConfigWindow(configuration, pluginInterface);
            chatListener = new ChatListener(configuration, clientState, chatGui, chatLog);

            this.commandManager.AddHandler(commandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Shows the Snooper window."
            });

            pluginInterface.UiBuilder.Draw += DrawUI;
            pluginInterface.UiBuilder.OpenConfigUi += () => configWindow.Visible = true;
        }

        public void Dispose()
        {
            snooperWindow.Dispose();
            commandManager.RemoveHandler(commandName);
            chatListener.Dispose();
        }

        private void OnCommand(string command, string args)
        {
            // in response to the slash command, just display our main UI
            snooperWindow.Visible = true;
        }

        private void DrawUI()
        {
            snooperWindow.Draw();
            configWindow.Draw();
        }
    }
}
