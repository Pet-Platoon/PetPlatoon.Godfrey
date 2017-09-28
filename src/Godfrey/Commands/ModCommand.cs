using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;

namespace Godfrey.Commands
{
    /*[Group("mod")]
    public class ModCommand
    {
        [Command("votekick")]
        public async Task VotekickAsync(CommandContext ctx, DiscordMember member, TimeSpan duration)
        {
            var interactivity = ctx.Client.GetInteractivityModule();

            var timeFormat = $"{(duration.Duration().Days > 0 ? string.Format("{0:0} Tag{1}, ", duration.Days, duration.Days == 1 ? string.Empty : "e") : string.Empty)}{(duration.Duration().Hours > 0 ? string.Format("{0:0} Stunde{1}, ", duration.Hours, duration.Hours == 1 ? string.Empty : "n") : string.Empty)}{(duration.Duration().Minutes > 0 ? string.Format("{0:0} Minute{1}, ", duration.Minutes, duration.Minutes == 1 ? string.Empty : "n") : string.Empty)}{(duration.Duration().Seconds > 0 ? string.Format("{0:0} Sekunde{1}", duration.Seconds, duration.Seconds == 1 ? string.Empty : "n") : string.Empty)}";

            var embedBuilder = new DiscordEmbedBuilder()
                .WithTitle("Votekick!")
                .WithColor(DiscordColor.Orange)
                .WithDescription($"Votekick für {member.Mention} wurde gestartet.{Environment.NewLine}Ihr habt {timeFormat} um zu entscheiden!{Environment.NewLine}Die Ja-Stimmen müssen mit 2/3 überwiegen{Environment.NewLine}{Environment.NewLine}✔ **Kick!**{Environment.NewLine}❌ **Kein Kick!**");
            var msg = await ctx.RespondAsync(embed: embedBuilder.Build());

            await msg.CreateReactionAsync(DiscordEmoji.FromUnicode("✔"));
            await Task.Delay(TimeSpan.FromMilliseconds(250));
            await msg.CreateReactionAsync(DiscordEmoji.FromUnicode("❌"));
            await Task.Delay(TimeSpan.FromMilliseconds(250));

            var pollResult = await interactivity.CollectReactionsAsync(msg, duration);

            var yesCount = pollResult.Reactions.FirstOrDefault(x => x.Key.ToString() == "✔").Value;
            var noCount = pollResult.Reactions.FirstOrDefault(x => x.Key.ToString() == "❌").Value;
            var count = yesCount + noCount;
            var needed = 2 / 3;
            var got = yesCount / count;

            if (count >= needed)
            {
                await ctx.RespondAsync($"Votekick für {member.Mention} ist beendet. {yesCount} stimmen für und {noCount} gegen den Kick. Damit ergibt sich ein Prozentsatz von {got * 100}%. Weg mit dem Schandgesicht!");
                return;
            }

            await ctx.RespondAsync($"Votekick für {member.Mention} ist beendet. {yesCount} stimmen für und {noCount} gegen den Kick. Damit ergibt sich ein Prozentsatz von {got * 100}%. Das Schandgesicht darf bleiben.");
        }

        [Command("votemute")]
        public async Task VotemuteAsync(CommandContext ctx, DiscordMember member, TimeSpan duration, TimeSpan muteDuration)
        {
            var interactivity = ctx.Client.GetInteractivityModule();

            var timeFormat = $"{(duration.Duration().Days > 0 ? string.Format("{0:0} Tag{1}, ", duration.Days, duration.Days == 1 ? string.Empty : "e") : string.Empty)}{(duration.Duration().Hours > 0 ? string.Format("{0:0} Stunde{1}, ", duration.Hours, duration.Hours == 1 ? string.Empty : "n") : string.Empty)}{(duration.Duration().Minutes > 0 ? string.Format("{0:0} Minute{1}, ", duration.Minutes, duration.Minutes == 1 ? string.Empty : "n") : string.Empty)}{(duration.Duration().Seconds > 0 ? string.Format("{0:0} Sekunde{1}", duration.Seconds, duration.Seconds == 1 ? string.Empty : "n") : string.Empty)}";
            var muteTimeFormat = $"{(muteDuration.Duration().Days > 0 ? string.Format("{0:0} Tag{1}, ", muteDuration.Days, muteDuration.Days == 1 ? string.Empty : "e") : string.Empty)}{(muteDuration.Duration().Hours > 0 ? string.Format("{0:0} Stunde{1}, ", muteDuration.Hours, muteDuration.Hours == 1 ? string.Empty : "n") : string.Empty)}{(muteDuration.Duration().Minutes > 0 ? string.Format("{0:0} Minute{1}, ", muteDuration.Minutes, muteDuration.Minutes == 1 ? string.Empty : "n") : string.Empty)}{(muteDuration.Duration().Seconds > 0 ? string.Format("{0:0} Sekunde{1}", muteDuration.Seconds, muteDuration.Seconds == 1 ? string.Empty : "n") : string.Empty)}";

            var embedBuilder = new DiscordEmbedBuilder()
                .WithTitle("Votemute!")
                .WithColor(DiscordColor.Orange)
                .WithDescription($"Votemute für {member.Mention} wurde gestartet.{Environment.NewLine}Ihr habt {timeFormat} um zu entscheiden! Der User wird dann für {muteTimeFormat} gemutet.{Environment.NewLine}Die Ja-Stimmen müssen mit 1/2 überwiegen{Environment.NewLine}{Environment.NewLine}✔ **Mute!**{Environment.NewLine}❌ **Kein Mute!**");
            var msg = await ctx.RespondAsync(embed: embedBuilder.Build());

            await msg.CreateReactionAsync(DiscordEmoji.FromUnicode("✔"));
            await Task.Delay(TimeSpan.FromMilliseconds(250));
            await msg.CreateReactionAsync(DiscordEmoji.FromUnicode("❌"));
            await Task.Delay(TimeSpan.FromMilliseconds(250));

            var pollResult = await interactivity.CollectReactionsAsync(msg, duration);

            var yesCount = pollResult.Reactions.FirstOrDefault(x => x.Key.ToString() == "✔").Value;
            var noCount = pollResult.Reactions.FirstOrDefault(x => x.Key.ToString() == "❌").Value;
            var count = yesCount + noCount;
            var needed = 1 / 2;
            var got = yesCount / count;

            if (count >= needed)
            {
                await ctx.RespondAsync($"Votemute für {member.Mention} ist beendet. {yesCount} stimmen für und {noCount} gegen den Mute. Damit ergibt sich ein Prozentsatz von {got * 100}%. Du hältscht jetzt dein Schnautzen!");
                if (ctx.Guild.Id == 123572824454463488)
                {
                    var role = ctx.Guild.GetRole(363062102111289354);
                    await member.GrantRoleAsync(role);
                    await Task.Delay(muteDuration);
                    await member.RevokeRoleAsync(role);
                }
                return;
            }

            await ctx.RespondAsync($"Votekick für {member.Mention} ist beendet. {yesCount} stimmen für und {noCount} gegen den Mute. Damit ergibt sich ein Prozentsatz von {got * 100}%. Kein Mute für ihn.");
        }
    }*/
}
