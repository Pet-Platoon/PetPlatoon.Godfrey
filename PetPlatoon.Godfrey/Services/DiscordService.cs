using System.Threading.Tasks;
using DSharpPlus;
using Microsoft.Extensions.Configuration;
using PetPlatoon.Godfrey.Services.Common;

namespace PetPlatoon.Godfrey.Services
{
    public class DiscordService : IService
    {
        #region Constructors

        public DiscordService(IConfiguration configuration)
        {
            Configuration = configuration;

            Client = new DiscordClient(new DiscordConfiguration
            {
                    Token = Configuration.GetValue<string>("DiscordConfiguration:Token"),
                    TokenType = TokenType.Bot,
                    LogLevel = LogLevel.Debug,
                    UseInternalLogHandler = true,
                    AutoReconnect = true,
                    GatewayCompressionLevel = GatewayCompressionLevel.Stream,
                    ReconnectIndefinitely = true
            });
        }

        #endregion Constructors

        #region Properties

        private IConfiguration Configuration { get; }
        
        internal DiscordClient Client { get; }

        #endregion Properties

        #region Methods

        public Task Start()
        {
            return Client.ConnectAsync();
        }

        #endregion Methods
    }
}
