using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.VoiceNext;
using Newtonsoft.Json;

namespace Godfrey
{
    public partial class Butler
    {
        public static ButlerConfig ButlerConfig => JsonConvert.DeserializeObject<ButlerConfig>(File.ReadAllText("config.json", new UTF8Encoding(false)));

        public static Random RandomGenerator { get; private set; }
        private DiscordClient Client { get; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        private VoiceNextExtension VoiceNextClient { get; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        private InteractivityExtension InteractivityModule { get; }
        private CommandsNextExtension CommandsNextModule { get; }

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
                GatewayCompressionLevel = GatewayCompressionLevel.Stream,
                MessageCacheSize = 50,
                AutomaticGuildSync = !ButlerConfig.UseUserToken
            };
            Client = new DiscordClient(dcfg);

            VoiceNextClient = Client.UseVoiceNext();

            InteractivityModule = Client.UseInteractivity(new InteractivityConfiguration());

            var dependencyCollectionBuilder = new DependencyCollectionBuilder()
                .AddInstance(this);

            var dependencyCollection = dependencyCollectionBuilder.Build();

            var cncfg = new CommandsNextConfiguration
            {
                StringPrefix = ButlerConfig.CommandPrefix,
                EnableDms = true,
                EnableMentionPrefix = true,
                CaseSensitive = true,
                Selfbot = ButlerConfig.UseUserToken,
                IgnoreExtraArguments = false,
                Dependencies = dependencyCollection
            };
            CommandsNextModule = Client.UseCommandsNext(cncfg);
            CommandsNextModule.RegisterCommands(GetType().Assembly);
            CommandsNextModule.CommandErrored += OnCommandErrored;
        }

        public async Task RunAsync()
        {
            await Client.ConnectAsync();
            await Task.Delay(-1);
        }
    }
}
