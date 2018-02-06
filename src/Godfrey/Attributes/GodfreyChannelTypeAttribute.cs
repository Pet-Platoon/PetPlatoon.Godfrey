using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Godfrey.Helpers;
using Godfrey.Models.Context;

namespace Godfrey.Attributes
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
            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                var channel = await ConfigHelper.GetValueAsync(ctx.Guild, _configKey, 0ul, uow);

                return channel == ctx.Channel.Id;
            }
        }
    }
}
