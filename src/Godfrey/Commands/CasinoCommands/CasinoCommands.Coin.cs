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
        private static readonly GodfreyCooldownAttribute CoinCooldown = new GodfreyCooldownAttribute(1, Constants.Quotes.Times.Downtime, CooldownBucketType.User);

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
                    user.LastCasinoCommandIssued = DateTime.UtcNow;
                    await uow.SaveChangesAsync();

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
    }
}
