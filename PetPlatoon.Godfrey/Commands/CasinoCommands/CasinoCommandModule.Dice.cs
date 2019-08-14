using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using PetPlatoon.Godfrey.Attributes;
using PetPlatoon.Godfrey.Database;
using PetPlatoon.Godfrey.Extensions;
using CooldownBucketType = PetPlatoon.Godfrey.Attributes.CooldownBucketType;

namespace PetPlatoon.Godfrey.Commands.CasinoCommands
{
    public partial class CasinoCommandModule
    {
        private static readonly GodfreyCooldownAttribute DiceCooldown = new GodfreyCooldownAttribute(1, Constants.Quotes.Times.Downtime, CooldownBucketType.User);

        [Command("dice"), Aliases("würfel")]
        [Description("Wirft einen Würfel. In einem Casino Channel, werden die Sides auf 6 geforced. Im Casino wird um Coins gespielt. Ein Wurf kostet 5 Coins. Die Ergebnisse 1 und 2 liefern keinen Gewinn. Das Ergebnis 3 gleicht die Wurfkosten aus. 4, 5 und 6 geben jeweils 5, 10 und 15 Coins.")]
        public async Task DiceCommandAsync(CommandContext ctx, [Description("Die Seiten des Würfels")] ushort sides = 6)
        {
            var databaseContext = ctx.Services.GetService<DatabaseContext>();

            var channel = await ctx.Guild.GetConfigValueAsync<ulong>(Constants.Casino.Channel, databaseContext, 0);

            if (channel != 0 && ctx.Channel.Id == channel)
            {
                await CasinoDiceCommandAsync(ctx, databaseContext);
                return;
            }

            await NormalDiceCommandAsync(ctx, sides);
        }

        private async Task CasinoDiceCommandAsync(CommandContext ctx, DatabaseContext uow)
        {
            if (!await DiceCooldown.ExecuteCheckAsync(ctx, false))
            {
                return;
            }

            var user = await ctx.User.GetUserAsync(uow);
            user.LastCasinoCommandIssued = DateTime.UtcNow;
            await uow.SaveChangesAsync();

            if (user.Coins < 5)
            {
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder()
                                                  .WithColor(DiscordColor.Red)
                                                  .WithDescription($"Du hast nicht genügend Coins um mitzuspielen. Fuck off m8 <3 ({user.Coins}/5 Coins)"));
                return;
            }

            var value = Startup.Random.Next(0, 6) + 1;

            var percentage = (float)value / 6;

            if (percentage < Half)
            {
                user.Coins -= 5;

                await ctx.RespondAsync(embed: new DiscordEmbedBuilder()
                                               .WithColor(DiscordColor.Green)
                                               .WithDescription($"{user.Name} würfelt eine {value} mit einem w6-Würfel. Du verlierst 5 Coins. Du besitzt nun {user.Coins}"));

                await uow.SaveChangesAsync();

                return;
            }

            if (percentage < TwoThirds)
            {
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder()
                                               .WithColor(DiscordColor.Green)
                                               .WithDescription($"{user.Name} würfelt eine {value} mit einem w6-Würfel. Du erhältst keine Coins. Du besitzt nun {user.Coins}"));

                await uow.SaveChangesAsync();

                return;
            }

            if (percentage >= TwoThirds)
            {
                var coins = percentage * 6;
                var transactCoins = (long)(coins - 3) * 5;

                user.Coins += transactCoins;

                await ctx.RespondAsync(embed: new DiscordEmbedBuilder()
                                               .WithColor(DiscordColor.Green)
                                               .WithDescription($"{user.Name} würfelt eine {value} mit einem w6-Würfel. Du erhältst nun {transactCoins} Coins. Du besitzt nun {user.Coins}"));

                await uow.SaveChangesAsync();
            }
        }

        private async Task NormalDiceCommandAsync(CommandContext ctx, ushort sides)
        {
            var value = Startup.Random.Next(0, sides) + 1;
            DiscordEmbed embed;

            if (value == 1945)
            {
                embed = new DiscordEmbedBuilder()
                        .WithAuthor(ctx.Member.Nickname ?? ctx.Member.Username, iconUrl: ctx.Member.AvatarUrl)
                        .WithColor(DiscordColor.Cyan)
                        .WithDescription($"{ctx.Member.Nickname ?? ctx.Member.Username} würfelte eine {value} mit einem W{sides}-Würfel.");

                await ctx.RespondAsync("https://www.youtube.com/watch?v=IPMnEmkoPFs", embed: embed);
                return;
            }

            embed = new DiscordEmbedBuilder()
                    .WithAuthor(ctx.Member.Nickname ?? ctx.Member.Username, iconUrl: ctx.Member.AvatarUrl)
                    .WithColor(DiscordColor.Cyan)
                    .WithDescription($"{ctx.Member.Nickname ?? ctx.Member.Username} würfelte eine {value} mit einem W{sides}-Würfel.");

            await ctx.RespondAsync(embed: embed);
        }
    }
}
