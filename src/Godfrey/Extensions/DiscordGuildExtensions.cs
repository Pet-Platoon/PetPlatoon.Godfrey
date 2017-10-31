using System.Threading.Tasks;
using DSharpPlus.Entities;
using Godfrey.Helpers;
using Godfrey.Models.Configs;
using Godfrey.Models.Context;

namespace Godfrey.Extensions
{
    public static class DiscordGuildExtensions
    {
        public static Task<Config> GetConfigAsync(this DiscordGuild guild, string key, DatabaseContext uow = null)
        {
            return ConfigHelper.GetConfigAsync(guild, key, uow);
        }

        public static Task AddConfigAsync(this Config config, DatabaseContext uow = null)
        {
            return ConfigHelper.AddConfigAsync(config, uow);
        }

        public static Task<T> GetConfigValueAsync<T>(this DiscordGuild guild, string key, T defaultValue = default(T), DatabaseContext uow = null)
        {
            return ConfigHelper.GetValueAsync(guild, key, defaultValue, uow);
        }

        public static Task<Config> SetConfigValueAsync<T>(this DiscordGuild guild, string key, T value,
                                                    DatabaseContext uow = null)
        {
            return ConfigHelper.SetValueAsync(guild, key, value, uow);
        }
    }
}
