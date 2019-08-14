using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;

namespace PetPlatoon.Godfrey.Commands.Converters
{
    public class DiscordMessageConverter : IArgumentConverter<DiscordMessage>
    {
        public async Task<Optional<DiscordMessage>> ConvertAsync(string value, CommandContext ctx)
        {
            if (ctx.Message.MentionedUsers.Count == 1)
            {
                return await ConvertUserMention(ctx.Message.MentionedUsers[0], ctx);
            }

            if (value.StartsWith("^") && int.TryParse(value.Substring(1), out var number))
            {
                return await ConvertUpperMessage(number, ctx);
            }

            if (!ulong.TryParse(value, out var id))
            {
                return new Optional<DiscordMessage>();
            }

            var message = await ctx.Channel.GetMessageAsync(id);

            if (message != null)
            {
                return new Optional<DiscordMessage>(message);
            }

            var user = await ctx.Guild.GetMemberAsync(id);
            return await ConvertUserMention(user, ctx);

        }

        private async Task<Optional<DiscordMessage>> ConvertUserMention(DiscordUser value, CommandContext ctx)
        {
            var messagesBeforeAsync = (await ctx.Channel.GetMessagesBeforeAsync(ctx.Message.Id)).OrderByDescending(x => x.Id);
            var messageByMention = messagesBeforeAsync.FirstOrDefault(x => x.Author.Id == value.Id);

            return messageByMention == null
                    ? new Optional<DiscordMessage>()
                    : new Optional<DiscordMessage>(messageByMention);
        }

        private async Task<Optional<DiscordMessage>> ConvertUpperMessage(int number, CommandContext ctx)
        {
            number = Math.Max(0, Math.Min(number, 100));

            var messages = (await ctx.Channel.GetMessagesBeforeAsync(ctx.Message.Id, number))
                           .OrderByDescending(x => x.Id).ToArray();
            var discordMessage = messages[number - 1];

            return discordMessage == null
                    ? new Optional<DiscordMessage>()
                    : new Optional<DiscordMessage>(discordMessage);
        }
    }
}
