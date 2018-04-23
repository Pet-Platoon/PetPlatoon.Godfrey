using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.EventArgs;
using Godfrey.Extensions;
using Godfrey.Models.Context;

namespace Godfrey
{
    public partial class Butler
    {
        private async Task OnReadyAsync(ReadyEventArgs e)
        {
            await Task.Run(() => OnReady());
        }

        private void OnReady()
        {
            Client.DebugLogger.LogMessage(LogLevel.Info, "Butler",
                                          $"Connected as {Client.CurrentUser.Username} on shard {Client.ShardId}",
                                          DateTime.UtcNow);
        }

        private async Task OnGuildAvailable(GuildCreateEventArgs e)
        {
            using (var uow = await DatabaseContextFactory.CreateAsync(ButlerConfig.ConnectionString))
            {
                await e.Guild.GetServerAsync(uow);
            }
        }

        private async Task OnMessageCreated(MessageCreateEventArgs e)
        {
            using (var uow = await DatabaseContextFactory.CreateAsync(ButlerConfig.ConnectionString))
            {
                var user = await e.Author.GetUserAsync(uow);

                if (user.Name != e.Author.Username)
                {
                    user.Name = e.Author.Username;
                    await uow.SaveChangesAsync();
                }
            }
        }

        // ReSharper disable once UnusedMember.Local
        private async Task OnCommandErrored(CommandErrorEventArgs e)
        {
            await Task.Run(() =>
            {
                if (e.Exception is CommandNotFoundException)
                {
                    return;
                }

                e.Context.Client.DebugLogger.LogMessage(LogLevel.Error, "Butler",
                                                        $"{e.Context.User.Username} tried executing '{e.Command?.QualifiedName ?? "<unknown command>"}' but it errored: {e.Exception.Demystify()}",
                                                        DateTime.UtcNow);

                if (e.Exception.InnerException != null)
                {
                    Console.WriteLine(e.Exception.InnerException.Demystify());
                }

                //var embedBuilder = new DiscordEmbedBuilder()
                //        .WithTitle("Ein Fehler ist aufgetreten!")
                //        .WithDescription($"Ein Fehler im Command `{e.Command?.QualifiedName ?? "<unknown command>"}` ist aufgetreten:{Environment.NewLine}```{e.Exception.Message}```")
                //        .WithColor(DiscordColor.Red);
                //
                //await e.Context.RespondAsync(embed: embedBuilder.Build());
            });
        }
    }
}
