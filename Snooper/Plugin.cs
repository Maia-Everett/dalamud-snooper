using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace Snooper
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Snooper";

        private const string commandName = "/snooper";

        private readonly ICommandManager commandManager;
        private readonly SnooperWindow snooperWindow;
        private readonly ConfigWindow configWindow;
        private readonly ChatListener chatListener;
        private readonly Configuration configuration;
        private readonly PluginState pluginState;

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] ICommandManager commandManager,
            [RequiredVersion("1.0")] IClientState clientState,
            [RequiredVersion("1.0")] IChatGui chatGui,
            [RequiredVersion("1.0")] ITargetManager targetManager,
            [RequiredVersion("1.0")] ISigScanner sigScanner,
            [RequiredVersion("1.0")] IGameInteropProvider interop)
        {
            configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            
            pluginState = new PluginState();
            pluginState.Visible = configuration.ShowOnStart;

            var chatLog = new ChatLog();
            snooperWindow = new SnooperWindow(configuration, clientState, pluginState, targetManager, chatLog, pluginInterface);
            configWindow = new ConfigWindow(configuration, pluginInterface);
            chatListener = new ChatListener(configuration, pluginState, clientState, chatGui, chatLog, targetManager, sigScanner, interop);

            this.commandManager = commandManager;
            this.commandManager.AddHandler(commandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Toggles the Snooper window."
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
            // in response to the slash command, toggle snooper window
            pluginState.Visible = !pluginState.Visible;
        }

        private void DrawUI()
        {
            snooperWindow.Draw();
            configWindow.Draw();
        }
    }
}
