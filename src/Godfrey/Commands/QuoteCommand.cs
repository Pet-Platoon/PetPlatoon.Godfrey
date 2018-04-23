using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Godfrey.Attributes;
using Godfrey.Collections;
using Godfrey.Exceptions;
using Godfrey.Extensions;
using Godfrey.Helpers;
using Godfrey.Models.Context;
using Godfrey.Models.Quotes;
using Microsoft.EntityFrameworkCore;
using CooldownBucketType = Godfrey.Attributes.CooldownBucketType;

namespace Godfrey.Commands
{
    [Group("quote")]
    [Aliases("kwot", "<:leroy:230337206751854592>", "<@240871410954665985>")]
    public class QuoteCommand : BaseCommandModule
    {
        public static DateTime LastRandomQuote;

        private static readonly GodfreyCooldownAttribute Cooldown =
                new GodfreyCooldownAttribute(1, Constants.Quotes.Times.Downtime, CooldownBucketType.User);

        [GroupCommand]
        public async Task ExecuteGroupAsync(CommandContext ctx)
        {
            if (!await Cooldown.ExecuteCheckAsync(ctx, false))
            {
                return;
            }

            await RandomQuoteAsync(ctx);
        }

        #region RandomQuote

        public async Task RandomQuoteAsync(CommandContext ctx)
        {
            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                var foreignsAllowed =
                        await ConfigHelper.GetValueAsync(ctx.Guild, Constants.Quotes.Foreigns, false, uow);
                if (!Butler.LastIssuedQuotes.ContainsKey(ctx.Guild.Id))
                {
                    var quoteCount = await uow.Quotes.CountAsync(x => x.ServerId == ctx.Guild.Id || foreignsAllowed);
                    var loopbackLength =
                            await ConfigHelper.GetValueAsync(ctx.Guild, Constants.Quotes.Loopbacks, 10, uow);
                    Butler.LastIssuedQuotes.Add(ctx.Guild.Id,
                                                new LoopBackList<ulong>(Math.Min(loopbackLength, quoteCount - 1)));
                }

                if (!await uow.Quotes.AnyAsync(x => (x.Server.Id == ctx.Guild.Id || foreignsAllowed) &&
                                                    !Butler.LastIssuedQuotes[ctx.Guild.Id].Contains(x.Id)))
                {
                    throw new MissingQuotesException("Es wurden keine Quotes gefunden");
                }

                var quotes = uow.Quotes.Where(x => !Butler.LastIssuedQuotes[ctx.Guild.Id].Contains(x.Id))
                                .Where(x => x.ServerId == ctx.Guild.Id || foreignsAllowed);

                try
                {
                    var quote = await quotes.OrderBy(x => Butler.RandomGenerator.Next())
                                            .Include(x => x.Author)
                                            .Include(x => x.Quoter)
                                            .FirstOrDefaultAsync();

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

                    Butler.LastIssuedQuotes[ctx.Guild.Id].Add(quote.Id);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }

        #endregion RandomQuote

        #region AddQuote

        [Command("add")]
        public async Task AddQuoteAsync(CommandContext ctx, ulong id, bool isNsfw = true)
        {
            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                var allowedUsers =
                        await ConfigHelper.GetValueAsync(ctx.Guild, Constants.Quotes.Permissions.Users, new ulong[] { },
                                                         uow);
                var allowedRoles =
                        await ConfigHelper.GetValueAsync(ctx.Guild, Constants.Quotes.Permissions.Roles, new ulong[] { },
                                                         uow);

                var quoter = await ctx.User.GetUserAsync(uow);

                if (allowedRoles.Any() && allowedUsers.Any())
                {
                    if (!ctx.Member.Roles.Any(x => allowedRoles.Contains(x.Id)) &&
                        allowedUsers.All(x => x != ctx.User.Id))
                    {
                        throw new UsageBlockedException("Du bist dazu nicht berechtigt.");
                    }
                }

                var message = await ctx.Channel.GetMessageAsync(id);

                if (message == null)
                {
                    throw new Exception("Es wurde keine Nachricht mit dieser Id in diesem Channel gefunden.");
                }

                if (message.Author.Id == ctx.Member.Id &&
                    !ctx.Member.PermissionsIn(ctx.Channel).HasFlag(Permissions.Administrator))
                {
                    throw new Exception("Du darfst dich nicht selber quoten!");
                }

                if (await uow.Quotes.AnyAsync(x => x.Id == id))
                {
                    throw new Exception("Diese Nachricht wurde bereits gequoted.");
                }

                var msg = await ctx.RespondAsync("Füge Quote hinzu...");

                var author = await message.Author.GetUserAsync(uow);

                var quote = new Quote
                {
                        Id = message.Id,
                        Message = message.Content,
                        AuthorId = author.Id,
                        QuoterId = quoter.Id,
                        ServerId = ctx.Guild.Id,
                        ChannelId = ctx.Channel.Id,
                        CreatedAt = message.CreationTimestamp.UtcDateTime,
                        IsNotSafeForWork = isNsfw
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

        #region SetNsfw

        [Command("setnsfw")]
        public async Task SetNsfwCommandAsync(CommandContext ctx, ulong id, bool isNsfw = false)
        {
            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                var allowedUsers =
                        await ConfigHelper.GetValueAsync(ctx.Guild, Constants.Quotes.Permissions.Users, new ulong[] { },
                                                         uow);
                var allowedRoles =
                        await ConfigHelper.GetValueAsync(ctx.Guild, Constants.Quotes.Permissions.Roles, new ulong[] { },
                                                         uow);

                if (allowedRoles.Any() && allowedUsers.Any())
                {
                    if (!ctx.Member.Roles.Any(x => allowedRoles.Contains(x.Id)) &&
                        allowedUsers.All(x => x != ctx.User.Id))
                    {
                        throw new UsageBlockedException("Du bist dazu nicht berechtigt.");
                    }
                }

                var quote = await uow.Quotes.FirstOrDefaultAsync(x => x.Id == id && x.ServerId == ctx.Guild.Id);
                DiscordEmbedBuilder embed;

                if (quote == null)
                {
                    embed = Constants.Embeds.Presets.Error(description: "Es wurde kein Quote mit der Id gefunden");
                    await ctx.RespondAsync(embed: embed);
                    return;
                }

                quote.IsNotSafeForWork = isNsfw;
                quote.UpdatedAt = DateTime.UtcNow;
                embed =
                        Constants.Embeds.Presets.Success(description:
                                                         $"Der Quote wurde nun als NSFW {(isNsfw ? "ge" : "de")}flagged.");
                await ctx.RespondAsync(embed: embed);
                await uow.SaveChangesAsync();
            }
        }

        #endregion SetNsfw

        #region DeleteQuote

        [Command("delete")]
        public async Task DeleteQuoteAsync(CommandContext ctx, ulong id)
        {
            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                var quotes = uow.Quotes.Where(x => x.ServerId == ctx.Guild.Id);
                var quote = await quotes.FirstOrDefaultAsync(x => x.Id == id);

                if (quote == null)
                {
                    throw new
                            KeyNotFoundException($"Der Quote mit der Id \"{id}\" wurde nicht gefunden oder gehört nicht zu diesem Server.");
                }

                uow.Quotes.Remove(quote);

                await uow.SaveChangesAsync();

                var embed = new DiscordEmbedBuilder()
                            .WithAuthor(quote.Author.Name)
                            .WithFooter($"Zitiert von {quote.Quoter.Name} | Erstellt: {quote.CreatedAt.PrettyPrint()}")
                            .WithDescription(quote.Message)
                            .WithColor(DiscordColor.Red)
                            .Build();

                await
                        ctx.RespondAsync($"Quote entfernt [#{quote.Id}; Message-Id: {quote.Id}; Channel-Id: {quote.ChannelId}]:",
                                         embed: embed);
            }
        }

        #endregion DeleteQuote

        #region Stats

        [Group("stats")]
        public class QuoteStatsCommand : BaseCommandModule
        {
            [GroupCommand]
            public async Task StatsCommandAsync(CommandContext ctx)
            {
                using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
                {
                    var topQuoter =
                            await uow.Users.OrderByDescending(x => x.QuotedMessages.Count).FirstOrDefaultAsync();
                    var topQuoted =
                            await uow.Users.OrderByDescending(x => x.AuthoredQuotes.Count).FirstOrDefaultAsync();

                    var embed = new DiscordEmbedBuilder()
                                .WithTitle("Quotestats")
                                .WithTimestamp(DateTime.UtcNow)
                                .WithColor(Constants.Embeds.Colors.Output)
                                .AddField("Anzahl", $"{await uow.Quotes.CountAsync()}", true)
                                .AddField("Anzahl (Server)",
                                          $"{await uow.Quotes.CountAsync(x => x.ServerId == ctx.Guild.Id)}", true);

                    if (topQuoter != null)
                    {
                        var count = topQuoter.QuotedMessages.Count;
                        embed.AddField("Top Quoter", $"{topQuoter.Name} ({count})", true);
                    }

                    if (topQuoted != null)
                    {
                        var count = topQuoted.AuthoredQuotes.Count;
                        embed.AddField("Top Quoted", $"{topQuoted.Name} ({count})", true);
                    }

                    await ctx.RespondAsync(embed: embed);
                }
            }
        }

        #endregion Stats

        #region Permissions

        #region Roles

        [Command("grant")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task GrantCommandAsync(CommandContext ctx, DiscordRole role)
        {
            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                var allowedRoles =
                        (await ConfigHelper.GetValueAsync(ctx.Guild, Constants.Quotes.Permissions.Roles,
                                                          new ulong[] { }, uow)).ToList();
                DiscordEmbedBuilder embed;

                if (allowedRoles.Contains(role.Id))
                {
                    embed =
                            Constants.Embeds.Presets.Output(description:
                                                            $"Die Rolle {role.Mention} kann bereits Quotes hinzufügen.");

                    await ctx.RespondAsync(embed: embed);
                    return;
                }

                allowedRoles.Add(role.Id);
                await ConfigHelper.SetValueAsync(ctx.Guild, Constants.Quotes.Permissions.Roles, allowedRoles.ToArray(),
                                                 uow);

                embed =
                        Constants.Embeds.Presets.Success(description:
                                                         $"Die Rolle {role.Mention} kann nun Quotes hinzufügen.");
                await ctx.RespondAsync(embed: embed);
            }
        }

        [Command("revoke")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task RevokeCommandAsync(CommandContext ctx, DiscordRole role)
        {
            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                var allowedRoles =
                        (await ConfigHelper.GetValueAsync(ctx.Guild, Constants.Quotes.Permissions.Roles,
                                                          new ulong[] { }, uow)).ToList();
                DiscordEmbedBuilder embed;

                if (!allowedRoles.Contains(role.Id))
                {
                    embed =
                            Constants.Embeds.Presets.Output(description:
                                                            $"Die Rolle {role.Mention} kann keine Quotes hinzufügen.");
                    await ctx.RespondAsync(embed: embed);
                    return;
                }

                allowedRoles.Remove(role.Id);
                await ConfigHelper.SetValueAsync(ctx.Guild, Constants.Quotes.Permissions.Roles, allowedRoles.ToArray(),
                                                 uow);

                embed =
                        Constants.Embeds.Presets.Success(description:
                                                         $"Die Rolle {role.Mention} kann nun keine Quotes mehr hinzufügen.");
                await ctx.RespondAsync(embed: embed);
            }
        }

        #endregion Roles

        #region Members

        [Command("grant")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task GrantCommandAsync(CommandContext ctx, DiscordUser member)
        {
            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                var allowedUsers =
                        (await ConfigHelper.GetValueAsync(ctx.Guild, Constants.Quotes.Permissions.Users,
                                                          new ulong[] { }, uow)).ToList();
                DiscordEmbedBuilder embed;

                if (allowedUsers.Contains(member.Id))
                {
                    embed =
                            Constants.Embeds.Presets.Output(description:
                                                            $"Der Member {member.Mention} kann bereits Quotes hinzufügen.");
                    await ctx.RespondAsync(embed: embed);
                    return;
                }

                allowedUsers.Add(member.Id);
                await ConfigHelper.SetValueAsync(ctx.Guild, Constants.Quotes.Permissions.Users, allowedUsers.ToArray(),
                                                 uow);

                embed =
                        Constants.Embeds.Presets.Success(description:
                                                         $"Der Member {member.Mention} kann nun Quotes hinzufügen.");
                await ctx.RespondAsync(embed: embed);
            }
        }

        [Command("revoke")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task RevokeCommandAsync(CommandContext ctx, DiscordUser member)
        {
            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                var allowedUsers =
                        (await ConfigHelper.GetValueAsync(ctx.Guild, Constants.Quotes.Permissions.Users,
                                                          new ulong[] { }, uow)).ToList();
                DiscordEmbedBuilder embed;

                if (!allowedUsers.Contains(member.Id))
                {
                    embed =
                            Constants.Embeds.Presets.Output(description:
                                                            $"Der Member {member.Mention} kann keine Quotes hinzufügen.");
                    await ctx.RespondAsync(embed: embed);
                    return;
                }

                allowedUsers.Remove(member.Id);
                await ConfigHelper.SetValueAsync(ctx.Guild, Constants.Quotes.Permissions.Users, allowedUsers.ToArray(),
                                                 uow);

                embed =
                        Constants.Embeds.Presets.Success(description:
                                                         $"Der Member {member.Mention} kann nun keine Quotes mehr hinzufügen.");
                await ctx.RespondAsync(embed: embed);
            }
        }

        #endregion Members

        #endregion Permissions

        #region Configs

        [Command("loopback")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task LoopbackAsync(CommandContext ctx, int loopbacks = 0)
        {
            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                DiscordEmbedBuilder embedBuilder;

                if (loopbacks == 0)
                {
                    loopbacks = await ConfigHelper.GetValueAsync(ctx.Guild, Constants.Quotes.Loopbacks, 10, uow);
                    embedBuilder = new DiscordEmbedBuilder()
                                   .WithColor(DiscordColor.Orange)
                                   .WithDescription($"Quote-Loopback steht auf: {loopbacks}. Es wird also jedes Quote für {loopbacks} zufällig ausgegebene Quotes ignoriert.");
                    await ctx.RespondAsync(embed: embedBuilder.Build());
                    return;
                }

                if (loopbacks == -1)
                {
                    loopbacks = 0;
                }

                await ConfigHelper.SetValueAsync(ctx.Guild, Constants.Quotes.Loopbacks, loopbacks, uow);
                embedBuilder = new DiscordEmbedBuilder()
                               .WithColor(DiscordColor.Green)
                               .WithDescription($"Quote-Loopback steht nun auf: {loopbacks}. Es wird also jedes Quote für {loopbacks} zufällig ausgegebene Quotes ignoriert. Durch Änderung der Loopbacklänge wird die Loopbackliste zurückgesetzt.");
                await ctx.RespondAsync(embed: embedBuilder.Build());

                Butler.LastIssuedQuotes[ctx.Guild.Id] = new LoopBackList<ulong>((ulong)loopbacks);
            }
        }

        [Command("foreign")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task ForeignAsync(CommandContext ctx, string allowed = "")
        {
            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                DiscordEmbedBuilder embedBuilder;

                var isAllowed = false;

                if (allowed == "")
                {
                    isAllowed = await ConfigHelper.GetValueAsync(ctx.Guild, Constants.Quotes.Foreigns, false, uow);
                    embedBuilder = new DiscordEmbedBuilder()
                                   .WithColor(DiscordColor.Orange)
                                   .WithDescription($"Quote-Foreign steht auf: {isAllowed}. Es werden Quotes von anderen Servern {(isAllowed ? "" : "nicht ")}angezeigt.");
                    await ctx.RespondAsync(embed: embedBuilder.Build());
                    return;
                }

                if (allowed.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                {
                    isAllowed = true;
                }
                else if (allowed.Equals("false", StringComparison.InvariantCultureIgnoreCase))
                {
                }
                else
                {
                    throw new ArgumentException("allowed must be either true, false or nothing", nameof(allowed));
                }

                await ConfigHelper.SetValueAsync(ctx.Guild, Constants.Quotes.Foreigns, isAllowed, uow);
                embedBuilder = new DiscordEmbedBuilder()
                               .WithColor(DiscordColor.Green)
                               .WithDescription($"Quote-Foreign steht nun auf: {isAllowed}. Es werden nun Quotes von anderen Servern {(isAllowed ? "" : "nicht ")}angezeigt.");
                await ctx.RespondAsync(embed: embedBuilder.Build());
            }
        }

        [Command("nsfw")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task NsfwCommandAsync(CommandContext ctx, string allowed = "")
        {
            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                DiscordEmbedBuilder embed;

                var isAllowed = false;

                if (allowed == "")
                {
                    isAllowed = await ConfigHelper.GetValueAsync(ctx.Guild, Constants.Quotes.HideNotSafeForWork, true,
                                                                 uow);
                    embed =
                            Constants.Embeds.Presets.Output(description:
                                                            $"Quote-NSFW steht auf: {isAllowed}. Es werden {(isAllowed ? "" : "keine ")}NSFW-Quotes angezeigt.");
                    await ctx.RespondAsync(embed: embed);
                    return;
                }

                if (allowed.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                {
                    isAllowed = true;
                }
                else if (allowed.Equals("false", StringComparison.InvariantCultureIgnoreCase))
                {
                }
                else
                {
                    throw new ArgumentException("allowed must be either true, false or nothing", nameof(allowed));
                }

                await ConfigHelper.SetValueAsync(ctx.Guild, Constants.Quotes.HideNotSafeForWork, isAllowed, uow);
                embed =
                        Constants.Embeds.Presets.Success(description:
                                                         $"Quote-NSFW steht nun auf: {isAllowed}. Es werden nun {(isAllowed ? "" : "keine ")}NSFW-Quotes angezeigt.");
                await ctx.RespondAsync(embed: embed);
            }
        }

        #endregion Configs
    }
}
