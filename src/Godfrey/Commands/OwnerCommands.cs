using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Godfrey.Extensions;
using Godfrey.Models.Context;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Newtonsoft.Json.Linq;

namespace Godfrey.Commands
{
    public class OwnerCommands : BaseCommandModule
    {
        [Command("unicode"), RequireOwner]
        public async Task UnicodeAsync(CommandContext ctx, [RemainingText] string text)
        {
            var sb = new StringBuilder();
            sb.AppendLine("```");

            var chars = new Dictionary<char, string>();
            
            foreach (var character in text)
            {
                if (chars.ContainsKey(character))
                {
                    sb.AppendLine($"{character} | {chars[character]}");

                    continue;
                }

                var value = string.Format("{0:X4}", (int)character);
                var request = WebRequest.CreateHttp($"https://codepoints.net/api/v1/codepoint/{value}");
                using (var response = await request.GetResponseAsync())
                {
                    using (var stream = response.GetResponseStream())
                    {
                        if (stream == null)
                        {
                            var embed = Constants.Embeds.Presets.Error(description: "Der Webserver antwortet nicht.");
                            await ctx.RespondAsync(embed: embed);
                            return;
                        }

                        using (var reader = new StreamReader(stream))
                        {
                            var json = JObject.Parse(await reader.ReadToEndAsync());
                            var name = json.Value<string>("na");
                            chars.Add(character, name);
                            sb.AppendLine($"{character} | {name}");
                        }
                    }
                }
            }
            sb.AppendLine("```");

            await ctx.RespondAsync(sb.ToString());
        }

        [Command("sql"), RequireOwner]
        public async Task SqlAsync(CommandContext ctx, [RemainingText] string sql)
        {
            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                var reader = await uow.Database.ExecuteSqlQueryAsync(sql);
                var read = reader.DbDataReader;

                if (!read.HasRows)
                {
                    await ctx.RespondAsync(embed: new DiscordEmbedBuilder()
                                                   .WithTitle("Keine Ergebnisse.")
                                                   .WithDescription($"Es wurden keine Ergebnisse für die SQL-Query ``{sql}`` gefunden.")
                                                   .WithColor(DiscordColor.Red).Build());
                    return;
                }

                var data = new Dictionary<string, List<string>>();

                for (var i = 0; i < read.FieldCount; i++)
                {
                    data.Add(read.GetName(i), new List<string>());
                }

                while (await reader.ReadAsync())
                {
                    for (var i = 0; i < data.Count; i++)
                    {
                        var value = read.GetValue(i).ToString();
                        data[read.GetName(i)].Add(value);
                    }
                }

                var sb = new StringBuilder();
                var lengths = new int[data.Count];
                var items = new List<string>();
                for (var i = 0; i < data.Count; i++)
                {
                    lengths[i] = data[data.Keys.ElementAt(i)].Aggregate("", (max, cur) => max.Length > cur.Length ? max : cur).Length;

                    if (data.Keys.ElementAt(i).Length > lengths[i])
                    {
                        lengths[i] = data.Keys.ElementAt(i).Length;
                    }

                    lengths[i] = 0 - lengths[i];

                    var formatter = $"{{0,{lengths[i]}}}";
                    items.Add(string.Format(formatter, data.Keys.ElementAt(i)));
                }

                var header = string.Join(" | ", items);
                sb.AppendLine(header);
                sb.AppendLine(new string('-', header.Length));
                items = new List<string>();

                for (var i = 0; i < data.ElementAt(0).Value.Count; i++)
                {
                    // ReSharper disable once AccessToModifiedClosure
                    items.AddRange(data.Select((t, j) => $"{{0,{lengths[j]}}}").Select((formatter, j) => string.Format(formatter, data[data.Keys.ElementAt(j)][i])));

                    var row = string.Join(" | ", items);
                    sb.AppendLine(row);
                    items = new List<string>();
                }

                await ctx.RespondAsync($"```{Environment.NewLine}{sb}{Environment.NewLine}```");
            }
        }

        [Command("eval"), Description("Evaluates C# code."), RequireOwner]
        public async Task EvalAsync(CommandContext ctx, [RemainingText] string code)
        {
            using (var uow = await DatabaseContextFactory.CreateAsync(Butler.ButlerConfig.ConnectionString))
            {
                var cs1 = code.IndexOf("```", StringComparison.InvariantCulture) + 3;
                cs1 = code.IndexOf('\n', cs1) + 1;
                var cs2 = code.LastIndexOf("```", StringComparison.InvariantCulture);

                if (cs1 == -1 || cs2 == -1)
                {
                    throw new ArgumentException("You need to wrap the code into a code block.", nameof(code));
                }

                var cs = code.Substring(cs1, cs2 - cs1);

                var msg = await ctx.RespondAsync("", embed: new DiscordEmbedBuilder()
                                                                    .WithColor(DiscordColor.Rose)
                                                                    .WithDescription("Evaluating...")
                                                                    .Build()).ConfigureAwait(false);

                try
                {
                    var globals = new EvalParameters(ctx, uow);
                    
                    var sopts = ScriptOptions.Default;
                    sopts.AddImports("System", "System.Collections.Generic", "System.Linq", "System.Text", "System.Threading.Tasks", "Microsoft.EntityFrameworkCore", "Microsoft.EntityFrameworkCore.Extensions.Internal", "DSharpPlus", "DSharpPlus.CommandsNext", "DSharpPlus.Interactivity", "Godfrey", "Godfrey.Collections", "Godfrey.Commands", "Godfrey.Exceptions", "Godfrey.Extensions", "Godfrey.Helpers", "Godfrey.Models", "Godfrey.Models.Common", "Godfrey.Models.Configs", "Godfrey.Models.Context", "Godfrey.Models.Quotes", "Godfrey.Models.Servers", "Godfrey.Models.Users");
                    sopts.AddReferences(AppDomain.CurrentDomain.GetAssemblies().Where(xa => !xa.IsDynamic && !string.IsNullOrWhiteSpace(xa.Location)));

                    var script = CSharpScript.Create(cs, sopts, typeof(EvalParameters));
                    script.Compile();
                    var result = await script.RunAsync(globals).ConfigureAwait(false);

                    if (string.IsNullOrWhiteSpace(result?.ReturnValue?.ToString()))
                    {
                        await msg.ModifyAsync(embed: new DiscordEmbedBuilder()
                                                      .WithTitle("Evaluation successful")
                                                      .WithDescription("No result was returned.")
                                                      .WithColor(DiscordColor.Azure)
                                                      .Build()).ConfigureAwait(false);
                        return;
                    }

                    await msg.ModifyAsync(embed: new DiscordEmbedBuilder()
                                                  .WithTitle("Evaluation result")
                                                  .WithDescription(result.ReturnValue.ToString())
                                                  .WithColor(DiscordColor.Azure)
                                                  .Build()).ConfigureAwait(false);
                }
                catch (Exception ex)
                {

                    await msg.ModifyAsync(embed: new DiscordEmbedBuilder()
                                                  .WithTitle("Evaluation result")
                                                  .WithDescription(string.Concat("**", ex.GetType().ToString(), "**: ", ex.Message))
                                                  .WithColor(DiscordColor.Red)
                                                  .Build()).ConfigureAwait(false);
                }
            }
        }
    }

    public class EvalParameters
    {
        public DiscordClient Client;

        public DiscordMessage Message { get; }
        public DiscordChannel Channel { get; }
        public DiscordGuild Guild { get; }
        public DiscordUser User { get; }
        public DiscordMember Member { get; }
        public CommandContext Context { get; }
        public DatabaseContext UnitOfWork { get; }

        public EvalParameters(CommandContext context, DatabaseContext uow)
        {
            Client = context.Client;

            Message = context.Message;
            Channel = Message.Channel;
            Guild = Channel.Guild;
            User = Message.Author;
            Member = Guild?.GetMemberAsync(User.Id).ConfigureAwait(false).GetAwaiter().GetResult();
            Context = context;
            
            UnitOfWork = uow;
        }
    }
}
