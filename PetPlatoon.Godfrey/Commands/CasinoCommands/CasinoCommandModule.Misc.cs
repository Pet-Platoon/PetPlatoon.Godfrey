using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using PetPlatoon.Godfrey.Attributes;
using PetPlatoon.Godfrey.Database;
using PetPlatoon.Godfrey.Extensions;

namespace PetPlatoon.Godfrey.Commands.CasinoCommands
{
    public partial class CasinoCommandModule
    {
        [Command("give"), GodfreyChannelType(Constants.Casino.Channel)]
        [Description("Gibt einem User eine bestimmte Menge an Coins.")]
        public async Task GiveCommandAsync(CommandContext ctx, [Description("Der User, der die Coins erhalten soll.")] DiscordUser to, [Description("Die Menge, die man dem User geben möchte.")] long amount)
        {
            var databaseContext = ctx.Services.GetService<DatabaseContext>();

            DiscordEmbedBuilder embed;

            var user = await ctx.User.GetUserAsync(databaseContext);
            var toUser = await to.GetUserAsync(databaseContext);
            user.LastCasinoCommandIssued = DateTime.UtcNow;
            await databaseContext.SaveChangesAsync();

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

            embed = Constants.Embeds.Presets.Success(description: $"{user.Name} gibt {toUser.Name} {amount} Coins.");
            await ctx.RespondAsync(embed: embed);
            await databaseContext.SaveChangesAsync();
        }

        [Command("steal"), Aliases("loot"), GodfreyChannelType(Constants.Casino.Channel), GodfreyCooldown(1, 300, Attributes.CooldownBucketType.User)]
        [Description("Startet den Versuch einem Mitspieler eine bestimme Menge an Coins zu stehlen. Es besteht eine 70/30 Chance, dass der User es **nicht** schaffst.")]
        public async Task StealCommandAsync(CommandContext ctx, [Description("Der User, dem man die Coins stehlen möchte.")] DiscordUser from, [Description("Die menge, die man dem User stehlen möchte.")] long amount)
        {
            var databaseContext = ctx.Services.GetService<DatabaseContext>();

            DiscordEmbedBuilder embed;
            var user = await ctx.User.GetUserAsync(databaseContext);
            var stealFrom = await from.GetUserAsync(databaseContext);
            user.LastCasinoCommandIssued = DateTime.UtcNow;
            await databaseContext.SaveChangesAsync();

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

            var percentage = Startup.Random.NextDouble();

            if (DateTime.UtcNow - stealFrom.LastCasinoCommandIssued > TimeSpan.FromDays(2))
            {
                percentage = 1;
            }

            if (percentage < 0.7)
            {
                embed = Constants.Embeds.Presets.Error("Du wurdest erwischt", $"Dein Gegner hat dich beim Stehlen erwischt und dich zum Krüppel geschlagen. Du verlierst {amount} Coins an {stealFrom.Name}.");
                user.Coins -= amount;
                stealFrom.Coins += amount;
                await ctx.RespondAsync(embed: embed);
                await databaseContext.SaveChangesAsync();
                return;
            }

            embed = Constants.Embeds.Presets.Success("Let's go sneaky beaky like", $"Du hast {amount} von {stealFrom.Name} gestohlen.");
            user.Coins += amount;
            stealFrom.Coins -= amount;
            await ctx.RespondAsync(embed: embed);
            await databaseContext.SaveChangesAsync();
        }
    }
}
