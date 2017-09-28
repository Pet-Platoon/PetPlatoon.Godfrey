using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Godfrey.Helpers;
using Godfrey.Models.Context;
using Godfrey.Models.Quotes;
using Microsoft.EntityFrameworkCore;

namespace Godfrey.Commands
{
    [Group("quote", CanInvokeWithoutSubcommand = true), Aliases("kwot", "<:leroy:230337206751854592>")]
    public class QuoteCommand
    {
        public static DateTime LastRandomQuote;

        public Task ExecuteGroupAsync(CommandContext ctx) => RandomQuoteAsync(ctx);

        #region RandomQuote

        [Command("random")]
        public async Task RandomQuoteAsync(CommandContext ctx)
        {
            await ctx.Message.DeleteAsync();

            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                var currentTime = DateTime.UtcNow;
                var lastRandomQuote = await ConfigHelper.GetValueAsync(ctx.Guild, "quote.time.last", new DateTime(), uow);
                var quoteDowntime = await ConfigHelper.GetValueAsync(ctx.Guild, "quote.time.downtime", TimeSpan.FromSeconds(300), uow);
                var infoShowtime = await ConfigHelper.GetValueAsync(ctx.Guild, "quote.time.info", TimeSpan.FromSeconds(30), uow);

                if (lastRandomQuote.Add(quoteDowntime) > currentTime)
                {
                    await ctx.Member.SendMessageAsync($"Du kannst in {lastRandomQuote.Add(quoteDowntime) - currentTime} wieder einen Quote anfordern!");
                    return;
                }

                lastRandomQuote = DateTime.UtcNow;
                await ConfigHelper.SetValueAsync(ctx.Guild, "quote.time.last", lastRandomQuote, uow);

                DiscordMessage msg;
                if (!await uow.Quotes.AnyAsync(x => x.GuildId == ctx.Guild.Id))
                {
                    msg = await ctx.RespondAsync("Es wurden keine Quotes gefunden.");
                    await Task.Delay(infoShowtime);
                    await msg.DeleteAsync();
                    return;
                }

                var quote = await uow.Quotes.Where(x => x.GuildId == ctx.Guild.Id).OrderBy(x => Butler.RandomGenerator.Next()).FirstOrDefaultAsync();
                if (quote == null)
                {
                    msg = await ctx.RespondAsync("Irgendwas ist schief gelaufen.");
                    await Task.Delay(infoShowtime);
                    await msg.DeleteAsync();
                    return;
                }

                var embed = new DiscordEmbedBuilder()
                    .WithAuthor(quote.AuthorName)
                    .WithFooter($"Zitiert von {quote.QuoterName} | Erstellt: {quote.CreatedAt}")
                    .WithDescription(quote.Message)
                    .WithColor(DiscordColor.Orange)
                    .Build();

                await ctx.RespondAsync($"Quote [#{quote.Id}]:", embed: embed);
            }
        }

        #endregion RandomQuote

        #region AddQuote

        [Command("add")]
        public async Task AddQuoteAsync(CommandContext ctx, ulong id)
        {
            await ctx.Message.DeleteAsync();

            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                var allowedUsers = await ConfigHelper.GetValueAsync(ctx.Guild, "quote.permission.users", new ulong[] { });
                var allowedRoles = await ConfigHelper.GetValueAsync(ctx.Guild, "quote.permission.roles", new ulong[] { });
                var blockedUsers = await ConfigHelper.GetValueAsync(ctx.Guild, "quote.permission.users.blocked", new ulong[] { });
                var blockedRoles = await ConfigHelper.GetValueAsync(ctx.Guild, "quote.permission.roles.blocked", new ulong[] { });
                var infoShowtime = await ConfigHelper.GetValueAsync(ctx.Guild, "quote.time.info", TimeSpan.FromSeconds(30), uow);
                DiscordMessage msg;

                if (blockedUsers.Any(x => x == ctx.User.Id) || ctx.Member.Roles.Any(x => blockedRoles.Contains(x.Id)))
                {
                    msg = await ctx.RespondAsync("Du bist ausgeschlossen.");
                    await Task.Delay(infoShowtime);
                    await msg.DeleteAsync();
                    return;
                }

                if ((allowedUsers.Any() || allowedRoles.Any()) && !(allowedUsers.Any(x => x == ctx.User.Id) && ctx.Member.Roles.Any(x => allowedRoles.Contains(x.Id))))
                {
                    msg = await ctx.RespondAsync("Du bist dazu nicht berechtigt.");
                    await Task.Delay(infoShowtime);
                    await msg.DeleteAsync();
                    return;
                }

                var message = await ctx.Channel.GetMessageAsync(id);

                if (message == null)
                {
                    msg = await ctx.RespondAsync("Ich kann keine Nachricht mit dieser Id in diesem Channel finden.");
                    await Task.Delay(infoShowtime);
                    await msg.DeleteAsync();
                    return;
                }

                if (message.Author.Id == ctx.Member.Id && !ctx.Member.PermissionsIn(ctx.Channel).HasFlag(Permissions.Administrator))
                {
                    msg = await ctx.RespondAsync("Du darfst dich nicht selber quoten!");
                    await Task.Delay(infoShowtime);
                    await msg.DeleteAsync();
                    return;
                }

                if (await uow.Quotes.AnyAsync(x => x.MessageId == id))
                {
                    msg = await ctx.RespondAsync("Diese Nachricht wurde bereits gequoted.");
                    await Task.Delay(infoShowtime);
                    await msg.DeleteAsync();
                    return;
                }

                msg = await ctx.RespondAsync("Füge Quote hinzu...");

                var quote = new Quote
                {
                    AuthorId = message.Author.Id,
                    AuthorName = message.Author.Username,
                    QuoterId = ctx.User.Id,
                    QuoterName = ctx.User.Username,
                    GuildId = ctx.Guild.Id,
                    ChannelId = ctx.Channel.Id,
                    MessageId = message.Id,
                    Message = message.Content,
                    CreatedAt = message.CreationTimestamp.UtcDateTime
                };

                var trackingQuote = await uow.Quotes.AddAsync(quote);
                await uow.SaveChangesAsync();
                var entity = trackingQuote.Entity;

                var embed = new DiscordEmbedBuilder()
                    .WithAuthor(entity.AuthorName)
                    .WithFooter($"Zitiert von {entity.QuoterName} | Erstellt: {entity.CreatedAt}")
                    .WithDescription(entity.Message)
                    .WithColor(DiscordColor.Green)
                    .Build();

                await msg.ModifyAsync($"Quote hinzugefügt [#{entity.Id}]:", embed);
                await Task.Delay(infoShowtime);
                await msg.DeleteAsync();
            }
        }

        #endregion AddQuote

        #region Permissions

        #region Roles

        [Command("grantrole")]
        public async Task GrantAsync(CommandContext ctx, DiscordRole role)
        {
            await ctx.Message.DeleteAsync();

            if (!ctx.Member.PermissionsIn(ctx.Channel).HasFlag(Permissions.Administrator))
            {
                await ctx.RespondAsync("Du bist dazu nicht berechtigt.");
                return;
            }

            DiscordEmbedBuilder embedBuilder;
            DiscordMessage msg;

            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                var infoShowtime = await ConfigHelper.GetValueAsync(ctx.Guild, "quote.time.info", TimeSpan.FromSeconds(30), uow);
                var allowedRoles = (await ConfigHelper.GetValueAsync(ctx.Guild, "quote.permission.roles", new ulong[] { }, uow)).ToList();

                if (allowedRoles.Contains(role.Id))
                {
                    embedBuilder = new DiscordEmbedBuilder()
                        .WithColor(DiscordColor.Red)
                        .WithDescription($"Die Rolle {role.Mention} kann bereits Quotes hinzufügen.");
                    msg = await ctx.RespondAsync(embed: embedBuilder.Build());
                    await Task.Delay(infoShowtime);
                    await msg.DeleteAsync();
                    return;
                }
                
                allowedRoles.Add(role.Id);
                await ConfigHelper.SetValueAsync(ctx.Guild, "quote.permission.roles", allowedRoles.ToArray(), uow);

                embedBuilder = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Green)
                    .WithDescription($"Die Rolle {role.Mention} kann nun Quotes hinzufügen.");
                msg = await ctx.RespondAsync(embed: embedBuilder.Build());
                await Task.Delay(infoShowtime);
                await msg.DeleteAsync();
            }
        }

        [Command("revokerole")]
        public async Task RevokeAsync(CommandContext ctx, DiscordRole role)
        {
            await ctx.Message.DeleteAsync();

            if (!ctx.Member.PermissionsIn(ctx.Channel).HasFlag(Permissions.Administrator))
            {
                await ctx.RespondAsync("Du bist dazu nicht berechtigt.");
                return;
            }

            DiscordEmbedBuilder embedBuilder;
            DiscordMessage msg;

            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                var infoShowtime = await ConfigHelper.GetValueAsync(ctx.Guild, "quote.time.info", TimeSpan.FromSeconds(30), uow);
                var allowedRoles = (await ConfigHelper.GetValueAsync(ctx.Guild, "quote.permission.roles", new ulong[] { }, uow)).ToList();

                if (allowedRoles.Contains(role.Id))
                {
                    embedBuilder = new DiscordEmbedBuilder()
                        .WithColor(DiscordColor.Red)
                        .WithDescription($"Die Rolle {role.Mention} kann keine Quotes hinzufügen.");
                    msg = await ctx.RespondAsync(embed: embedBuilder.Build());
                    await Task.Delay(infoShowtime);
                    await msg.DeleteAsync();
                    return;
                }

                allowedRoles.Remove(role.Id);
                await ConfigHelper.SetValueAsync(ctx.Guild, "quote.permission.roles", allowedRoles.ToArray(), uow);

                embedBuilder = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Green)
                    .WithDescription($"Die Rolle {role.Mention} kann nun keine Quotes mehr hinzufügen.");
                msg = await ctx.RespondAsync(embed: embedBuilder.Build());
                await Task.Delay(infoShowtime);
                await msg.DeleteAsync();
            }
        }

        [Command("unblockrole")]
        public async Task UnblockAsync(CommandContext ctx, DiscordRole role)
        {
            await ctx.Message.DeleteAsync();

            if (!ctx.Member.PermissionsIn(ctx.Channel).HasFlag(Permissions.Administrator))
            {
                await ctx.RespondAsync("Du bist dazu nicht berechtigt.");
                return;
            }

            DiscordEmbedBuilder embedBuilder;
            DiscordMessage msg;

            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                var infoShowtime = await ConfigHelper.GetValueAsync(ctx.Guild, "quote.time.info", TimeSpan.FromSeconds(30), uow);
                var blockedRoles = (await ConfigHelper.GetValueAsync(ctx.Guild, "quote.permission.roles.blocked", new ulong[] { }, uow)).ToList();

                if (blockedRoles.Contains(role.Id))
                {
                    embedBuilder = new DiscordEmbedBuilder()
                        .WithColor(DiscordColor.Red)
                        .WithDescription($"Die Rolle {role.Mention} ist nicht geblockt.");
                    msg = await ctx.RespondAsync(embed: embedBuilder.Build());
                    await Task.Delay(infoShowtime);
                    await msg.DeleteAsync();
                    return;
                }
                
                blockedRoles.Remove(role.Id);
                await ConfigHelper.SetValueAsync(ctx.Guild, "quote.permission.roles.blocked", blockedRoles.ToArray(), uow);

                embedBuilder = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Green)
                    .WithDescription($"Die Rolle {role.Mention} ist nun nicht mehr geblockt.");
                msg = await ctx.RespondAsync(embed: embedBuilder.Build());
                await Task.Delay(infoShowtime);
                await msg.DeleteAsync();
            }
        }

        [Command("blockrole")]
        public async Task BlockAsync(CommandContext ctx, DiscordRole role)
        {
            await ctx.Message.DeleteAsync();

            if (!ctx.Member.PermissionsIn(ctx.Channel).HasFlag(Permissions.Administrator))
            {
                await ctx.RespondAsync("Du bist dazu nicht berechtigt.");
                return;
            }

            DiscordEmbedBuilder embedBuilder;
            DiscordMessage msg;

            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                var infoShowtime = await ConfigHelper.GetValueAsync(ctx.Guild, "quote.time.info", TimeSpan.FromSeconds(30), uow);
                var blockedRoles = (await ConfigHelper.GetValueAsync(ctx.Guild, "quote.permission.roles.blocked", new ulong[] { }, uow)).ToList();

                if (blockedRoles.Contains(role.Id))
                {
                    embedBuilder = new DiscordEmbedBuilder()
                        .WithColor(DiscordColor.Red)
                        .WithDescription($"Die Rolle {role.Mention} ist bereits geblockt.");
                    msg = await ctx.RespondAsync(embed: embedBuilder.Build());
                    await Task.Delay(infoShowtime);
                    await msg.DeleteAsync();
                    return;
                }

                blockedRoles.Add(role.Id);
                await ConfigHelper.SetValueAsync(ctx.Guild, "quote.permission.roles.blocked", blockedRoles.ToArray(), uow);

                embedBuilder = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Green)
                    .WithDescription($"Die Rolle {role.Mention} ist nun geblockt.");
                msg = await ctx.RespondAsync(embed: embedBuilder.Build());
                await Task.Delay(infoShowtime);
                await msg.DeleteAsync();
            }
        }

        #endregion Roles

        #region Members

        [Command("grantmember")]
        public async Task GrantAsync(CommandContext ctx, DiscordUser member)
        {
            await ctx.Message.DeleteAsync();

            if (!ctx.Member.PermissionsIn(ctx.Channel).HasFlag(Permissions.Administrator))
            {
                await ctx.RespondAsync("Du bist dazu nicht berechtigt.");
                return;
            }

            DiscordEmbedBuilder embedBuilder;
            DiscordMessage msg;

            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                var infoShowtime = await ConfigHelper.GetValueAsync(ctx.Guild, "quote.time.info", TimeSpan.FromSeconds(30), uow);
                var allowedUsers = (await ConfigHelper.GetValueAsync(ctx.Guild, "quote.permission.users", new ulong[] { }, uow)).ToList();

                if (allowedUsers.Contains(member.Id))
                {
                    embedBuilder = new DiscordEmbedBuilder()
                        .WithColor(DiscordColor.Red)
                        .WithDescription($"Der Member {member.Mention} kann bereits Quotes hinzufügen.");
                    msg = await ctx.RespondAsync(embed: embedBuilder.Build());
                    await Task.Delay(infoShowtime);
                    await msg.DeleteAsync();
                    return;
                }

                allowedUsers.Add(member.Id);
                await ConfigHelper.SetValueAsync(ctx.Guild, "quote.permission.users", allowedUsers.ToArray(), uow);

                embedBuilder = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Green)
                    .WithDescription($"Der Member {member.Mention} kann nun Quotes hinzufügen.");
                msg = await ctx.RespondAsync(embed: embedBuilder.Build());
                await Task.Delay(infoShowtime);
                await msg.DeleteAsync();
            }
        }

        [Command("revokemember")]
        public async Task RevokeAsync(CommandContext ctx, DiscordUser member)
        {
            await ctx.Message.DeleteAsync();

            if (!ctx.Member.PermissionsIn(ctx.Channel).HasFlag(Permissions.Administrator))
            {
                await ctx.RespondAsync("Du bist dazu nicht berechtigt.");
                return;
            }

            DiscordEmbedBuilder embedBuilder;
            DiscordMessage msg;

            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                var infoShowtime = await ConfigHelper.GetValueAsync(ctx.Guild, "quote.time.info", TimeSpan.FromSeconds(30), uow);
                var allowedUsers = (await ConfigHelper.GetValueAsync(ctx.Guild, "quote.permission.users", new ulong[] { }, uow)).ToList();

                if (allowedUsers.Contains(member.Id))
                {
                    embedBuilder = new DiscordEmbedBuilder()
                        .WithColor(DiscordColor.Red)
                        .WithDescription($"Der Member {member.Mention} kann keine Quotes hinzufügen.");
                    msg = await ctx.RespondAsync(embed: embedBuilder.Build());
                    await Task.Delay(infoShowtime);
                    await msg.DeleteAsync();
                    return;
                }

                allowedUsers.Remove(member.Id);
                await ConfigHelper.SetValueAsync(ctx.Guild, "quote.permission.users", allowedUsers.ToArray(), uow);

                embedBuilder = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Green)
                    .WithDescription($"Der Member {member.Mention} kann nun keine Quotes mehr hinzufügen.");
                msg = await ctx.RespondAsync(embed: embedBuilder.Build());
                await Task.Delay(infoShowtime);
                await msg.DeleteAsync();
            }
        }

        [Command("unblockmember")]
        public async Task UnblockAsync(CommandContext ctx, DiscordUser member)
        {
            await ctx.Message.DeleteAsync();

            if (!ctx.Member.PermissionsIn(ctx.Channel).HasFlag(Permissions.Administrator))
            {
                await ctx.RespondAsync("Du bist dazu nicht berechtigt.");
                return;
            }

            DiscordEmbedBuilder embedBuilder;
            DiscordMessage msg;

            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                var infoShowtime = await ConfigHelper.GetValueAsync(ctx.Guild, "quote.time.info", TimeSpan.FromSeconds(30), uow);
                var blockedUsers = (await ConfigHelper.GetValueAsync(ctx.Guild, "quote.permission.users.blocked", new ulong[] { }, uow)).ToList();

                if (!blockedUsers.Contains(member.Id))
                {
                    embedBuilder = new DiscordEmbedBuilder()
                        .WithColor(DiscordColor.Red)
                        .WithDescription($"Der Member {member.Mention} ist nicht geblockt.");
                    msg = await ctx.RespondAsync(embed: embedBuilder.Build());
                    await Task.Delay(infoShowtime);
                    await msg.DeleteAsync();
                    return;
                }

                blockedUsers.Remove(member.Id);
                await ConfigHelper.SetValueAsync(ctx.Guild, "quote.permission.users.blocked", blockedUsers.ToArray(), uow);

                embedBuilder = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Green)
                    .WithDescription($"Der Member {member.Mention} ist nun nicht mehr geblockt.");
                msg = await ctx.RespondAsync(embed: embedBuilder.Build());
                await Task.Delay(infoShowtime);
                await msg.DeleteAsync();
            }
        }

        [Command("blockmember")]
        public async Task BlockAsync(CommandContext ctx, DiscordMember member)
        {
            await ctx.Message.DeleteAsync();

            if (!ctx.Member.PermissionsIn(ctx.Channel).HasFlag(Permissions.Administrator))
            {
                await ctx.RespondAsync("Du bist dazu nicht berechtigt.");
                return;
            }

            DiscordEmbedBuilder embedBuilder;
            DiscordMessage msg;

            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                var infoShowtime = await ConfigHelper.GetValueAsync(ctx.Guild, "quote.time.info", TimeSpan.FromSeconds(30), uow);
                var blockedUsers = (await ConfigHelper.GetValueAsync(ctx.Guild, "quote.permission.users.blocked", new ulong[] { }, uow)).ToList();

                if (blockedUsers.Contains(member.Id))
                {
                    embedBuilder = new DiscordEmbedBuilder()
                        .WithColor(DiscordColor.Red)
                        .WithDescription($"Der Member {member.Mention} ist bereits geblockt.");
                    msg = await ctx.RespondAsync(embed: embedBuilder.Build());
                    await Task.Delay(infoShowtime);
                    await msg.DeleteAsync();
                    return;
                }

                blockedUsers.Add(member.Id);
                await ConfigHelper.SetValueAsync(ctx.Guild, "quote.permission.users.blocked", blockedUsers.ToArray(), uow);

                embedBuilder = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Green)
                    .WithDescription($"Der Member {member.Mention} ist nun geblockt.");
                msg = await ctx.RespondAsync(embed: embedBuilder.Build());
                await Task.Delay(infoShowtime);
                await msg.DeleteAsync();
            }
        }

        #endregion Members

        #endregion Permissions

        #region Configs

        [Command("downtime")]
        public async Task DowntimeAsync(CommandContext ctx, TimeSpan time = default(TimeSpan))
        {
            await ctx.Message.DeleteAsync();

            if (!ctx.Member.PermissionsIn(ctx.Channel).HasFlag(Permissions.Administrator))
            {
                await ctx.RespondAsync("Du bist dazu nicht berechtigt.");
                return;
            }

            DiscordMessage msg;
            DiscordEmbedBuilder embedBuilder;

            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                var infoShowtime = await ConfigHelper.GetValueAsync(ctx.Guild, "quote.time.info", TimeSpan.FromSeconds(30), uow);

                if (time == default(TimeSpan))
                {
                    var downtime = await ConfigHelper.GetValueAsync(ctx.Guild, "quote.time.downtime", TimeSpan.FromSeconds(300), uow);
                    embedBuilder = new DiscordEmbedBuilder()
                        .WithColor(DiscordColor.Orange)
                        .WithDescription($"Quote-Downtime steht auf: {downtime}");
                    msg = await ctx.RespondAsync(embed: embedBuilder.Build());
                    await Task.Delay(infoShowtime);
                    await msg.DeleteAsync();
                    return;
                }

                await ConfigHelper.SetValueAsync(ctx.Guild, "quote.time.downtime", time, uow);
                embedBuilder = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Green)
                    .WithDescription($"Quote-Downtime steht auf nun auf: {time}");
                msg = await ctx.RespondAsync(embed: embedBuilder.Build());
                await Task.Delay(infoShowtime);
                await msg.DeleteAsync();
            }
        }

        [Command("infotime")]
        public async Task InfotimeAsync(CommandContext ctx, TimeSpan time = default(TimeSpan))
        {
            await ctx.Message.DeleteAsync();

            if (!ctx.Member.PermissionsIn(ctx.Channel).HasFlag(Permissions.Administrator))
            {
                await ctx.RespondAsync("Du bist dazu nicht berechtigt.");
                return;
            }

            DiscordMessage msg;
            DiscordEmbedBuilder embedBuilder;

            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                var infoShowtime = await ConfigHelper.GetValueAsync(ctx.Guild, "quote.time.info", TimeSpan.FromSeconds(30), uow);

                if (time == default(TimeSpan))
                {
                    embedBuilder = new DiscordEmbedBuilder()
                        .WithColor(DiscordColor.Orange)
                        .WithDescription($"Quote-Infotime steht auf: {infoShowtime}");
                    msg = await ctx.RespondAsync(embed: embedBuilder.Build());
                    await Task.Delay(infoShowtime);
                    await msg.DeleteAsync();
                    return;
                }

                infoShowtime = time;

                await ConfigHelper.SetValueAsync(ctx.Guild, "quote.time.info", time, uow);
                embedBuilder = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Green)
                    .WithDescription($"Quote-Infotime steht auf nun auf: {time}");
                msg = await ctx.RespondAsync(embed: embedBuilder.Build());
                await Task.Delay(infoShowtime);
                await msg.DeleteAsync();
            }
        }

        #endregion Configs
    }
}
