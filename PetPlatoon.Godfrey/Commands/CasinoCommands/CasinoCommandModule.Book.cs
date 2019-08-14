using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using PetPlatoon.Godfrey.Attributes;
using PetPlatoon.Godfrey.Database;
using PetPlatoon.Godfrey.Extensions;
using CooldownBucketType = PetPlatoon.Godfrey.Attributes.CooldownBucketType;

namespace PetPlatoon.Godfrey.Commands.CasinoCommands
{
    public partial class CasinoCommandModule
    {
        #region Properties

        private static readonly string[] Icons =
        {
                "🤠",
                "🤴",
                "🤴",
                "🤴",
                "📕",
                "📕",
                "📕",
                "🐦",
                "🐦",
                "🐦",
                "🐦",
                "🐦",
                "🐦",
                "🐦",
                "🐞",
                "🐞",
                "🐞",
                "🐞",
                "🐞",
                "🐞",
                "🐞",
                "🇦",
                "🇦",
                "🇦",
                "🇦",
                "🇦",
                "🇦",
                "🇦",
                "🇦",
                "🇦",
                "🇦",
                "🇦",
                "🇦",
                "🇦",
                "🇦",
                "🇦",
                "🇦",
                "🇦",
                "🇦",
                "🇦",
                "🇦",
                "🇦",
                "🇦",
                "🇦",
                "🇦",
                "🇦",
                "🇦",
                "🇦",
                "🇦",
                "🇦",
                "🇦",
                "🇦",
                "🇦",
                "🇦",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇰",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇶",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🇯",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
                "🔟",
        };

        private const long MaxBetPerLine = 5;
        private const long Lines = 9;
        private const long MaxBet = MaxBetPerLine * Lines;

        #endregion Properties

        #region Commands

        [Command("bookofra"), Aliases("book", "ra", "bor"), GodfreyChannelType(Constants.Casino.Channel), GodfreyCooldown(1, 30, CooldownBucketType.User)]
        public async Task BookOfRaCommandAsync(CommandContext ctx, long bet)
        {
            var databaseContext = ctx.Services.GetService<DatabaseContext>();

            DiscordEmbedBuilder embed;

            var user = await ctx.User.GetUserAsync(databaseContext);
            user.LastCasinoCommandIssued = DateTime.UtcNow;
            await databaseContext.SaveChangesAsync();

            if (bet <= 0)
            {
                embed = Constants.Embeds.Presets.Error(description: "Wen willst du hier verarschen?");
                await ctx.RespondAsync(embed: embed);
                return;
            }

            if (user.Coins < bet)
            {
                embed = Constants.Embeds.Presets.Error(description: $"Du besitzt nur {user.Coins} Coins.");
                await ctx.RespondAsync(embed: embed);
                return;
            }

            if (bet > MaxBet)
            {
                embed = Constants.Embeds.Presets.Error(description: $"Du kannst nur {MaxBet} Coins setzen.");
                await ctx.RespondAsync(embed: embed);
                return;
            }

            var symbols = GenerateSymbols();
            var sb = new StringBuilder();

            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 5; j++)
                {
                    sb.Append($"{symbols[i, j]} ");
                }

                sb.Append(Environment.NewLine);
            }

            var lines = CheckWinningLines(symbols).Where(x => x.Count > 1 && x.Value > 0);

            var winningLines = lines as WinningLine[] ?? lines.ToArray();
            if (winningLines.Any())
            {
                var embedSb = new StringBuilder();

                var outPercentage = 0f;

                embedSb.AppendLine("```");

                foreach (var line in winningLines)
                {
                    var count = line.Count + line.BookCount;
                    var percentage = line.Value / (float)count / 100;
                    outPercentage += percentage;
                    embedSb.AppendLine($"Line {line.Line}: {line.Count} * {line.Symbol} ({line.BookCount} Bücher)");
                }

                var coins = (long)(bet * outPercentage);

                embedSb.AppendLine("```");

                embedSb.AppendLine($"Du erspielst dir {coins} Coins!");

                embed = Constants.Embeds.Presets.Success("Book of Ra", embedSb.ToString());
                await ctx.RespondAsync(sb.ToString(), false, embed);

                user.Coins += coins;
                await databaseContext.SaveChangesAsync();
                return;
            }

            embed = Constants.Embeds.Presets.Error("Book of Ra", $"Du verlierst {bet} Coins.");
            await ctx.RespondAsync(sb.ToString(), embed: embed);

            user.Coins -= bet;
            await databaseContext.SaveChangesAsync();
        }

        #endregion Commands

        #region Helpers

        private IEnumerable<WinningLine> CheckWinningLines(string[,] symbols)
        {
            var winningLines = new int[9][];
            winningLines[0] = new[]{ 0, 1, 2, 1, 0 };
            winningLines[1] = new[]{ 0, 0, 0, 0, 0 };
            winningLines[2] = new[]{ 0, 0, 1, 2, 2 };
            winningLines[3] = new[]{ 1, 2, 2, 2, 1 };
            winningLines[4] = new[]{ 1, 1, 1, 1, 1 };
            winningLines[5] = new[]{ 1, 0, 0, 0, 1 };
            winningLines[6] = new[]{ 2, 2, 1, 0, 0 };
            winningLines[7] = new[]{ 2, 2, 2, 2, 2 };
            winningLines[8] = new[]{ 2, 1, 0, 1, 2 };

            var lines = new List<WinningLine>();

            for (var i = 0; i < winningLines.GetLength(0); i++)
            {
                CheckWinningLine(symbols, winningLines[i], out var symbol, out var count, out var bookCount);
                lines.Add(new WinningLine(i + 1, symbol, count, bookCount));
            }

            return lines;
        }

        private static int GetValueForSymbol(string symbol, int count)
        {
            switch (symbol)
            {
                case "🤠":
                {
                    if (count == 5)
                    {
                        return 40000;
                    }

                    if (count == 4)
                    {
                        return 8000;
                    }

                    if (count == 3)
                    {
                        return 800;
                    }

                    if (count == 2)
                    {
                        return 80;
                    }

                    return 0;
                }

                case "🤴":
                {
                    if (count == 5)
                    {
                        return 16000;
                    }

                    if (count == 4)
                    {
                        return 3200;
                    }

                    if (count == 3)
                    {
                        return 320;
                    }

                    if (count == 2)
                    {
                        return 40;
                    }

                    return 0;
                }

                case "📕":
                {
                    if (count == 5)
                    {
                        return 14400;
                    }

                    if (count == 4)
                    {
                        return 1440;
                    }

                    if (count == 3)
                    {
                        return 144;
                    }

                    return 0;
                }

                case "🐦":
                {
                    if (count == 5)
                    {
                        return 6000;
                    }

                    if (count == 4)
                    {
                        return 800;
                    }

                    if (count == 3)
                    {
                        return 240;
                    }

                    if (count == 2)
                    {
                        return 40;
                    }

                    return 0;
                }

                case "🐞":
                {
                    if (count == 5)
                    {
                        return 6000;
                    }

                    if (count == 4)
                    {
                        return 800;
                    }

                    if (count == 3)
                    {
                        return 240;
                    }

                    if (count == 2)
                    {
                        return 40;
                    }

                    return 0;
                }

                case "🇦":
                {
                    if (count == 5)
                    {
                        return 1200;
                    }

                    if (count == 4)
                    {
                        return 320;
                    }

                    if (count == 3)
                    {
                        return 40;
                    }

                    return 0;
                }

                case "🇰":
                case "🇶":
                case "🇯":
                case "🔟":
                {
                    if (count == 5)
                    {
                        return 800;
                    }

                    if (count == 4)
                    {
                        return 200;
                    }

                    if (count == 3)
                    {
                        return 40;
                    }

                    return 0;
                }

                default:
                {
                    return 0;
                }
            }
        }

        private string[,] GenerateSymbols()
        {
            var symbols = new string[3, 5];

            for (var i = 0; i < symbols.GetLength(0); i++)
            {
                for (var j = 0; j < symbols.GetLength(1); j++)
                {
                    var symbol = Icons[Startup.Random.Next(Icons.Length)];
                    symbols[i, j] = symbol;
                }
            }

            return symbols;
        }

        private int FindFirstIndexOfSpecificSymbol(string[,] symbols, IReadOnlyList<int> rows, string symbol)
        {
            for (var i = 0; i < 5; i++)
            {
                if (IsSymbolEqual(symbols, rows[i], i, symbol, false))
                {
                    return i;
                }
            }

            return -1;
        }

        private int GetBookCount(string[,] symbols, IReadOnlyList<int> rows)
        {
            var bookCount = 0;
            var firstIndex = FindFirstIndexOfSpecificSymbol(symbols, rows, "📕");

            if (firstIndex == -1)
            {
                return 0;
            }

            for (var i = firstIndex; i < 5; i++)
            {
                if (IsSymbolEqual(symbols, rows[i], i, "📕"))
                {
                    bookCount++;
                }
            }

            return bookCount;
        }

        private void CheckWinningLine(string[,] symbols, IReadOnlyList<int> rows, out string symbol, out int count, out int bookCount)
        {
            symbol = symbols[rows[0], 0];

            count = 0;
            bookCount = GetBookCount(symbols, rows);

            for (var i = 0; i < 5; i++)
            {
                if (IsSymbolEqual(symbols, rows[i], i, symbol, false))
                {
                    count++;
                }
            }
        }

        private bool IsSymbolEqual(string[,] symbols, int row, int column, string symbol, bool includeBook = true)
        {
            return symbols[row, column] == symbol || includeBook && symbols[row, column] == "📕";
        }

        #endregion Helpers

        #region Inner Classes

        private class WinningLine
        {
            public int Line { get; }
            public string Symbol { get; }
            public int Count { get; }
            public int BookCount { get; }
            public int Value => GetValueForSymbol(Symbol, Count + BookCount);

            public WinningLine(int line, string symbol, int count, int bookCount)
            {
                Line = line;
                Symbol = symbol;
                Count = count;
                BookCount = bookCount;
            }
        }

        #endregion Inner Classes
    }
}
