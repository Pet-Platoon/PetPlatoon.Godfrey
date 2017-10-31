using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Godfrey.Extensions;
using Godfrey.Helpers;
using Godfrey.Models.Context;

namespace Godfrey.Commands
{
    public class DiceCommands
    {
        [Command("dice"), Aliases("würfel")]
        public async Task DiceAsync(CommandContext ctx, int sides = 6)
        {
            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                var channels = await ctx.Guild.GetConfigValueAsync<IList<ulong>>("game.dice.channels", null, uow);
                if (channels.Contains(ctx.Channel.Id))
                {


                    return;
                }
            }

            if (sides < 4)
            {
                throw new NotSupportedException("Der Würfel muss mindestens vier Seiten haben.");
            }

            var rng = new Random((int)(DateTime.UtcNow.Ticks % int.MaxValue));
            var value = rng.Next(0, sides) + 1;

            if (value == 1945)
            {
                await ctx.RespondAsync("https://www.youtube.com/watch?v=IPMnEmkoPFs", embed: new DiscordEmbedBuilder()
                                               .WithAuthor(ctx.Member.Nickname ?? ctx.Member.Username,
                                                           icon_url: ctx.Member.AvatarUrl)
                                               .WithColor(DiscordColor.Cyan)
                                               .WithDescription($"{ctx.Member.Nickname ?? ctx.Member.Username} würfelte eine {value} mit einem W{sides}-Würfel."));
                return;
            }

            await ctx.RespondAsync(embed: new DiscordEmbedBuilder()
                                              .WithAuthor(ctx.Member.Nickname ?? ctx.Member.Username, icon_url: ctx.Member.AvatarUrl)
                                              .WithColor(DiscordColor.Cyan)
                                              .WithDescription($"{ctx.Member.Nickname ?? ctx.Member.Username} würfelte eine {value} mit einem W{sides}-Würfel."));
        }

        [Command("coin"), Aliases("münze")]
        public async Task CoinAsync(CommandContext ctx)
        {
            var rng = new Random((int)(DateTime.UtcNow.Ticks % int.MaxValue));
            var value = rng.Next(0, 2);
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder()
                                           .WithAuthor(ctx.Member.Nickname ?? ctx.Member.Username, icon_url: ctx.Member.AvatarUrl)
                                           .WithColor(DiscordColor.Cyan)
                                           .WithDescription($"{ctx.Member.Nickname ?? ctx.Member.Username} warf eine Münze und bekam {(value == 0 ? "Kopf" : "Zahl")}."));
        }
    }
}
