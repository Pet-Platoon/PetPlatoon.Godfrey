using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Godfrey.Extensions;
using Godfrey.Models.Context;

namespace Godfrey.Commands.CasinoCommands
{
    public partial class CasinoCommands : BaseCommandModule
    {
        internal const float Half = 1f / 2;
        internal const float Third = 1f / 3;
        internal const float TwoThirds = Third * 2;

        [Command("casino")]
        [RequireUserPermissions(Permissions.Administrator)]
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
                        embed =
                                Constants.Embeds.Presets.Output(description:
                                                                "Der jetzige Channel ist bereits das Casino.");

                        await ctx.RespondAsync(embed: embed);
                        return;
                    }

                    if (!removeOld)
                    {
                        embed =
                                Constants.Embeds.Presets.Error(description:
                                                               "Es existiert bereits ein Casino für diesen Server. Um den jetzigen Channel als Casino zu nutzen, hänge ein `true` an den Befehl an");

                        await ctx.RespondAsync(embed: embed);
                        return;
                    }

                    channel = ctx.Channel.Id;
                    await ctx.Guild.SetConfigValueAsync(Constants.Casino.Channel, channel, uow);

                    embed =
                            Constants.Embeds.Presets.Success(description:
                                                             "Der Channel für das Casino wurde erfolgreich auf den jetzigen Channel geändert.");
                    await ctx.RespondAsync(embed: embed);
                    return;
                }

                channel = ctx.Channel.Id;
                await ctx.Guild.SetConfigValueAsync(Constants.Casino.Channel, channel, uow);

                embed =
                        Constants.Embeds.Presets.Success(description:
                                                         "Der Channel für das Casino wurde erfolgreich auf den jetzigen Channel gesetzt.");
                await ctx.RespondAsync(embed: embed);
            }
        }
    }
}
