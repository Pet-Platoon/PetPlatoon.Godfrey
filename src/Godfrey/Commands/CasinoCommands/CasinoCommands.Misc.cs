using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Godfrey.Attributes;
using Godfrey.Extensions;
using Godfrey.Models.Context;
using CooldownBucketType = Godfrey.Attributes.CooldownBucketType;

namespace Godfrey.Commands.CasinoCommands
{
    public partial class CasinoCommands
    {
        [Command("give")]
        [GodfreyChannelType(Constants.Casino.Channel)]
        [Description("Gibt einem User eine bestimmte Menge an Coins.")]
        public async Task GiveCommandAsync(CommandContext ctx, [Description("Der User, der die Coins erhalten soll.")]
                                           DiscordUser to, [Description("Die Menge, die man dem User geben möchte.")]
                                           long amount)
        {
            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                DiscordEmbedBuilder embed;

                var user = await ctx.User.GetUserAsync(uow);
                var toUser = await to.GetUserAsync(uow);
                user.LastCasinoCommandIssued = DateTime.UtcNow;
                await uow.SaveChangesAsync();

                if (amount <= 0)
                {
                    embed = Constants.Embeds.Presets.Error(description: $"Wen willst du hier verarschen?");
                    await ctx.RespondAsync(embed: embed);
                    return;
                }

                if (user.Coins < amount)
                {
                    embed = Constants.Embeds.Presets.Error(description: $"Du besitzt nur {user.Coins} Credits.");
                    await ctx.RespondAsync(embed: embed);
                    return;
                }

                if (toUser.Coins + amount < amount)
                {
                    embed =
                            Constants.Embeds.Presets.Error(description:
                                                           $"{toUser.Name} kann nicht so viel Geld besitzen");
                    await ctx.RespondAsync(embed: embed);
                    return;
                }

                user.Coins -= amount;
                toUser.Coins += amount;

                embed =
                        Constants.Embeds.Presets
                                 .Success(description: $"{user.Name} gibt {toUser.Name} {amount} Coins.");
                await ctx.RespondAsync(embed: embed);
                await uow.SaveChangesAsync();
            }
        }

        [Command("steal")]
        [Aliases("loot")]
        [GodfreyChannelType(Constants.Casino.Channel)]
        [GodfreyCooldown(1, 300, CooldownBucketType.User)]
        [Description(
                "Startet den Versuch einem Mitspieler eine bestimme Menge an Coins zu stehlen. Es besteht eine 70/30 Chance, dass der User es **nicht** schafft.")]
        public async Task StealCommandAsync(CommandContext ctx,
                                            [Description("Der User, dem man die Coins stehlen möchte.")]
                                            DiscordUser from,
                                            [Description("Die menge, die man dem User stehlen möchte.")]
                                            long amount)
        {
            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                DiscordEmbedBuilder embed;
                var user = await ctx.User.GetUserAsync(uow);
                var stealFrom = await from.GetUserAsync(uow);
                user.LastCasinoCommandIssued = DateTime.UtcNow;
                await uow.SaveChangesAsync();

                if (amount <= 0)
                {
                    embed = Constants.Embeds.Presets.Error(description: $"Wen willst du hier verarschen?");
                    await ctx.RespondAsync(embed: embed);
                    return;
                }

                if (user.Coins == 0)
                {
                    embed = Constants.Embeds.Presets.Error(description: "Du besitzt keine Coins.");
                    await ctx.RespondAsync(embed: embed);
                    return;
                }

                if (stealFrom.Coins == 0)
                {
                    embed = Constants.Embeds.Presets.Error(description: $"{stealFrom.Name} besitzt keine Coins.");
                    await ctx.RespondAsync(embed: embed);
                    return;
                }

                var max = Math.Min(user.Coins, stealFrom.Coins);
                amount = Math.Min(amount, max);

                var percentage = Butler.RandomGenerator.NextDouble();

                if (DateTime.UtcNow - stealFrom.LastCasinoCommandIssued > TimeSpan.FromDays(2))
                {
                    percentage = 1;
                }

                if (percentage < 0.7)
                {
                    embed = Constants.Embeds.Presets.Error("Du wurdest erwischt",
                                                           $"Dein Gegner hat dich beim Stehlen erwischt und dich zum Krüppel geschlagen. Du verlierst {amount} Coins an {stealFrom.Name}.");
                    user.Coins -= amount;
                    stealFrom.Coins += amount;
                    await ctx.RespondAsync(embed: embed);
                    await uow.SaveChangesAsync();
                    return;
                }

                embed = Constants.Embeds.Presets.Success("Let's go sneaky beaky like",
                                                         $"Du hast {amount} von {stealFrom.Name} gestohlen.");
                user.Coins += amount;
                stealFrom.Coins -= amount;
                await ctx.RespondAsync(embed: embed);
                await uow.SaveChangesAsync();
            }
        }
    }
}
