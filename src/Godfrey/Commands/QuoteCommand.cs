﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Godfrey.Exceptions;
using Godfrey.Extensions;
using Godfrey.Helpers;
using Godfrey.Models.Context;
using Godfrey.Models.Quotes;
using Godfrey.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace Godfrey.Commands
{
    [Group("quote", CanInvokeWithoutSubcommand = true), Aliases("kwot", "<:leroy:230337206751854592>", "<@240871410954665985>")]
    public class QuoteCommand
    {
        public static DateTime LastRandomQuote;

        public Task ExecuteGroupAsync(CommandContext ctx) => RandomQuoteAsync(ctx);

        #region RandomQuote

        [Command("random")]
        public async Task RandomQuoteAsync(CommandContext ctx)
        {
            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                var currentTime = DateTime.UtcNow;
                var lastRandomQuote = await ConfigHelper.GetValueAsync(ctx.Guild, "quote.time.last", new DateTime(), uow);
                var quoteDowntime = await ConfigHelper.GetValueAsync(ctx.Guild, "quote.time.downtime", TimeSpan.FromSeconds(300), uow);

                if (lastRandomQuote.Add(quoteDowntime) > currentTime)
                {
                    await ctx.Member.SendMessageAsync($"Du kannst in {(lastRandomQuote.Add(quoteDowntime) - currentTime).PrettyPrint()} wieder einen Quote anfordern!");
                    return;
                }

                lastRandomQuote = DateTime.UtcNow;
                await ConfigHelper.SetValueAsync(ctx.Guild, "quote.time.last", lastRandomQuote, uow);
                
                if (!await uow.Quotes.AnyAsync(x => x.Server.Id == ctx.Guild.Id))
                {
                    throw new MissingQuotesException("Es wurden keine Quotes gefunden");
                }

                var quote = await uow.Quotes.Where(x => x.Server.Id == ctx.Guild.Id).OrderBy(x => Butler.RandomGenerator.Next()).FirstOrDefaultAsync();
                if (quote == null)
                {
                    throw new Exception("Unexpected error occurred. Quote is null after quote count check");
                }

                var embed = new DiscordEmbedBuilder()
                    .WithAuthor(quote.Author.Name)
                    .WithFooter($"Zitiert von {quote.Quoter.Name} | Erstellt: {quote.CreatedAt.PrettyPrint()}")
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
            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                var allowedUsers = await ConfigHelper.GetValueAsync(ctx.Guild, "quote.permission.users", new ulong[] { });
                var allowedRoles = await ConfigHelper.GetValueAsync(ctx.Guild, "quote.permission.roles", new ulong[] { });
                var blockedUsers = await ConfigHelper.GetValueAsync(ctx.Guild, "quote.permission.users.blocked", new ulong[] { });
                var blockedRoles = await ConfigHelper.GetValueAsync(ctx.Guild, "quote.permission.roles.blocked", new ulong[] { });

                var quoter = await uow.Users.FirstOrDefaultAsync(x => x.Id == ctx.User.Id);

                if (quoter == null)
                {
                    quoter = new User
                    {
                        
                    };
                }

                if (blockedUsers.Any(x => x == ctx.User.Id) || ctx.Member.Roles.Any(x => blockedRoles.Contains(x.Id)))
                {
                    throw new UsageBlockedException("Du bist von der Nutzung ausgeschlossen.");
                }

                // ctx.Member.Roles.Any(x => allowedRoles.Contains(x.Id))
                if (allowedRoles.Any() && !allowedUsers.Any())
                {
                    if (!ctx.Member.Roles.Any(x => allowedRoles.Contains(x.Id)))
                    {
                        throw new UsageBlockedException("Du bist dazu nicht berechtigt.");
                    }
                }

                if (!allowedRoles.Any() && allowedUsers.Any())
                {
                    if (allowedUsers.All(x => x != ctx.User.Id))
                    {
                        throw new UsageBlockedException("Du bist dazu nicht berechtigt.");
                    }
                }

                if (allowedRoles.Any() && allowedUsers.Any())
                {
                    if (!ctx.Member.Roles.Any(x => allowedRoles.Contains(x.Id)) && allowedUsers.All(x => x != ctx.User.Id))
                    {
                        throw new UsageBlockedException("Du bist dazu nicht berechtigt.");
                    }
                }

                var message = await ctx.Channel.GetMessageAsync(id);

                if (message == null)
                {
                    throw new Exception("Es wurde keine Nachricht mit dieser Id in diesem Channel gefunden.");
                }

                if (message.Author.Id == ctx.Member.Id && !ctx.Member.PermissionsIn(ctx.Channel).HasFlag(Permissions.Administrator))
                {
                    throw new Exception("Du darfst dich nicht selber quoten!");
                }

                if (await uow.Quotes.AnyAsync(x => x.Id == id))
                {
                    throw new Exception("Diese Nachricht wurde bereits gequoted.");
                }

                var msg = await ctx.RespondAsync("Füge Quote hinzu...");

                var quote = new Quote
                {
                    Id = message.Id,
                    Message = message.Content,
                    AuthorId = message.Author.Id,
                    QuoterId = ctx.User.Id,
                    ServerId = ctx.Guild.Id,
                    ChannelId = ctx.Channel.Id,
                    CreatedAt = message.CreationTimestamp.UtcDateTime
                };

                var trackingQuote = await uow.Quotes.AddAsync(quote);
                await uow.SaveChangesAsync();
                var entity = trackingQuote.Entity;

                var embed = new DiscordEmbedBuilder()
                    .WithAuthor(entity.Author.Name)
                    .WithFooter($"Zitiert von {entity.Quoter.Name} | Erstellt: {entity.CreatedAt.PrettyPrint()}")
                    .WithDescription(entity.Message)
                    .WithColor(DiscordColor.Green)
                    .Build();

                await msg.ModifyAsync($"Quote hinzugefügt [#{entity.Id}]:", embed);
            }
        }

        #endregion AddQuote

        #region DeleteQuote

        [Command("delete")]
        public async Task DeleteQuoteAsync(CommandContext ctx, ulong id)
        {
            if (!ctx.Member.PermissionsIn(ctx.Channel).HasFlag(Permissions.Administrator))
            {
                throw new UsageBlockedException("Du bist dazu nicht berechtigt.");
            }

            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                var quotes = uow.Quotes.Where(x => x.ServerId == ctx.Guild.Id);
                var quote = await quotes.FirstOrDefaultAsync(x => x.Id == id);

                if (quote == null)
                {
                    throw new KeyNotFoundException($"Der Quote mit der Id \"{id}\" wurde nicht gefunden oder gehört nicht zu diesem Server.");
                }

                uow.Quotes.Remove(quote);

                await uow.SaveChangesAsync();
                
                var embed = new DiscordEmbedBuilder()
                        .WithAuthor(quote.Author.Name)
                        .WithFooter($"Zitiert von {quote.Quoter.Name} | Erstellt: {quote.CreatedAt.PrettyPrint()}")
                        .WithDescription(quote.Message)
                        .WithColor(DiscordColor.Red)
                        .Build();

                await ctx.RespondAsync($"Quote entfernt [#{quote.Id}; Message-Id: {quote.Id}; Channel-Id: {quote.ChannelId}]:", embed: embed);
            }
        }

        #endregion DeleteQuote

        #region Permissions

        #region Roles

        [Command("grantrole"), RequirePermissions(Permissions.Administrator)]
        public async Task GrantAsync(CommandContext ctx, DiscordRole role)
        {
            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                var allowedRoles = (await ConfigHelper.GetValueAsync(ctx.Guild, "quote.permission.roles", new ulong[] { }, uow)).ToList();
                DiscordEmbedBuilder embedBuilder;

                if (allowedRoles.Contains(role.Id))
                {
                    embedBuilder = new DiscordEmbedBuilder()
                        .WithColor(DiscordColor.Red)
                        .WithDescription($"Die Rolle {role.Mention} kann bereits Quotes hinzufügen.");
                    await ctx.RespondAsync(embed: embedBuilder.Build());
                    return;
                }
                
                allowedRoles.Add(role.Id);
                await ConfigHelper.SetValueAsync(ctx.Guild, "quote.permission.roles", allowedRoles.ToArray(), uow);

                embedBuilder = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Green)
                    .WithDescription($"Die Rolle {role.Mention} kann nun Quotes hinzufügen.");
                await ctx.RespondAsync(embed: embedBuilder.Build());
            }
        }

        [Command("revokerole"), RequirePermissions(Permissions.Administrator)]
        public async Task RevokeAsync(CommandContext ctx, DiscordRole role)
        {
            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                var allowedRoles = (await ConfigHelper.GetValueAsync(ctx.Guild, "quote.permission.roles", new ulong[] { }, uow)).ToList();
                DiscordEmbedBuilder embedBuilder;

                if (allowedRoles.Contains(role.Id))
                {
                    embedBuilder = new DiscordEmbedBuilder()
                        .WithColor(DiscordColor.Red)
                        .WithDescription($"Die Rolle {role.Mention} kann keine Quotes hinzufügen.");
                    await ctx.RespondAsync(embed: embedBuilder.Build());
                    return;
                }

                allowedRoles.Remove(role.Id);
                await ConfigHelper.SetValueAsync(ctx.Guild, "quote.permission.roles", allowedRoles.ToArray(), uow);

                embedBuilder = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Green)
                    .WithDescription($"Die Rolle {role.Mention} kann nun keine Quotes mehr hinzufügen.");
                await ctx.RespondAsync(embed: embedBuilder.Build());
            }
        }

        [Command("unblockrole"), RequirePermissions(Permissions.Administrator)]
        public async Task UnblockAsync(CommandContext ctx, DiscordRole role)
        {
            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                var blockedRoles = (await ConfigHelper.GetValueAsync(ctx.Guild, "quote.permission.roles.blocked", new ulong[] { }, uow)).ToList();
                DiscordEmbedBuilder embedBuilder;

                if (blockedRoles.Contains(role.Id))
                {
                    embedBuilder = new DiscordEmbedBuilder()
                        .WithColor(DiscordColor.Red)
                        .WithDescription($"Die Rolle {role.Mention} ist nicht geblockt.");
                    await ctx.RespondAsync(embed: embedBuilder.Build());
                    return;
                }
                
                blockedRoles.Remove(role.Id);
                await ConfigHelper.SetValueAsync(ctx.Guild, "quote.permission.roles.blocked", blockedRoles.ToArray(), uow);

                embedBuilder = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Green)
                    .WithDescription($"Die Rolle {role.Mention} ist nun nicht mehr geblockt.");
                await ctx.RespondAsync(embed: embedBuilder.Build());
            }
        }

        [Command("blockrole"), RequirePermissions(Permissions.Administrator)]
        public async Task BlockAsync(CommandContext ctx, DiscordRole role)
        {
            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                var blockedRoles = (await ConfigHelper.GetValueAsync(ctx.Guild, "quote.permission.roles.blocked", new ulong[] { }, uow)).ToList();
                DiscordEmbedBuilder embedBuilder;

                if (blockedRoles.Contains(role.Id))
                {
                    embedBuilder = new DiscordEmbedBuilder()
                        .WithColor(DiscordColor.Red)
                        .WithDescription($"Die Rolle {role.Mention} ist bereits geblockt.");
                    await ctx.RespondAsync(embed: embedBuilder.Build());
                    return;
                }

                blockedRoles.Add(role.Id);
                await ConfigHelper.SetValueAsync(ctx.Guild, "quote.permission.roles.blocked", blockedRoles.ToArray(), uow);

                embedBuilder = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Green)
                    .WithDescription($"Die Rolle {role.Mention} ist nun geblockt.");
                await ctx.RespondAsync(embed: embedBuilder.Build());
            }
        }

        #endregion Roles

        #region Members

        [Command("grantmember"), RequirePermissions(Permissions.Administrator)]
        public async Task GrantAsync(CommandContext ctx, DiscordUser member)
        {
            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                var allowedUsers = (await ConfigHelper.GetValueAsync(ctx.Guild, "quote.permission.users", new ulong[] { }, uow)).ToList();
                DiscordEmbedBuilder embedBuilder;

                if (allowedUsers.Contains(member.Id))
                {
                    embedBuilder = new DiscordEmbedBuilder()
                        .WithColor(DiscordColor.Red)
                        .WithDescription($"Der Member {member.Mention} kann bereits Quotes hinzufügen.");
                    await ctx.RespondAsync(embed: embedBuilder.Build());
                    return;
                }

                allowedUsers.Add(member.Id);
                await ConfigHelper.SetValueAsync(ctx.Guild, "quote.permission.users", allowedUsers.ToArray(), uow);

                embedBuilder = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Green)
                    .WithDescription($"Der Member {member.Mention} kann nun Quotes hinzufügen.");
                await ctx.RespondAsync(embed: embedBuilder.Build());
            }
        }

        [Command("revokemember"), RequirePermissions(Permissions.Administrator)]
        public async Task RevokeAsync(CommandContext ctx, DiscordUser member)
        {
            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                var allowedUsers = (await ConfigHelper.GetValueAsync(ctx.Guild, "quote.permission.users", new ulong[] { }, uow)).ToList();
                DiscordEmbedBuilder embedBuilder;

                if (allowedUsers.Contains(member.Id))
                {
                    embedBuilder = new DiscordEmbedBuilder()
                        .WithColor(DiscordColor.Red)
                        .WithDescription($"Der Member {member.Mention} kann keine Quotes hinzufügen.");
                    await ctx.RespondAsync(embed: embedBuilder.Build());
                    return;
                }

                allowedUsers.Remove(member.Id);
                await ConfigHelper.SetValueAsync(ctx.Guild, "quote.permission.users", allowedUsers.ToArray(), uow);

                embedBuilder = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Green)
                    .WithDescription($"Der Member {member.Mention} kann nun keine Quotes mehr hinzufügen.");
                await ctx.RespondAsync(embed: embedBuilder.Build());
            }
        }

        [Command("unblockmember"), RequirePermissions(Permissions.Administrator)]
        public async Task UnblockAsync(CommandContext ctx, DiscordUser member)
        {
            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                var blockedUsers = (await ConfigHelper.GetValueAsync(ctx.Guild, "quote.permission.users.blocked", new ulong[] { }, uow)).ToList();
                DiscordEmbedBuilder embedBuilder;

                if (!blockedUsers.Contains(member.Id))
                {
                    embedBuilder = new DiscordEmbedBuilder()
                        .WithColor(DiscordColor.Red)
                        .WithDescription($"Der Member {member.Mention} ist nicht geblockt.");
                    await ctx.RespondAsync(embed: embedBuilder.Build());
                    return;
                }

                blockedUsers.Remove(member.Id);
                await ConfigHelper.SetValueAsync(ctx.Guild, "quote.permission.users.blocked", blockedUsers.ToArray(), uow);

                embedBuilder = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Green)
                    .WithDescription($"Der Member {member.Mention} ist nun nicht mehr geblockt.");
                await ctx.RespondAsync(embed: embedBuilder.Build());
            }
        }

        [Command("blockmember"), RequirePermissions(Permissions.Administrator)]
        public async Task BlockAsync(CommandContext ctx, DiscordMember member)
        {
            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                var blockedUsers = (await ConfigHelper.GetValueAsync(ctx.Guild, "quote.permission.users.blocked", new ulong[] { }, uow)).ToList();
                DiscordEmbedBuilder embedBuilder;

                if (blockedUsers.Contains(member.Id))
                {
                    embedBuilder = new DiscordEmbedBuilder()
                        .WithColor(DiscordColor.Red)
                        .WithDescription($"Der Member {member.Mention} ist bereits geblockt.");
                    await ctx.RespondAsync(embed: embedBuilder.Build());
                    return;
                }

                blockedUsers.Add(member.Id);
                await ConfigHelper.SetValueAsync(ctx.Guild, "quote.permission.users.blocked", blockedUsers.ToArray(), uow);

                embedBuilder = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Green)
                    .WithDescription($"Der Member {member.Mention} ist nun geblockt.");
                await ctx.RespondAsync(embed: embedBuilder.Build());
            }
        }

        #endregion Members

        #endregion Permissions

        #region Configs

        [Command("downtime"), RequirePermissions(Permissions.Administrator)]
        public async Task DowntimeAsync(CommandContext ctx, TimeSpan time = default(TimeSpan))
        {
            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                DiscordEmbedBuilder embedBuilder;

                if (time == default(TimeSpan))
                {
                    var downtime = await ConfigHelper.GetValueAsync(ctx.Guild, "quote.time.downtime", TimeSpan.FromSeconds(300), uow);
                    embedBuilder = new DiscordEmbedBuilder()
                        .WithColor(DiscordColor.Orange)
                        .WithDescription($"Quote-Downtime steht auf: {downtime.PrettyPrint()}");
                    await ctx.RespondAsync(embed: embedBuilder.Build());
                    return;
                }

                await ConfigHelper.SetValueAsync(ctx.Guild, "quote.time.downtime", time, uow);
                embedBuilder = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Green)
                    .WithDescription($"Quote-Downtime steht auf nun auf: {time.PrettyPrint()}");
                await ctx.RespondAsync(embed: embedBuilder.Build());
            }
        }

        #endregion Configs
    }
}
