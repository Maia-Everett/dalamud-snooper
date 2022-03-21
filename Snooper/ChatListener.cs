using Dalamud.Game.ClientState;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Snooper
{
    internal class ChatListener: IDisposable
    {
        private readonly ChatGui chatGui;
        private readonly ChatLog chatLog;
        private readonly ClientState clientState;

        internal ChatListener(ClientState clientState, ChatGui chatGui, ChatLog chatLog)
        {
            this.clientState = clientState;
            this.chatGui = chatGui;
            this.chatLog = chatLog;
            chatGui.ChatMessage += OnChatMessage;
        }

        public void Dispose()
        {
            chatGui.ChatMessage -= OnChatMessage;
        }

        private void OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            if (type == XivChatType.Say
                || type == XivChatType.StandardEmote
                || type == XivChatType.CustomEmote
                || type == XivChatType.Yell
                || type == XivChatType.Shout
                || type == XivChatType.Party)
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

                chatLog.Add(playerName, new Snooper.ChatEntry(message.ToString(), type, DateTime.Now));
            }
        }
    }
}
