﻿using System;
using System.Linq;
using System.Text.RegularExpressions;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;

using Snooper.SeFunctions;

namespace Snooper;

internal class ChatListener : IDisposable
{
    private static readonly Regex UnicodePrivateUseArea = new(@"[\uE000-\uF8FF]+", RegexOptions.Compiled);

    private readonly Configuration configuration;
    private readonly PluginState pluginState;
    private readonly IChatGui chatGui;
    private readonly ChatLog chatLog;
    private readonly IClientState clientState;
    private readonly ITargetManager targetManager;
    private readonly PlaySound playSound;

    internal ChatListener(Configuration configuration, PluginState pluginState, IClientState clientState,
        IChatGui chatGui, ChatLog chatLog, ITargetManager targetManager, PlaySound playSound)
    {
        this.configuration = configuration;
        this.pluginState = pluginState;
        this.clientState = clientState;
        this.chatGui = chatGui;
        this.chatLog = chatLog;
        this.targetManager = targetManager;
        this.playSound = playSound;
        chatGui.ChatMessage += OnChatMessage;
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
            // GitHub issue #5: When the message is from self in party chat, a bogus U+E090 character is prepended
            // to the character name. Strip the entire Private Use Area to be safe.
            playerName = UnicodePrivateUseArea.Replace(playerName, "");

            var chatEntry = new ChatEntry(playerName, message.ToString(), type, DateTime.UtcNow);
            chatLog.Add(playerName, chatEntry);

            if (type == XivChatType.TellOutgoing && clientState.LocalPlayer != null)
            {
                // If Alice sends an outgoing tell to Bob, then the sender, counterintuitively, is reported as Bob.
                // We want to record the message for both Alice *and* Bob's chat logs.
                string selfName = clientState.LocalPlayer.Name.TextValue;
                // ChatEntry is immutable, so reusing the instance is safe
                chatLog.Add(selfName, chatEntry);
            }

            var alertSound = configuration.GetEffectiveAlertSound();

            // Play alert if enabled and the message came from the target
            if (alertSound != Sounds.None
                    && pluginState.Visible
                    && type != XivChatType.TellIncoming
                    && type != XivChatType.TellOutgoing)
            {
                var target = targetManager.Target;

                if (target != null
                    && target.ObjectKind == ObjectKind.Player
                    && target.Name.ToString() == playerName)
                {
                    playSound.Play(alertSound);
                }
            }
        }
    }
}
