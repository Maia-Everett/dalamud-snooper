using System;
using System.Linq;

using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;

using Snooper.SeFunctions;

namespace Snooper
{
    internal class ChatListener: IDisposable
    {
        private readonly Configuration configuration;
        private readonly PluginState pluginState;
        private readonly IChatGui chatGui;
        private readonly ChatLog chatLog;
        private readonly IClientState clientState;
        private readonly ITargetManager targetManager;
        private readonly PlaySound playSound;

        internal ChatListener(Configuration configuration, PluginState pluginState, IClientState clientState,
            IChatGui chatGui, ChatLog chatLog, ITargetManager targetManager, ISigScanner sigScanner, IGameInteropProvider interop)
        {
            this.configuration = configuration;
            this.pluginState = pluginState;
            this.clientState = clientState;
            this.chatGui = chatGui;
            this.chatLog = chatLog;
            this.targetManager = targetManager;
            chatGui.ChatMessage += OnChatMessage;
            playSound = new PlaySound(sigScanner, interop);
        }

        public void Dispose()
        {
            chatGui.ChatMessage -= OnChatMessage;
        }

        private void OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            if (configuration.AllowedChatTypes.Contains(type))
            {
                PlayerPayload? playerPayload;

                if (clientState.LocalPlayer != null && sender.ToString() == clientState.LocalPlayer.Name.TextValue)
                {
                    // If the messages are sent by the player themselves, we record them as the sender.
                    // This is necessary so that custom emotes starting with "You" are recorded correctly.
                    playerPayload = new PlayerPayload(clientState.LocalPlayer.Name.TextValue, clientState.LocalPlayer.HomeWorld.Id);
                }
                else
                {
                    playerPayload = sender.Payloads.SingleOrDefault(x => x is PlayerPayload) as PlayerPayload
                        ?? message.Payloads.FirstOrDefault(x => x is PlayerPayload) as PlayerPayload;
                }

                var playerName = playerPayload != null ? playerPayload.PlayerName : sender.ToString();

                chatLog.Add(playerName, new Snooper.ChatEntry(playerName, message.ToString(), type, DateTime.Now));

                // Play alert if enabled and the message came from the target
                if (configuration.SoundAlerts && pluginState.Visible && type != XivChatType.TellIncoming)
                {
                    var target = targetManager.Target;

                    if (target != null
                        && target.ObjectKind == ObjectKind.Player
                        && target.Name.ToString() == playerName)
                    {
                        playSound.Play(Sounds.Sound16);
                    }
                }
            }
        }
    }
}
