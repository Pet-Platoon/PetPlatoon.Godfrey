using System;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using DSharpPlus;
using Microsoft.EntityFrameworkCore;
using PetPlatoon.Godfrey.Database;
using PetPlatoon.Godfrey.Database.Servers;
using PetPlatoon.Godfrey.Database.Users;
using PetPlatoon.Godfrey.Services.Common;

namespace PetPlatoon.Godfrey.Services
{
    public class DatabaseSeedService : IService
    {
        #region Constructors

        public DatabaseSeedService(DiscordService discordService, DatabaseContext databaseContext)
        {
            DatabaseContext = databaseContext;
            DiscordService = discordService;

            Timer = new Timer(TimeSpan.FromSeconds(15).TotalMilliseconds);
            Timer.Elapsed += Tick;
        }

        #endregion Constructors

        #region Properties

        private DatabaseContext DatabaseContext { get; }
        private DiscordService DiscordService { get; }

        private Timer Timer { get; }

        #endregion Properties

        #region Methods

        public Task Start()
        {
            Timer.Start();

            return Task.CompletedTask;
        }

        private async void Tick(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            DiscordService.Client.DebugLogger.LogMessage(LogLevel.Info, "DatabaseSeeding", "Started database seeding.",
                DateTime.UtcNow);

            await DeleteUnknownGuildMembers();
            await DeleteLeftServers();

            await AddUnknownUsers();
            await AddConnectedGuilds();
            await AddConnectedMembers();

            await UpdateGuilds();

            DiscordService.Client.DebugLogger.LogMessage(LogLevel.Info, "DatabaseSeeding",
                $"Seeded database. Next seed on {DateTime.UtcNow.AddMilliseconds(Timer.Interval)}", DateTime.UtcNow);
        }

        private async Task DeleteUnknownGuildMembers()
        {
            var guilds = DiscordService.Client.Guilds.Values.ToArray();

            var leftGuilds = DatabaseContext.Servers.Where(x => guilds.All(y => y.Id != x.Id)).ToArray();
            var membersFromLeftGuilds = leftGuilds.SelectMany(x => x.Members).ToArray();
            DatabaseContext.ServerMembers.RemoveRange(membersFromLeftGuilds);
            await DatabaseContext.SaveChangesAsync();

            DiscordService.Client.DebugLogger.LogMessage(LogLevel.Debug, "DatabaseSeeding",
                $"Deleted {membersFromLeftGuilds.Length} members from disconnected guilds.", DateTime.UtcNow);

            var members = guilds.SelectMany(x => x.Members).ToArray();
            var guildMembersToDelete = DatabaseContext.ServerMembers
                .Where(x => !members.Any(y => y.Key == x.UserId && y.Value.Guild.Id == x.ServerId)).ToArray();
            DatabaseContext.ServerMembers.RemoveRange(guildMembersToDelete);
            await DatabaseContext.SaveChangesAsync();

            DiscordService.Client.DebugLogger.LogMessage(LogLevel.Debug, "DatabaseSeeding",
                $"Deleted {guildMembersToDelete.Length} disconnected members.", DateTime.UtcNow);
        }

        private async Task DeleteLeftServers()
        {
            var leftGuilds = DatabaseContext.Servers.Where(x => !DiscordService.Client.Guilds.ContainsKey(x.Id))
                .ToArray();
            DatabaseContext.Servers.RemoveRange(leftGuilds);
            await DatabaseContext.SaveChangesAsync();

            DiscordService.Client.DebugLogger.LogMessage(LogLevel.Debug, "DatabaseSeeding",
                $"Deleted {leftGuilds.Length} disconnected guilds.", DateTime.UtcNow);
        }

        private async Task AddUnknownUsers()
        {
            var guilds = DiscordService.Client.Guilds.Values.ToArray();
            var members = guilds.SelectMany(x => x.Members).ToArray();
            var unknownMembers = members.Where(x => !DatabaseContext.Users.Any(y => y.Id == x.Key)).ToArray();

            foreach (var discordMember in unknownMembers)
            {
                try
                {
                    if (await DatabaseContext.Users.AnyAsync(x => x.Id == discordMember.Key))
                    {
                        continue;
                    }

                    var user = new User
                    {
                            Id = discordMember.Key,
                            Name = discordMember.Value.Username
                    };

                    await DatabaseContext.Users.AddAsync(user);
                    await DatabaseContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    DiscordService.Client.DebugLogger.LogMessage(LogLevel.Error, "DatabaseSeeding",
                                                                 $"Error seeding user {discordMember.Key} ({discordMember.Value.Username})", DateTime.UtcNow, ex);
                }
            }

            DiscordService.Client.DebugLogger.LogMessage(LogLevel.Debug, "DatabaseSeeding",
                $"Added {unknownMembers.Length} unknown users.", DateTime.UtcNow);
        }

        private async Task AddConnectedGuilds()
        {
            var guilds = DiscordService.Client.Guilds.Values.ToArray();
            var unknownGuilds = guilds.Where(x => DatabaseContext.Servers.All(y => y.Id != x.Id)).ToArray();

            foreach (var discordGuild in unknownGuilds)
            {
                var guild = new Server
                {
                    Id = discordGuild.Id,
                    Name = discordGuild.Name,
                    OwnerId = discordGuild.Owner.Id
                };

                await DatabaseContext.Servers.AddAsync(guild);
                await DatabaseContext.SaveChangesAsync();
            }

            DiscordService.Client.DebugLogger.LogMessage(LogLevel.Debug, "DatabaseSeeding",
                $"Added {unknownGuilds.Length} unknown guilds.", DateTime.UtcNow);
        }

        private async Task AddConnectedMembers()
        {
            var guilds = DiscordService.Client.Guilds.Values.ToArray();
            var members = guilds.SelectMany(x => x.Members).ToArray();
            var unknownMembers = members.Where(x =>
                DatabaseContext.ServerMembers.All(y => y.UserId != x.Key && y.ServerId != x.Value.Guild.Id)).ToArray();

            foreach (var discordMember in unknownMembers)
            {
                var member = new ServerMember
                {
                    UserId = discordMember.Key,
                    ServerId = discordMember.Value.Guild.Id
                };

                await DatabaseContext.ServerMembers.AddAsync(member);
                await DatabaseContext.SaveChangesAsync();
            }

            DiscordService.Client.DebugLogger.LogMessage(LogLevel.Debug, "DatabaseSeeding",
                $"Added {unknownMembers.Length} unknown guild members.", DateTime.UtcNow);
        }

        private async Task UpdateGuilds()
        {
            var guilds = DiscordService.Client.Guilds.Values.ToArray();

            foreach (var discordGuild in guilds)
            {
                var server = await DatabaseContext.Servers.SingleAsync(x => x.Id == discordGuild.Id);

                if (server.Name == discordGuild.Name && server.OwnerId == discordGuild.Owner.Id)
                {
                    continue;
                }

                server.Name = discordGuild.Name;
                server.OwnerId = discordGuild.Owner.Id;

                await DatabaseContext.SaveChangesAsync();
            }

            DiscordService.Client.DebugLogger.LogMessage(LogLevel.Debug, "DatabaseSeeding",
                $"Checked {guilds.Length} servers for updates.", DateTime.UtcNow);
        }

        #endregion Methods
    }
}
