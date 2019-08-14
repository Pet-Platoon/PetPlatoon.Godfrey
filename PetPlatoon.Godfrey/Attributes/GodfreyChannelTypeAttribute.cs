using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.DependencyInjection;
using PetPlatoon.Godfrey.Database;
using PetPlatoon.Godfrey.Helpers;

namespace PetPlatoon.Godfrey.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class GodfreyChannelTypeAttribute : CheckBaseAttribute
    {
        private readonly string _configKey;

        public GodfreyChannelTypeAttribute(string configKey)
        {
            _configKey = configKey;
        }

        public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            var databaseContext = ctx.Services.GetService<DatabaseContext>();
            var channel = await ConfigHelper.GetValueAsync(ctx.Guild, _configKey, databaseContext, 0ul);

            return channel == ctx.Channel.Id;
        }
    }
}
