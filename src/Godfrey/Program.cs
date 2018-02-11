using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fluent.Task;
using Godfrey.Models.Context;
using Newtonsoft.Json;
using TaskScheduler = Fluent.Task.TaskScheduler;

namespace Godfrey
{
    internal static class Program
    {
        private static void Main() => Run().Wait();

        private static TaskScheduler Scheduler { get; set; }

        private static async Task Run()
        {
            var cfg = new ButlerConfig();
            if (!File.Exists("config.json"))
            {
                var json = JsonConvert.SerializeObject(cfg);
                File.WriteAllText("config.json", json, new UTF8Encoding(false));
                Console.WriteLine("Config file was not found, a new one was generated. Fill it with proper values and rerun this program");
                Console.ReadKey();

                return;
            }

            (await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString)).Dispose();

            Scheduler = TaskScheduler.Instance().Start();
            Schedule.Instance(CasinoCronAction)
                    .SetTimeHour(6)
                    .SetFrequencyTime(TimeSpan.FromDays(1))
                    .RunLoop(Scheduler);

            var tskl = new List<Task>();
            for (var i = 0; i < Butler.ButlerConfig.ShardCount; i++)
            {
                var bot = new Butler(i);
                tskl.Add(bot.RunAsync());
                await Task.Delay(7500);
            }

            await Task.WhenAll(tskl);

            await Task.Delay(-1);
        }

        private static async void CasinoCronAction(object parameter)
        {
            Console.WriteLine("Adding 15 coins to every user that was active in the last two days");
            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                foreach (var user in uow.Users.Where(x => DateTime.UtcNow - x.LastCasinoCommandIssued <= TimeSpan.FromDays(2)))
                {
                    user.Coins += 15;
                }

                await uow.SaveChangesAsync();
            }
        }
    }
}
