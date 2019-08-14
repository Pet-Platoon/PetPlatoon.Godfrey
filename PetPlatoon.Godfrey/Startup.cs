using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Autofac.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PetPlatoon.Godfrey.Database;
using PetPlatoon.Godfrey.Messenger;
using PetPlatoon.Godfrey.Services.Common;

namespace PetPlatoon.Godfrey
{
    internal class Startup
    {
        #region Constructors

        internal Startup()
        {
            if (Instance != null)
            {
                throw new NotSupportedException("Startup already initialized!");
            }

            Instance = this;
            Random = new Random();

            var builder = new ContainerBuilder();

            RegisterConfiguration(builder);
            RegisterEventAggregator(builder);
            RegisterDatabase(builder);
            RegisterServices(builder);

            Container = builder.Build();
        }

        #endregion Constructors

        #region Properties

        internal static Startup Instance { get; private set; }
        
        internal static Random Random { get; private set; }
        
        internal IContainer Container { get; }

        #endregion Properties

        #region Methods

        private void RegisterConfiguration(ContainerBuilder builder)
        {
            var configBuilder = new ConfigurationBuilder();
            configBuilder.SetBasePath(Path.Join(Directory.GetCurrentDirectory(), "config"));
            configBuilder.AddXmlFile("database.xml");
            configBuilder.AddXmlFile("discord.xml");
            configBuilder.AddXmlFile("permissions.xml");

            var configModule = new ConfigurationModule(configBuilder.Build());

            builder.Register(c => configModule.Configuration).As<IConfiguration>().SingleInstance();
        }

        private void RegisterEventAggregator(ContainerBuilder builder)
        {
            builder.Register(c => EventAggregator.Instance).AsSelf().SingleInstance();
        }

        private void RegisterDatabase(ContainerBuilder builder)
        {
            builder.Register(c =>
            {
                var config = c.Resolve<IConfiguration>();
                var optionsBuilder = new DbContextOptionsBuilder<DatabaseContext>();
                optionsBuilder.UseMySql(config.GetConnectionString(config["environment"] ?? "development"));

                var options = optionsBuilder.Options;

                var databaseContext = new DatabaseContext(options);
                databaseContext.Database.Migrate();

                return databaseContext;
            }).AsSelf().InstancePerDependency();
        }

        private void RegisterServices(ContainerBuilder builder)
        {
            var services = GetType().Assembly.GetTypes().Where(type => type.GetInterfaces().Contains(typeof(IService)))
                                    .ToArray();
            builder.RegisterTypes(services).AsSelf().SingleInstance();
        }

        internal async Task Run()
        {
            using (var scope = Container.BeginLifetimeScope())
            {
                var tasks = new List<Task>();
                var services = GetType().Assembly.GetTypes().Where(type => type.GetInterfaces().Contains(typeof(IService)))
                                        .ToArray();
                
                foreach (var service in services)
                {
                    if (!scope.IsRegistered(service))
                    {
                        Console.WriteLine($"Found {nameof(IService)} which isn't registered: {service.FullName}");
                        continue;
                    }

                    var instance = (IService)scope.Resolve(service);
                    var task = instance.Start();
                    Console.WriteLine($"Issued Start Task on {service.FullName}");
                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);
                await Task.Delay(-1);
            }
        }

        #endregion Methods
    }
}
