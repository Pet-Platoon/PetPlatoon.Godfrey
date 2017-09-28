using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Godfrey.Models.Context;
using Newtonsoft.Json;

namespace Godfrey
{
    internal class Program
    {
        private static void Main() => Run().Wait();

        private static async Task Run()
        {
            var cfg = new ButlerConfig();
            var json = string.Empty;
            if (!File.Exists("config.json"))
            {
                json = JsonConvert.SerializeObject(cfg);
                File.WriteAllText("config.json", json, new UTF8Encoding(false));
                Console.WriteLine("Config file was not found, a new one was generated. Fill it with proper values and rerun this program");
                Console.ReadKey();

                return;
            }

            DatabaseContextFactory.Create(Butler.ButlerConfig.ConnectionString).Dispose();

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
    }
}
