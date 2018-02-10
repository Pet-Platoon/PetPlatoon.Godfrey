using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Godfrey.Attributes;
using Godfrey.Extensions;
using Godfrey.Models.Context;
using Microsoft.EntityFrameworkCore;
using CooldownBucketType = Godfrey.Attributes.CooldownBucketType;

namespace Godfrey.Commands
{
    public class CasinoCommands : BaseCommandModule
    {
        internal const float Half = 1f / 2;
        internal const float Third = 1f / 3;
        internal const float TwoThirds = Third * 2;

        private static readonly GodfreyCooldownAttribute DiceCooldown = new GodfreyCooldownAttribute(1, Constants.Quotes.Times.Downtime, CooldownBucketType.User);
        private static readonly GodfreyCooldownAttribute CoinCooldown = new GodfreyCooldownAttribute(1, Constants.Quotes.Times.Downtime, CooldownBucketType.User);

        [Command("casino"), RequireUserPermissions(Permissions.Administrator)]
        public async Task CasinoCommandAsync(CommandContext ctx, bool removeOld = false)
        {
            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                DiscordEmbed embed;
                var channel = await ctx.Guild.GetConfigValueAsync<ulong>(Constants.Casino.Channel, 0, uow);

                if (channel != 0)
                {
                    if (ctx.Channel.Id == channel)
                    {
                        embed = Constants.Embeds.Presets.Output(description: "Der jetzige Channel ist bereits das Casino.");

                        await ctx.RespondAsync(embed: embed);
                        return;
                    }

                    if (!removeOld)
                    {
                        embed = Constants.Embeds.Presets.Error(description: "Es existiert bereits ein Casino für diesen Server. Um den jetzigen Channel als Casino zu nutzen, hänge ein `true` an den Befehl an");

                        await ctx.RespondAsync(embed: embed);
                        return;
                    }

                    channel = ctx.Channel.Id;
                    await ctx.Guild.SetConfigValueAsync(Constants.Casino.Channel, channel, uow);

                    embed = Constants.Embeds.Presets.Success(description: "Der Channel für das Casino wurde erfolgreich auf den jetzigen Channel geändert.");
                    await ctx.RespondAsync(embed: embed);
                    return;
                }

                channel = ctx.Channel.Id;
                await ctx.Guild.SetConfigValueAsync(Constants.Casino.Channel, channel, uow);

                embed = Constants.Embeds.Presets.Success(description: "Der Channel für das Casino wurde erfolgreich auf den jetzigen Channel gesetzt.");
                await ctx.RespondAsync(embed: embed);
            }
        }

        /// <summary>
        /// Gives a specific user a specific amount of coins.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="to"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        [Command("give"), GodfreyChannelType(Constants.Casino.Channel)]
        [Description("Gibt einem User eine bestimmte Menge an Coins.")]
        public async Task GiveCommandAsync(CommandContext ctx, [Description("Der User, der die Coins erhalten soll.")] DiscordUser to, [Description("Die Menge, die man dem User geben möchte.")] long amount)
        {
            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                DiscordEmbedBuilder embed;

                var user = await ctx.User.GetUserAsync(uow);
                var toUser = await to.GetUserAsync(uow);

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
                    embed = Constants.Embeds.Presets.Error(description: $"{toUser.Name} kann nicht so viel Geld besitzen");
                    await ctx.RespondAsync(embed: embed);
                    return;
                }

                user.Coins -= amount;
                toUser.Coins += amount;

                embed = Constants.Embeds.Presets .Success(description: $"{user.Name} gibt {toUser.Name} {amount} Coins.");
                await ctx.RespondAsync(embed: embed);
                await uow.SaveChangesAsync();
            }
        }

        [Command("steal"), Aliases("loot"), GodfreyChannelType(Constants.Casino.Channel), GodfreyCooldown(1, 300, CooldownBucketType.User)]
        [Description("Startet den Versuch einem Mitspieler eine bestimme Menge an Coins zu stehlen. Es besteht eine 70/30 Chance, dass der User es **nicht** schaffst.")]
        public async Task StealCommandAsync(CommandContext ctx, [Description("Der User, dem man die Coins stehlen möchte.")] DiscordUser from, [Description("Die menge, die man dem User stehlen möchte.")] long amount)
        {
            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                DiscordEmbedBuilder embed;
                var user = await ctx.User.GetUserAsync(uow);
                var stealFrom = await from.GetUserAsync(uow);

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

                if (percentage < 0.7)
                {
                    embed = Constants.Embeds.Presets.Error("Du wurdest erwischt", $"Dein Gegner hat dich beim Stehlen erwischt und dich zum Krüppel geschlagen. Du verlierst {amount} Coins an {stealFrom.Name}.");
                    user.Coins -= amount;
                    stealFrom.Coins += amount;
                    await ctx.RespondAsync(embed: embed);
                    await uow.SaveChangesAsync();
                    return;
                }

                embed = Constants.Embeds.Presets.Success("Let's go sneaky beaky like", $"Du hast {amount} von {stealFrom.Name} gestohlen.");
                user.Coins += amount;
                stealFrom.Coins -= amount;
                await ctx.RespondAsync(embed: embed);
                await uow.SaveChangesAsync();
            }
        }

        [Command("dice"), Aliases("würfel")]
        [Description("Wirft einen Würfel. In einem Casino Channel, werden die Sides auf 6 geforced. Im Casino wird um Coins gespielt. Ein Wurf kostet 5 Coins. Die Ergebnisse 1 und 2 liefern keinen Gewinn. Das Ergebnis 3 gleicht die Wurfkosten aus. 4, 5 und 6 geben jeweils 5, 10 und 15 Coins.")]
        public async Task DiceCommandAsync(CommandContext ctx, [Description("Die Seiten des Würfels")] ushort sides = 6)
        {
            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                var channel = await ctx.Guild.GetConfigValueAsync<ulong>(Constants.Casino.Channel, 0, uow);

                if (channel != 0 && ctx.Channel.Id == channel)
                {
                    await CasinoDiceCommandAsync(ctx, uow);
                    return;
                }

                await NormalDiceCommandAsync(ctx, sides);
            }
        }

        private async Task CasinoDiceCommandAsync(CommandContext ctx, DatabaseContext uow)
        {
            if (!await DiceCooldown.ExecuteCheckAsync(ctx, false))
            {
                return;
            }

            var user = await ctx.User.GetUserAsync(uow);

            if (user.Coins < 5)
            {
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder()
                                                  .WithColor(DiscordColor.Red)
                                                  .WithDescription($"Du hast nicht genügend Coins um mitzuspielen. Fuck off m8 <3 ({user.Coins}/5 Coins)"));
                return;
            }

            var value = Butler.RandomGenerator.Next(0, 6) + 1;

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
            var value = Butler.RandomGenerator.Next(0, sides) + 1;
            DiscordEmbed embed;

            if (value == 1945)
            {
                embed = new DiscordEmbedBuilder()
                        .WithAuthor(ctx.Member.Nickname ?? ctx.Member.Username, icon_url: ctx.Member.AvatarUrl)
                        .WithColor(DiscordColor.Cyan)
                        .WithDescription($"{ctx.Member.Nickname ?? ctx.Member.Username} würfelte eine {value} mit einem W{sides}-Würfel.");

                await ctx.RespondAsync("https://www.youtube.com/watch?v=IPMnEmkoPFs", embed: embed);
                return;
            }

            embed = new DiscordEmbedBuilder()
                    .WithAuthor(ctx.Member.Nickname ?? ctx.Member.Username, icon_url: ctx.Member.AvatarUrl)
                    .WithColor(DiscordColor.Cyan)
                    .WithDescription($"{ctx.Member.Nickname ?? ctx.Member.Username} würfelte eine {value} mit einem W{sides}-Würfel.");

            await ctx.RespondAsync(embed: embed);
        }

        [Command("coin"), Aliases("münze")]
        [Description("Wirf eine Münze, die entweder Kopf oder Zahl ausgibt. Im Casino kostet dieser Wurf eine Münze.")]
        public async Task CoinCommandAsync(CommandContext ctx)
        {
            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                var channel = await ctx.Guild.GetConfigValueAsync<ulong>(Constants.Casino.Channel, 0, uow);

                if (ctx.Channel.Id == channel)
                {
                    if (!await CoinCooldown.ExecuteCheckAsync(ctx, false))
                    {
                        return;
                    }

                    var user = await ctx.User.GetUserAsync(uow);

                    if (user.Coins == 0)
                    {
                        var embed = Constants.Embeds.Presets.Error(description: "Du besitzt keinen Coin");
                        await ctx.RespondAsync(embed: embed);
                        return;
                    }

                    user.Coins--;
                    await uow.SaveChangesAsync();
                }
            }

            var value = Butler.RandomGenerator.Next(0, 2);
            await ctx.RespondAsync(embed: new DiscordEmbedBuilder()
                                          .WithAuthor(ctx.Member.Nickname ?? ctx.Member.Username, icon_url: ctx.Member.AvatarUrl)
                                          .WithColor(DiscordColor.Cyan)
                                          .WithDescription($"{ctx.Member.Nickname ?? ctx.Member.Username} warf eine Münze und bekam {(value == 0 ? "Kopf" : "Zahl")}."));
        }

        [Command("top"), GodfreyChannelType(Constants.Casino.Channel)]
        [Description("Liefert eine bestimmte Anzahl an Usern mit den meisten Coins aus.")]
        public async Task TopCommandAsync(CommandContext ctx, [Description("Die Menge an Usern, die ausgegeben werden soll.")] int many = 5)
        {
            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                many = Math.Min(Math.Max(1, many), 25);
                var list = await uow.Users.OrderByDescending(x => x.Coins).Take(many).ToListAsync();

                var embed = Constants.Embeds.Presets.Output("Coin Toplist");
                for (var i = 0; i < list.Count; i++)
                {
                    embed.AddField($"{i + 1} {list[i].Name}", $"{list[i].Coins} Coins");
                }

                await ctx.RespondAsync(embed: embed);
            }
        }

        [Command("info"), GodfreyChannelType(Constants.Casino.Channel)]
        [Description("Gibt aus wie viele Coins ein bestimmter User hat. Wenn kein User angegeben, wird der ausführende User genutzt.")]
        public async Task UserCommandAsync(CommandContext ctx, [Description("Der User, welcher abgefragt werden soll.")] DiscordUser user = null)
        {
            if (user == null)
            {
                user = ctx.User;
            }

            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                var usr = await user.GetUserAsync(uow);
                var embed = Constants.Embeds.Presets.Output("Coinauskunft", $"{usr.Name} besitzt {usr.Coins} Coins!");
                await ctx.RespondAsync(embed: embed);
            }
        }
    }
}
