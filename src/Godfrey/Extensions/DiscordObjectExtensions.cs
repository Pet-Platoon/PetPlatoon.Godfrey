using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Godfrey.Models.Context;
using Godfrey.Models.Servers;
using Godfrey.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace Godfrey.Extensions
{
    public static class DiscordObjectExtensions
    {
        public static async Task MapToDatabaseAsync(this CommandContext ctx, DatabaseContext context)
        {
            await ctx.User.GetUserAsync(context);
            await ctx.Guild.GetServerAsync(context);
            foreach (var messageMentionedUser in ctx.Message.MentionedUsers)
            {
                await messageMentionedUser.GetUserAsync(context);
            }
        }

        public static async Task<User> GetUserAsync(this DiscordUser member, DatabaseContext context)
        {
            var result = await context.Users.FirstOrDefaultAsync(x => x.Id == member.Id);

            if (result == null)
            {
                result = new User
                {
                    Id = member.Id,
                    Name = member.Username,
                    Coins = 100
                };

                await context.Users.AddAsync(result);

                await context.SaveChangesAsync();
            }

            return result;
        }

        public static async Task<User> GetUserAsync(this DiscordMember member, DatabaseContext context)
        {
            var result = await context.Users.FirstOrDefaultAsync(x => x.Id == member.Id);

            if (result == null)
            {
                result = new User
                {
                    Id = member.Id,
                    Name = member.Username,
                    Coins = 100
                };

                await context.Users.AddAsync(result);

                await context.SaveChangesAsync();
            }

            return result;
        }

        public static async Task<Server> GetServerAsync(this DiscordGuild guild, DatabaseContext context)
        {
            var result = await context.Servers.FirstOrDefaultAsync(x => x.Id == guild.Id);

            if (result == null)
            {
                result = new Server
                {
                    Id = guild.Id,
                    Name = guild.Name,
                    Owner = await guild.Owner.GetUserAsync(context)
                };

                await context.Servers.AddAsync(result);

                await context.SaveChangesAsync();

                foreach (var discordMember in await guild.GetAllMembersAsync())
                {
                    var member = await discordMember.GetUserAsync(context);

                    await result.GetServerMemberAsync(member, context);
                }

                await context.SaveChangesAsync();
            }

            return result;
        }

        public static async Task<ServerMember> GetServerMemberAsync(this Server server, User user, DatabaseContext context)
        {
            var result = await context.ServerMembers.FirstOrDefaultAsync(x => x.ServerId == server.Id && x.UserId == user.Id);

            if (result == null)
            {
                result = new ServerMember
                {
                    ServerId = server.Id,
                    UserId = user.Id
                };

                await context.ServerMembers.AddAsync(result);

                await context.SaveChangesAsync();
            }

            return result;
        }
    }
}
