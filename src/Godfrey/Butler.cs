using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.VoiceNext;
using Newtonsoft.Json;

namespace Godfrey
{
    public class Butler
    {
        public static ButlerConfig ButlerConfig => JsonConvert.DeserializeObject<ButlerConfig>(File.ReadAllText("config.json", new UTF8Encoding(false)));
        public static Random RandomGenerator { get; private set; }
        private DiscordClient Client { get; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        private VoiceNextClient VoiceNextClient { get; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        private InteractivityModule InteractivityModule { get; }
        private CommandsNextModule CommandsNextModule { get; }

        public Butler(int shardId)
        {
            RandomGenerator = new Random();

            var dcfg = new DiscordConfiguration
            {
                AutoReconnect = true,
                LargeThreshold = 250,
                LogLevel = LogLevel.Debug,
                Token = ButlerConfig.Token,
                TokenType = ButlerConfig.UseUserToken ? TokenType.User : TokenType.Bot,
                UseInternalLogHandler = true,
                ShardId = shardId,
                ShardCount = ButlerConfig.ShardCount,
                EnableCompression = true,
                MessageCacheSize = 50,
                AutomaticGuildSync = !ButlerConfig.UseUserToken
            };
            Client = new DiscordClient(dcfg);

            VoiceNextClient = Client.UseVoiceNext();

            InteractivityModule = Client.UseInteractivity();

            var dependencyCollectionBuilder = new DependencyCollectionBuilder()
                .AddInstance(this);

            var dependencyCollection = dependencyCollectionBuilder.Build();

            var cncfg = new CommandsNextConfiguration
            {
                StringPrefix = ButlerConfig.CommandPrefix,
                EnableDms = true,
                EnableMentionPrefix = true,
                CaseSensitive = true,
                SelfBot = ButlerConfig.UseUserToken,
                IgnoreExtraArguments = false,
                Dependencies = dependencyCollection
            };
            CommandsNextModule = Client.UseCommandsNext(cncfg);
            CommandsNextModule.RegisterCommands(GetType().Assembly);
            CommandsNextModule.CommandErrored += OnCommandErrored;
        }

        private async Task OnCommandErrored(CommandErrorEventArgs e)
        {
            e.Context.Client.DebugLogger.LogMessage(LogLevel.Error, "Butler", $"{e.Context.User.Username} tried executing '{e.Command?.QualifiedName ?? "<unknown command>"}' but it errored: {e.Exception.GetType()}: {e.Exception.Message ?? "<no message>"}", DateTime.Now);

            var embedBuilder = new DiscordEmbedBuilder()
                .WithTitle("Ein Fehler ist aufgetreten!")
                .WithDescription($"Ein Fehler im Command `{e.Command?.QualifiedName ?? "<unknown command>"}` ist aufgetreten:{Environment.NewLine}```{e.Exception.Message}```")
                .WithColor(DiscordColor.Red);

            await e.Context.RespondAsync(embed: embedBuilder.Build());
        }

        public async Task RunAsync()
        {
            await Client.ConnectAsync();
            await Task.Delay(-1);
        }
    }
}
