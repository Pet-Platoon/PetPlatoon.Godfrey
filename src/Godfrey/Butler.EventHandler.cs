﻿using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;

namespace Godfrey
{
    public partial class Butler
    {
        private async Task OnCommandErrored(CommandErrorEventArgs e)
        {
            if (e.Exception is CommandNotFoundException)
            {
                return;
            }

            e.Context.Client.DebugLogger.LogMessage(LogLevel.Error, "Butler", $"{e.Context.User.Username} tried executing '{e.Command?.QualifiedName ?? "<unknown command>"}' but it errored: {e.Exception.GetType()}: {e.Exception.Message ?? "<no message>"}", DateTime.Now);

            var embedBuilder = new DiscordEmbedBuilder()
                    .WithTitle("Ein Fehler ist aufgetreten!")
                    .WithDescription($"Ein Fehler im Command `{e.Command?.QualifiedName ?? "<unknown command>"}` ist aufgetreten:{Environment.NewLine}```{e.Exception.Message}```")
                    .WithColor(DiscordColor.Red);

            await e.Context.RespondAsync(embed: embedBuilder.Build());
        }
    }
}