using Discord.Commands;
using Discord;
using System;
using System.Threading.Tasks;
using OpenSkillBot.Skill;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using OpenSkillBot.Tournaments;

namespace OpenSkillBot.BotCommands
{
    [RequirePermittedRole]
    [Name("Tournament Commands")]
    public class TournamentCommands : ModuleBase<SocketCommandContext> {
        [RequirePermittedRole]
        [Command("createtournament")]
        [Alias(new string[] {"ct"})]
        [Summary("Creates a tournament.")]
        public async Task CreateTournamentCommand(
            [Summary("The name of the tournament.")] string tournamentName,
            [Summary("The UTC time of the tournament, in the form HHMM (ie, 1600 for 4PM UTC)")] ushort utcTime,
            [Summary("The calendar date of the tournament in DD/MM/YYYY, DD/MM/YY, or DD. The missing fields will be autofilled.")] string calendarDate = ""
        ) {
            var now = DateTime.UtcNow;

            // parse date
            int[] date = {now.Day, now.Month, now.Year};
            var dateSpl = calendarDate.Split('/', 3, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < dateSpl.Length; ++i) {
                date[i] = Convert.ToInt32(dateSpl[i]);
            }

            // Auto date
            var time = new DateTime(
                date[2], date[1], date[0],
                (utcTime / 100) % 24,
                (utcTime % 100) % 60,
                0);

            if (dateSpl.Length >= 3 && time < now) {
                await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed("The tournament cannot be in the past."));
                return;
            }

            // Delegate function to add the required amount of time given how many parameters were given
            Func<DateTime, int, DateTime> Add;
            switch (dateSpl.Length) {
                case 0:
                    Add = AddDays;
                break;
                case 1:
                    Add = AddMonths;
                break;
                case 2:
                    Add = AddYears;
                break;
                default: // default case to prevent compiler from complaining of missing case
                    Add = (a,b) => DateTime.Now;
                    break;
            }

            // Keep adding until the tournament is in the future.
            while (time < now) {
                time = Add(time, 1);
            }
                
            var tourney = new Tournament(time, tournamentName, TournamentType.DoubleElim);

            await Program.Controller.AddTourmanet(tourney);

            await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed(
                $"Created the tournament **{tournamentName}**:" + Environment.NewLine + Environment.NewLine +
                $"Date: {tourney.GetTimeStr()}" + Environment.NewLine
            ));
        }

        [RequirePermittedRole]
        [Command("viewtournaments")]
        [Alias(new string[] {"vt"})]
        [Summary("Views all current and future tournaments.")]
        public async Task ViewTourmanets() {
            var tourneys = Program.Controller.Tournaments;

            if (tourneys == null || tourneys.Count == 0) {
                await ReplyAsync("", false, EmbedHelper.GenerateInfoEmbed("There are no tournaments."));
                return;
            }

            var eb = new EmbedBuilder().WithColor(Discord.Color.Blue);
            for (int i = 0; i < tourneys.Count; ++i) {
                eb.AddField((i+1) + " - " + tourneys[i].Name, "Time: " + tourneys[i].GetTimeStr());
            }
            await ReplyAsync("", false, eb.Build());
        }

        [RequirePermittedRole]
        [Command("addparticipants")]
        [Alias(new string[] {"ap"})]
        [Summary("Adds a participant to a selected tournament.")]
        public async Task AddParticipantsCommand(
            [Summary("The index of the tournament (find this using !viewtournaments or !vt).")] int tourneyIndex,
            [Summary("A comma separated list of the players to add.")][Remainder] string players) {
            var tourneys = Program.Controller.Tournaments;
            var t = tourneys[tourneyIndex-1];

            var msg = await Program.DiscordIO.SendMessage("", 
                Context.Channel, 
                EmbedHelper.GenerateInfoEmbed($":arrows_counterclockwise: Adding players to the tournament **{t.Name}**..."));

            if (tourneys == null || tourneys.Count == 0) {
                await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed("There are no tournaments."));
                return;
            }

            var playerNames = new StringBuilder();
            var playersSpl = players.Split(',');
            foreach (var p in playersSpl) {
                var pl = PlayerManagementCommands.FindPlayer(p);
                if (pl == null) await ReplyAsync("", false, EmbedHelper.GenerateWarnEmbed($"Could not find the player **{p}**."));
                else {
                    await t.AddPlayer(pl, true);
                    playerNames.Append(pl.IGN + Environment.NewLine);
                }
            }

            Program.Controller.SerializeTourneys();
            await t.SendMessage();

            await msg.DeleteAsync();

            await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed(
                $"Added the following players to the tournament **{t.Name}**:" + Environment.NewLine + Environment.NewLine +
                playerNames.ToString()));
        }

        static DateTime AddDays(DateTime time, int days) {
            return time.AddDays(days);
        }
        static DateTime AddMonths(DateTime time, int months) {
            return time.AddMonths(months);
        }
        static DateTime AddYears(DateTime time, int years) {
            return time.AddYears(years);
        }
    }
}