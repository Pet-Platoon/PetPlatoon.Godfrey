using System.Threading.Tasks;
using DSharpPlus.Entities;
using Godfrey.Models.Configs;
using Godfrey.Models.Context;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Godfrey.Helpers
{
    public static class ConfigHelper
    {
        public static async Task<Config> GetConfigAsync(DiscordGuild guild, string key, DatabaseContext uow = null)
        {
            if (uow == null)
            {
                uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString);
            }

            return await uow.Configs.FirstOrDefaultAsync(x => x.GuildId == guild.Id && x.Key == key);
        }

        public static async Task AddConfigAsync(Config config, DatabaseContext uow = null)
        {
            if (uow == null)
            {
                uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString);
            }

            await uow.Configs.AddAsync(config);
            await uow.SaveChangesAsync();
        }

        public static async Task<T> GetValueAsync<T>(DiscordGuild guild, string key, T defaultValue, DatabaseContext uow = null)
        {
            var cfg = await GetConfigAsync(guild, key, uow);
            return cfg == null ? defaultValue : JsonConvert.DeserializeObject<T>(cfg.Value);
        }

        public static async Task<Config> SetValueAsync<T>(DiscordGuild guild, string key, T value, DatabaseContext uow = null)
        {
            var cfg = await GetConfigAsync(guild, key, uow);
            if (cfg != null)
            {
                cfg.Value = JsonConvert.SerializeObject(value);
                await uow.SaveChangesAsync();
                return cfg;
            }

            cfg = new Config
            {
                GuildId = guild.Id,
                Key = key,
                Value = JsonConvert.SerializeObject(value)
            };

            await AddConfigAsync(cfg);

            return cfg;
        }
    }
}
