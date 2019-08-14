using System.Threading.Tasks;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using PetPlatoon.Godfrey.Database;
using PetPlatoon.Godfrey.Database.Configs;
using PetPlatoon.Godfrey.Database.Users;
using PetPlatoon.Godfrey.Helpers;

namespace PetPlatoon.Godfrey.Extensions
{
    public static class DiscordObjectExtensions
    {
        public static async Task<User> GetUserAsync(this DiscordUser member, DatabaseContext context)
        {
            if (member == null)
            {
                return null;
            }

            var result = await context.Users.SingleOrDefaultAsync(x => x.Id == member.Id);

            if (result == null)
            {
                result = new User
                {
                        Id = member.Id,
                        Name = member.Username
                };

                var entity = await context.Users.AddAsync(result);
                await context.SaveChangesAsync();

                result = entity.Entity;
            }

            return result;
        }

        public static Task<User> GetUserAsync(this DiscordMember member, DatabaseContext context)
        {
            return GetUserAsync((DiscordUser)member, context);
        }

        public static Task<Config> GetConfigAsync(this DiscordGuild guild, string key, DatabaseContext uow = null)
        {
            return ConfigHelper.GetConfigAsync(guild, key, uow);
        }

        public static Task AddConfigAsync(this Config config, DatabaseContext uow = null)
        {
            return ConfigHelper.AddConfigAsync(config, uow);
        }

        public static Task<T> GetConfigValueAsync<T>(this DiscordGuild guild, string key, DatabaseContext uow, T defaultValue = default(T))
        {
            return ConfigHelper.GetValueAsync(guild, key, uow, defaultValue);
        }

        public static Task<Config> SetConfigValueAsync<T>(this DiscordGuild guild, string key, T value,
                                                          DatabaseContext uow = null)
        {
            return ConfigHelper.SetValueAsync(guild, key, value, uow);
        }
    }
}
