using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using PetPlatoon.Godfrey.Database;
using PetPlatoon.Godfrey.Extensions;

namespace PetPlatoon.Godfrey.Commands.CasinoCommands
{
    public partial class CasinoCommandModule : BaseCommandModule
    {
        internal const float Half = 1f / 2;
        internal const float Third = 1f / 3;
        internal const float TwoThirds = Third * 2;

        [Command("casino"), RequireUserPermissions(Permissions.Administrator)]
        public async Task CasinoCommandAsync(CommandContext ctx, bool removeOld = false)
        {
            var databaseContext = ctx.Services.GetService<DatabaseContext>();

            DiscordEmbed embed;
            var channel = await ctx.Guild.GetConfigValueAsync<ulong>(Constants.Casino.Channel, databaseContext);

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
                await ctx.Guild.SetConfigValueAsync(Constants.Casino.Channel, channel, databaseContext);

                embed = Constants.Embeds.Presets.Success(description: "Der Channel für das Casino wurde erfolgreich auf den jetzigen Channel geändert.");
                await ctx.RespondAsync(embed: embed);
                return;
            }

            channel = ctx.Channel.Id;
            await ctx.Guild.SetConfigValueAsync(Constants.Casino.Channel, channel, databaseContext);

            embed = Constants.Embeds.Presets.Success(description: "Der Channel für das Casino wurde erfolgreich auf den jetzigen Channel gesetzt.");
            await ctx.RespondAsync(embed: embed);
        }
    }
}
