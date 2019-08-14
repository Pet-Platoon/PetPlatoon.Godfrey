using System;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PetPlatoon.Godfrey.Database;
using PetPlatoon.Godfrey.Database.Configs;

namespace PetPlatoon.Godfrey.Helpers
{
    public static class ConfigHelper
    {
        public static async Task<Config> GetConfigAsync(DiscordGuild guild, string key, DatabaseContext uow)
        {
            if (uow == null)
            {
                throw new ArgumentNullException(nameof(uow));
            }

            return await uow.Configs.FirstOrDefaultAsync(x => x.ServerId == guild.Id && x.Key == key);
        }

        public static async Task AddConfigAsync(Config config, DatabaseContext uow)
        {
            if (uow == null)
            {
                throw new ArgumentNullException(nameof(uow));
            }

            await uow.Configs.AddAsync(config);
            await uow.SaveChangesAsync();
        }

        public static async Task<T> GetValueAsync<T>(DiscordGuild guild, string key, DatabaseContext uow, T defaultValue = default(T))
        {
            var cfg = await GetConfigAsync(guild, key, uow);
            return cfg == null ? defaultValue : JsonConvert.DeserializeObject<T>(cfg.Value);
        }

        public static async Task<Config> SetValueAsync<T>(DiscordGuild guild, string key, T value, DatabaseContext uow)
        {
            var cfg = await GetConfigAsync(guild, key, uow);

            if (uow == null)
            {
                throw new ArgumentNullException(nameof(uow));
            }

            if (cfg != null)
            {
                cfg.Value = JsonConvert.SerializeObject(value);
                await uow.SaveChangesAsync();
                return cfg;
            }

            cfg = new Config
            {
                    ServerId = guild.Id,
                    Key = key,
                    Value = JsonConvert.SerializeObject(value)
            };

            await AddConfigAsync(cfg, uow);

            return cfg;
        }
    }
}
