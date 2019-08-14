using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PetPlatoon.Godfrey.Attributes;
using PetPlatoon.Godfrey.Database;
using PetPlatoon.Godfrey.Extensions;

namespace PetPlatoon.Godfrey.Commands.CasinoCommands
{
    public partial class CasinoCommandModule
    {
        [Command("top"), GodfreyChannelType(Constants.Casino.Channel)]
        [Description("Liefert eine bestimmte Anzahl an Usern mit den meisten Coins aus.")]
        public async Task TopCommandAsync(CommandContext ctx, [Description("Die Menge an Usern, die ausgegeben werden soll.")] int many = 5)
        {
            var databaseContext = ctx.Services.GetService<DatabaseContext>();

            many = Math.Min(Math.Max(1, many), 25);
            var list = await databaseContext.Users.OrderByDescending(x => x.Coins).Take(many).ToListAsync();

            var embed = Constants.Embeds.Presets.Output("Coin Toplist");
            for (var i = 0; i < list.Count; i++)
            {
                embed.AddField($"{i + 1} {list[i].Name}", $"{list[i].Coins} Coins");
            }

            await ctx.RespondAsync(embed: embed);
        }

        [Command("info"), GodfreyChannelType(Constants.Casino.Channel)]
        [Description("Gibt aus wie viele Coins ein bestimmter User hat. Wenn kein User angegeben, wird der ausführende User genutzt.")]
        public async Task UserCommandAsync(CommandContext ctx, [Description("Der User, welcher abgefragt werden soll.")] DiscordUser user = null)
        {
            if (user == null)
            {
                user = ctx.User;
            }

            var databaseContext = ctx.Services.GetService<DatabaseContext>();

            var usr = await user.GetUserAsync(databaseContext);
            var embed = Constants.Embeds.Presets.Output("Coinauskunft", $"{usr.Name} besitzt {usr.Coins} Coins!");
            await ctx.RespondAsync(embed: embed);
        }
    }
}
