using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PetPlatoon.Godfrey.Collections;
using PetPlatoon.Godfrey.Database;
using PetPlatoon.Godfrey.Database.Quotes;
using PetPlatoon.Godfrey.Extensions;

namespace PetPlatoon.Godfrey.Commands
{
    [Group("quote")]
    [Description("Provides quote commands.")]
    public class QuoteCommandModule : BaseCommandModule
    {
        private static readonly CooldownAttribute Cooldown = new CooldownAttribute(1, 120.0, CooldownBucketType.User);
        internal static LoopBackList<ulong> LoopBackList { get; set; }

        [GroupCommand]
        [Description("Returns a random quote. You're getting cooldowned for 2 minutes after using this command.")]
        public async Task ExecuteGroupAsync(CommandContext ctx)
        {
            await QuoteCommandAsync(ctx);
        }

        public async Task QuoteCommandAsync(CommandContext ctx)
        {
            var databaseContext = ctx.Services.GetService<DatabaseContext>();

            if (!await databaseContext.Quotes.AnyAsync(x => !x.IsDeleted))
            {
                await ctx.RespondAsync(embed: new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Red)
                    .WithTitle("An error occurred!")
                    .WithDescription("Couldn't find any quotes!"));

                return;
            }

            if (!await Cooldown.ExecuteCheckAsync(ctx, false))
            {
                var remainingCooldown = Cooldown.GetRemainingCooldown(ctx);

                await ctx.RespondAsync(ctx.User.Mention, embed: new DiscordEmbedBuilder()
                    .WithTitle("Slow down mate")
                    .WithColor(DiscordColor.Red)
                    .WithDescription(
                        $"You have to wait {remainingCooldown.PrettyPrint()} until you can request a new random quote!"));
                return;
            }

            if (LoopBackList == null)
            {
                var quoteCount = await databaseContext.Quotes.CountAsync();
                LoopBackList = new LoopBackList<ulong>(quoteCount - (int)MathF.Ceiling(quoteCount / 2f));
            }

            var quotes = databaseContext.Quotes
                .Where(x => !LoopBackList.Contains(x.Id))
                .Where(x => !x.IsDeleted)
                .Include(x => x.Author)
                .Include(x => x.Quoter)
                .Include(x => x.Server)
                .RandomOrder(Startup.Random);
            var quote = await quotes.FirstAsync();

            await ctx.RespondAsync($"Quote [#{quote.Id}]:", embed: new DiscordEmbedBuilder()
                .WithAuthor(quote.Author.Name)
                .WithFooter($"Quoted by {quote.Quoter.Name} on {quote.CreatedAt.PrettyPrint()}")
                .WithDescription(quote.Message)
                .WithColor(DiscordColor.Orange)
                .Build());

            LoopBackList.Add(quote.Id);
        }

        [Command("add")]
        [Description("Adds a quote to the database.")]
        public async Task AddQuoteCommandAsync(CommandContext ctx,
            [Description(
                "@Mention for the last message of the user or id of a specific message.")]
            DiscordMessage message)
        {
            var interactivity = ctx.Client.GetInteractivity();
            var configuration = ctx.Services.GetService<IConfiguration>();
            var databaseContext = ctx.Services.GetService<DatabaseContext>();

            var permissions = configuration.GetSection("permissions");
            var quotePermissions = permissions.GetSection("quote");
            var addQuotePermission = quotePermissions["add"];
            var rolesRaw = addQuotePermission.Split(',');
            var roles = rolesRaw.Select(ulong.Parse).ToArray();

            if (ctx.Member.Roles.All(x => !roles.Contains(x.Id)))
            {
                return;
            }

            var quote = await databaseContext.Quotes
                .Include(x => x.Author)
                .Include(x => x.Server)
                .SingleOrDefaultAsync(x => x.Id == message.Id);
            var isNew = false;

            if (quote == null)
            {
                var author = await message.Author.GetDatabaseUserAsync(databaseContext);
                var quoter = await ctx.User.GetDatabaseUserAsync(databaseContext);

                quote = new Quote
                {
                    Id = message.Id,
                    Message = message.Content,
                    Author = author,
                    Quoter = quoter,
                    ServerId = ctx.Guild.Id,
                    ChannelId = ctx.Channel.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsNotSafeForWork = ctx.Channel.IsNSFW
                };

                isNew = true;
            }
            else
            {
                if (!quote.IsDeleted)
                {
                    await ctx.RespondAsync(embed: new DiscordEmbedBuilder()
                        .WithTitle("Already added!")
                        .WithDescription(
                            $"This quotes got already added by {quote.Quoter.Name} on {quote.CreatedAt.PrettyPrint()}")
                        .WithColor(DiscordColor.Red));
                    return;
                }
            }

            quote.IsDeleted = false;

            var msg = await ctx.RespondAsync($"Should I add the following quote to the database? [#{quote.Id}]:",
                embed: new DiscordEmbedBuilder()
                    .WithAuthor(quote.Author.Name)
                    .WithFooter($"Quoted by {quote.Quoter.Name} on {quote.CreatedAt.PrettyPrint()}")
                    .WithDescription(quote.Message)
                    .WithColor(DiscordColor.Yellow)
                    .Build());

            var options = new[]
                {DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"), DiscordEmoji.FromName(ctx.Client, ":x:")};
            foreach (var option in options)
            {
                await msg.CreateReactionAsync(option);
            }

            var reactionContext = await interactivity.WaitForReactionAsync(reactionAddEventArgs => options.Contains(reactionAddEventArgs.Emoji), ctx.User);

            Console.WriteLine(reactionContext.Result.Emoji.ToString());

            if (reactionContext.Result.Emoji.GetDiscordName() == ":x:")
            {
                await msg.ModifyAsync($"I'm not adding that quote!  [#{quote.Id}]:", new DiscordEmbedBuilder()
                    .WithAuthor(quote.Author.Name)
                    .WithFooter($"Quoted by {quote.Quoter.Name} on {quote.CreatedAt.PrettyPrint()}")
                    .WithDescription(quote.Message)
                    .WithColor(DiscordColor.Red)
                    .Build());

                return;
            }

            if (isNew)
            {
                await databaseContext.Quotes.AddAsync(quote);
            }

            await databaseContext.SaveChangesAsync();

            var quoteCount = await databaseContext.Quotes.CountAsync();
            LoopBackList = new LoopBackList<ulong>(quoteCount - (int)MathF.Ceiling(quoteCount / 2f));

            await msg.ModifyAsync($"Added Quote [#{quote.Id}]:", new DiscordEmbedBuilder()
                .WithAuthor(quote.Author.Name)
                .WithFooter($"Quoted by {quote.Quoter.Name} on {quote.CreatedAt.PrettyPrint()}")
                .WithDescription(quote.Message)
                .WithColor(DiscordColor.Green)
                .Build());
        }
    }
}
