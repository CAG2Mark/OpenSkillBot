using Discord.Commands;
using Discord;
using System;
using System.Threading.Tasks;
using OpenSkillBot.Skill;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using OpenSkillBot.Tournaments;
using System.Text.RegularExpressions;

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
            [Summary("The calendar date of the tournament in DD/MM/YYYY, DD/MM, or DD. The missing fields will be autofilled.")] string calendarDate = ""
        ) {
            var tourney = Tournament.GenerateTournament(tournamentName, utcTime, calendarDate);

            await Program.Controller.AddTournament(tourney);

            await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed(
                $"Created the tournament **{tournamentName}**:" + Environment.NewLine + Environment.NewLine +
                $"Date: {tourney.GetTimeStr()}" + Environment.NewLine
            ));

            if (Program.Controller.Tournaments.Count == 1) await SetCurrentTournamentCommand(1);
        }

        [RequirePermittedRole]
        [Command("createchallongetournament")]
        [Alias(new string[] {"cct"})]
        [Summary("Creates a tournament and sets it up on Challonge.")]
        public async Task CreateTournamentCommand(
            [Summary("The name of the tournament.")] string tournamentName,
            [Summary("The format of the tournament.")] string format,
            [Summary("The UTC time of the tournament, in the form HHMM (ie, 1600 for 4PM UTC)")] ushort utcTime,
            [Summary("The calendar date of the tournament in DD/MM/YYYY, DD/MM, or DD. The missing fields will be autofilled.")] string calendarDate = ""
        ) {
            var tourney = Tournament.GenerateTournament(tournamentName, utcTime, calendarDate, format);
            var ct = await tourney.SetUpChallonge();

            await Program.Controller.AddTournament(tourney);

            await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed(
                $"Created the tournament **{tournamentName}**:" + Environment.NewLine + Environment.NewLine +
                $"Date: {tourney.GetTimeStr()}" + Environment.NewLine +
                $"Challonge URL: {ct.FullChallongeUrl}"
            ));

            if (Program.Controller.Tournaments.Count == 1) await SetCurrentTournamentCommand(1);
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

        static Tournament selectedTourney = null;

        [RequirePermittedRole]
        [Command("setcurrenttournament")]
        [Alias(new string[] {"sct"})]
        [Summary("Selects the tournament you want to add/remove players from or modify.")]
        public async Task SetCurrentTournamentCommand(
            [Summary("The index of the tournament (find this using !viewtournaments or !vt).")] int tourneyIndex
        ) {
            var tourneys = Program.Controller.Tournaments;
            if (tourneyIndex < 1 || tourneyIndex > tourneys.Count) {
                await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed(
                    $"{tourneyIndex} is out of range; it should be between 1 and {tourneys.Count} inclusive.")
                );
                return;
            }
            var t = tourneys[tourneyIndex-1];

            selectedTourney = t;

            await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed($"Set the selected tournament to **{t.Name}**."));
        }
            

        [RequirePermittedRole]
        [Command("addparticipants")]
        [Alias(new string[] {"ap"})]
        [Summary("Adds participants to the selected tournament.")]
        public async Task AddParticipantsCommand(
            [Summary("A space-separated list of the players to add. Separate players in a team with a comma.")][Remainder] string players) {
            
            if (selectedTourney == null) {
                await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed(
                    "No tournament is currently selected. Set one using `!setcurrenttournament` or `!sct`.")
                );
                return;
            }
            var msg = await Program.DiscordIO.SendMessage("", 
                Context.Channel, 
                EmbedHelper.GenerateInfoEmbed($":arrows_counterclockwise: Adding players to the tournament **{selectedTourney.Name}**..."));

            var playerNames = new StringBuilder();

            var playersSpl = Regex.Split(players, " (?=(?:[^']*'[^']*')*[^']*$)");

            foreach (var t in playersSpl) {
                try {
                    // get team
                    Team team = SkillCommands.strToTeam(t);

                    if ((await selectedTourney.AddTeam(team, true)).Item1)
                        playerNames.Append(team + Environment.NewLine);
                    else
                        await ReplyAsync("", false, EmbedHelper.GenerateWarnEmbed($"**{team}** is already in the bracket."));
                }
                catch (Exception e) {
                    await ReplyAsync("", false, EmbedHelper.GenerateWarnEmbed(e.Message));
                    continue;
                }
            }

            await selectedTourney.SendMessage();

            await msg.DeleteAsync();

            if (!string.IsNullOrWhiteSpace(playerNames.ToString()))
                await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed(
                    $"Added the following players/teams to the tournament **{selectedTourney.Name}**:" + Environment.NewLine + Environment.NewLine +
                    playerNames.ToString()));
            else
                await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed("No changes were made."));
        }

        [RequirePermittedRole]
        [Command("removeparticipants")]
        [Alias(new string[] {"rp"})]
        [Summary("Removes participants from the selected tournament.")]
        public async Task RemoveParticipantsCommand(
            [Summary("A space-separated list of the players to add. Separate players in a team with a comma.")][Remainder] string players) {
            
            if (selectedTourney == null) {
                await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed(
                    "No tournament is currently selected. Set one using `!setcurrenttournament` or `!sct`.")
                );
                return;
            }
            var msg = await Program.DiscordIO.SendMessage("", 
                Context.Channel, 
                EmbedHelper.GenerateInfoEmbed($":arrows_counterclockwise: Removing players from the tournament **{selectedTourney.Name}**..."));

            var playerNames = new StringBuilder();
            var playersSpl = Regex.Split(players, " (?=(?:[^']*'[^']*')*[^']*$)");

            foreach (var t_str in playersSpl) {
                try {
                    var t = SkillCommands.strToTeam(t_str);

                    if (await selectedTourney.RemoveTeam(t, true))
                        playerNames.Append(t + Environment.NewLine);
                    else
                        await ReplyAsync("", false, EmbedHelper.GenerateWarnEmbed($"Could not remove **{t}** as they were not in the tournament."));
                } catch (Exception e) {
                    await ReplyAsync("", false, EmbedHelper.GenerateWarnEmbed(e.Message));
                    continue;
                }
            }

            await selectedTourney.SendMessage();

            await msg.DeleteAsync();

            if (!string.IsNullOrWhiteSpace(playerNames.ToString()))
                await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed(
                    $"Removed the following players/teams from tournament **{selectedTourney.Name}**:" + Environment.NewLine + Environment.NewLine +
                    playerNames.ToString()));
            else
                await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed("No changes were made."));
        }

        [RequirePermittedRole]
        [Command("deletetournament")]
        [Summary("Deletes the selected tournament.")]
        public async Task DeleteTournamentCommand() {
            if (selectedTourney == null) {
                await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed(
                    "No tournament is currently selected. Set one using `!setcurrenttournament` or `!sct`.")
                );
                return;
            }

            var t = selectedTourney;
            await Program.Controller.RemoveTournament(t);
            selectedTourney = null;
            await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed($"Deleted the tournament **{t.Name}**."));
        }

        [RequirePermittedRole]
        [Command("starttournament")]
        [Alias(new string[] { "st", "start" })]
        [Summary("Starts the tournament.")]
        public async Task StartTournamentCommand() {
            if (selectedTourney == null) {
                await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed(
                    "No tournament is currently selected. Set one using `!setcurrenttournament` or `!sct`.")
                );
                return;
            }

            await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed($"Started the tournament {selectedTourney.Name}."));
        }

        [RequirePermittedRole]
        [Command("rebuildparticipants")]
        [Alias(new string[] { "rb", "rebuild" })]
        [Summary("Fetches the list of participants from Challonge for the current tournament, then updates the local list of participants.")]
        public async Task RebuildParticipantsCommand() {
            if (selectedTourney == null) {
                await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed(
                    "No tournament is currently selected. Set one using `!setcurrenttournament` or `!sct`.")
                );
                return;
            }

            var msg = await Program.DiscordIO.SendMessage("", 
                Context.Channel, 
                EmbedHelper.GenerateInfoEmbed($":arrows_counterclockwise: Rebuilding the participants list of **{selectedTourney.Name}**..."));

            // rebuild
            try {
                await selectedTourney.RebuildParticipantsList();
            } catch (Exception e) {
                await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed("Aborted rebuilding the participants list because of the following error:"
                + Environment.NewLine + Environment.NewLine + e.Message));
                await msg.DeleteAsync();

                return;
            }

            await msg.DeleteAsync();
            await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed($"Rebuilt the participants of list of {selectedTourney.Name}."));
        }

        [RequirePermittedRole]
        [Command("tournamentfullmatch")]
        [Alias(new string[] { "tfm", "cfm" })]
        [Summary("Calculates a full match between two teams, and reports it to the current tournament.")]
        public async Task TournamentFullMatchCommand([Summary("The first team.")] string team1, [Summary("The second team.")] string team2,
            [Summary("The result of a match. By default, the first team wins. Enter 0 for a draw.")] int result = 1
        ) {
            if (!Program.Controller.IsTourneyActive) {
                await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed(
                    "No tournament is currently running.")
                );
                return;
            }
            await ReplyAsync("", false, (await SkillCommands.FullMatch(team1, team2, result)).Item2);
        }



    }
}