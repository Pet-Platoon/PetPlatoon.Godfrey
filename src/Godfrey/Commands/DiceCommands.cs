using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace Godfrey.Commands
{
    public class DiceCommands
    {
        [Command("dice"), Aliases("würfel")]
        public async Task DiceAsync(CommandContext ctx, int sides = 6)
        {
            if (sides <= 4)
            {
                throw new NotSupportedException("Der Würfel muss mindestens vier Seiten haben.");
            }

            var rng = new Random((int)(DateTime.UtcNow.Ticks % int.MaxValue));
            var value = rng.Next(0, sides) + 1;
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder()
                                              .WithAuthor(ctx.Member.Nickname ?? ctx.Member.Username, icon_url: ctx.Member.AvatarUrl)
                                              .WithColor(DiscordColor.Cyan)
                                              .WithDescription($"{ctx.Member.Nickname ?? ctx.Member.Username} würfelte eine {value}. Mit einem W{sides}-Würfel."));
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
