using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PetPlatoon.Godfrey.Commands.Converters;
using PetPlatoon.Godfrey.Database;
using PetPlatoon.Godfrey.Services.Common;

namespace PetPlatoon.Godfrey.Services
{
    public class DiscordCommandService : IService
    {
        #region Constructors

        public DiscordCommandService(IConfiguration configuration, DiscordService discordService,
                                     DatabaseContext databaseContext)
        {
            Configuration = configuration;
            DiscordService = discordService;
            DatabaseContext = databaseContext;

            var serviceCollection = new ServiceCollection()
                                    .AddSingleton(DatabaseContext)
                                    .AddSingleton(Configuration);
            var serviceProvider = serviceCollection.BuildServiceProvider(true);

            Commands = DiscordService.Client.UseCommandsNext(new CommandsNextConfiguration
            {
                CaseSensitive = false,
                EnableDefaultHelp = true,
                EnableDms = true,
                EnableMentionPrefix = true,
                StringPrefixes = new []{"?"},
                Services = serviceProvider
            });
            
            Commands.RegisterConverter(new DiscordMessageConverter());

            Commands.RegisterCommands(GetType().Assembly);

            DiscordService.Client.UseInteractivity(new InteractivityConfiguration());

            DiscordService.Client.MessageReactionAdded += OnMessageReactionAdded;
            Commands.CommandErrored += OnCommandErrored;
        }

        #endregion Constructors

        #region Properties

        private IConfiguration Configuration { get; }
        private DiscordService DiscordService { get; }
        private DatabaseContext DatabaseContext { get; }
        
        internal CommandsNextExtension Commands { get; }

        #endregion Properties

        #region Methods

        public Task Start()
        {
            return Task.CompletedTask;
        }

        private Task OnMessageReactionAdded(MessageReactionAddEventArgs e)
        {
            return Task.CompletedTask;

            //if (e.Emoji != DiscordEmoji.FromName(DiscordService.Client, ":clipboard:"))
            //{
            //    return;
            //}
            //
            //var context = Commands.CreateFakeContext(e.User, e.Channel, $"?quote add {e.Message.Id}", "?",
            //    Commands.FindCommand($"quote add {e.Message.Id}", out var rawArguments), rawArguments);
            //
            //await Commands.ExecuteCommandAsync(context);
        }

        private Task OnCommandErrored(CommandErrorEventArgs e)
        {
            DiscordService.Client.DebugLogger.LogMessage(LogLevel.Critical, nameof(DiscordCommandService), $"\"{e.Command.QualifiedName}\" Command execution failed!", DateTime.UtcNow, e.Exception);
            return Task.CompletedTask;
        }

        #endregion Methods
    }
}
