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
    [Name("Tournaments")]
    [Summary("Manage tournaments and run tournament-specific commands.")]
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
                $"Date: {tourney.GetStartTimeStr()}" + Environment.NewLine
            ));

            // set to this
            await SetCurrentTournamentCommand(Program.Controller.Tournaments.Count);
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

            var msgTask = Program.DiscordIO.SendMessage("", Context.Channel, EmbedHelper.GenerateInfoEmbed(":arrows_counterclockwise: Created tournament. Creating on challonge..."));
            var msg = await msgTask;
            
            var ct = await tourney.SetUpChallonge();
            var atTask = Program.Controller.AddTournament(tourney);
            await atTask;

            await msg.DeleteAsync();

            await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed(
                $"Created the tournament **{tournamentName}**:" + Environment.NewLine + Environment.NewLine +
                $"Date: {tourney.GetStartTimeStr()}" + Environment.NewLine +
                $"Challonge URL: {ct.FullChallongeUrl}"
            ));

            await SetCurrentTournamentCommand(Program.Controller.Tournaments.Count);
        }

        [RequirePermittedRole]
        [Command("viewtournaments")]
        [Alias(new string[] {"vt"})]
        [Summary("Views all current and future tournaments.")]
        public async Task ViewTournaments() {
            var tourneys = Program.Controller.Tournaments;

            if (tourneys == null || tourneys.Count == 0) {
                await ReplyAsync("", false, EmbedHelper.GenerateInfoEmbed("There are no tournaments."));
                return;
            }

            var eb = new EmbedBuilder().WithColor(Discord.Color.Blue);
            for (int i = 0; i < tourneys.Count; ++i) {
                eb.AddField((i+1) + " - " + tourneys[i].Name, "Time: " + tourneys[i].GetStartTimeStr());
            }
            await ReplyAsync("", false, eb.Build());
        }

        // lazy method to just reference the selected tournament in the controller
        static Tournament selectedTourney {
            get => Program.Controller.Tourneys.ActiveTournament;
            set => Program.Controller.Tourneys.ActiveTournament = value;
        }

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
        [Command("viewselectedtournament")]
        [Alias(new string[] { "vst" })]
        [Summary("Views the tournament, including moderation information with the output.")]
        public async Task ViewSelectedTournamnetCommand() {
            if (selectedTourney == null) {
                await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed(
                    "No tournament is currently selected. Set one using `!setcurrenttournament` or `!sct`.")
                );
                return;
            }

            await ReplyAsync("", false, selectedTourney.GetEmbed(true));
        }

        [RequirePermittedRole]
        [Command("allowparticipnts")]
        [Alias(new string[] {"alp"})]
        [Summary("Explicitly allows participants to self-join the selected tournament.")]
        public async Task AllowParticipantsCommand(
            [Summary("A space-separated list of the players to explicitly allow.")][Remainder] string players) {
            
            if (selectedTourney == null) {
                await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed(
                    "No tournament is currently selected. Set one using `!setcurrenttournament` or `!sct`.")
                );
                return;
            }
            
            var playerNames = new StringBuilder();

            foreach (var t in StrListToTeams(players)) {
                try {
                    foreach (var p in t.Players) {
                        if (selectedTourney.AllowPlayer(p)) {
                            await ReplyAsync("", false, EmbedHelper.GenerateInfoEmbed($"Note: Removed {p.IGN} from the denied list."));
                        }
                        playerNames.Append(p.IGN + Environment.NewLine);
                    }
                } catch (Exception e) {
                    await ReplyAsync("", false, EmbedHelper.GenerateWarnEmbed(e.Message));
                    continue;
                }
            }

            if (!string.IsNullOrWhiteSpace(playerNames.ToString()))
                await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed(
                    $"Added the following players to the allowed list of **{selectedTourney.Name}**:" + Environment.NewLine + Environment.NewLine +
                    playerNames.ToString()));
            else
                await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed("No changes were made."));
        }

        // [SignupsChannelOnly]
        [Command("jointournament")]
        [Alias(new string[] {"jt"})]
        [Summary("Join a tournament.")]
        public async Task JoinTournamentCommand(
            [Summary("The a command separated list of tournaments to join, ie 1,2 to join tournaments 1 and 2.")] string tournaments = "1", 
            [Remainder][Summary("An optional tournament join message.")] string message = null) {
                if (Context.Channel.Id != Program.Config.SignupsChannelId) {
                    await ReplyAsync("This command can only be used in the signups channel.");
                    return;
                }

                var player = Program.CurLeaderboard.Players.FirstOrDefault(p => p.DiscordId == Context.Message.Author.Id);
                if (player == null) {
                    await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed("You are not linked to the leaderboard. Please contact an administrator if you believe this is a mistake."));
                    return;
                }

                var spl = tournaments.Split(',');
                foreach (var t_s in spl) {
                    try {
                        int t = Convert.ToInt32(t_s);
                        var tourney = Program.Controller.Tournaments.FirstOrDefault(tm => tm.Index == t);

                        var res = await tourney.Signup(player, message);
                        if (res == 0) {
                            await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed($"You were successfully added to the tournament **{tourney.Name}**."));
                        }
                        else if (res == 1) {
                            await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed($"You are already in the tournament **{tourney.Name}**."));
                        }
                        else {
                            await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed($"You do not have permission to join the tournament **{tourney.Name}**."));
                        }
                    }
                    catch (Exception) {
                        await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed($"Could not infer the tournament from *{t_s}*."));
                    }
                }
            }

        [RequirePermittedRole]
        [Command("denyparticipnts")]
        [Alias(new string[] {"dnp"})]
        [Summary("Explicitly denies participants from self-joining the selected tournament.")]
        public async Task DenyParticipantsCommand(
            [Summary("A space-separated list of the players to explicitly deny.")][Remainder] string players) {
            
            if (selectedTourney == null) {
                await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed(
                    "No tournament is currently selected. Set one using `!setcurrenttournament` or `!sct`.")
                );
                return;
            }
            
            var playerNames = new StringBuilder();

            foreach (var t in StrListToTeams(players)) {
                try {
                    foreach (var p in t.Players) {
                        if (selectedTourney.DenyPlayer(p)) {
                            await ReplyAsync("", false, EmbedHelper.GenerateInfoEmbed($"Note: Removed {p.IGN} from the allow list."));
                        }
                        playerNames.Append(p.IGN + Environment.NewLine);
                    }
                } catch (Exception e) {
                    await ReplyAsync("", false, EmbedHelper.GenerateWarnEmbed(e.Message));
                    continue;
                }
            }

            if (!string.IsNullOrWhiteSpace(playerNames.ToString()))
                await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed(
                    $"Added the following players to the allowed list of **{selectedTourney.Name}**:" + Environment.NewLine + Environment.NewLine +
                    playerNames.ToString()));
            else
                await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed("No changes were made."));
        }

            

        [RequirePermittedRole]
        [Command("addparticipants")]
        [Alias(new string[] {"ap"})]
        [Summary("Adds participants to the selected tournament. Ignores the denied list.")]
        public async Task AddParticipantsCommand(
            [Summary("A space-separated list of the teams to add. Separate players in a team with a comma.")][Remainder] string players) {
            
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

            foreach (var team in StrListToTeams(players)) {
                try {
                    var addTask = selectedTourney.AddTeam(team, true);
                    var editTask = Program.DiscordIO.EditMessage(msg, "", 
                        EmbedHelper.GenerateInfoEmbed($":arrows_counterclockwise: Adding **{team.ToString()}** to the tournament **{selectedTourney.Name}**..."));

                    await editTask;

                    if ((await addTask).Item1)
                        playerNames.Append(team + Environment.NewLine);
                    else
                        await ReplyAsync("", false, EmbedHelper.GenerateWarnEmbed($"**{team}** is already in the bracket."));
                }
                catch (Exception e) {
                    await ReplyAsync("", false, EmbedHelper.GenerateWarnEmbed(e.Message));
                    continue;
                }
            }

            var editTask2 = Program.DiscordIO.EditMessage(msg, "", 
                EmbedHelper.GenerateInfoEmbed($":arrows_counterclockwise: Rebuilding index..."));

            await Task.WhenAll(editTask2, selectedTourney.RebuildIndex());
            await Task.WhenAll(selectedTourney.SendMessage(), msg.DeleteAsync());

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
            [Summary("A space-separated list of the teams to add. Separate players in a team with a comma.")][Remainder] string players) {
            
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

            foreach (var t in StrListToTeams(players)) {
                try {
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

            await Program.Controller.StartTournament(selectedTourney);

            await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed($"Started the tournament {selectedTourney.Name}."));
        }

        [RequirePermittedRole]
        [Command("rebuildindex")]
        [Alias(new string[] { "rbi", "rebuild" })]
        [Summary("Fetches the list of participants and matches from Challonge for the current tournament, then updates the local list of participants.")]
        public async Task RebuildParticipantsCommand() {
            if (selectedTourney == null) {
                await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed(
                    "No tournament is currently selected. Set one using `!setcurrenttournament` or `!sct`.")
                );
                return;
            }

            var msg = await Program.DiscordIO.SendMessage("", 
                Context.Channel, 
                EmbedHelper.GenerateInfoEmbed($":arrows_counterclockwise: Rebuilding the participants and matches index of **{selectedTourney.Name}**..."));

            // rebuild
            try {
                await selectedTourney.RebuildIndex();
            } catch (Exception e) {
                await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed("Aborted rebuilding the index because of the following error:"
                + Environment.NewLine + Environment.NewLine + e.Message));
                await msg.DeleteAsync();

                return;
            }

            await msg.DeleteAsync();
            await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed($"Rebuilt the index of **{selectedTourney.Name}**."));
        }

        [RequirePermittedRole]
        [Command("tournamentstartmatch")]
        [Alias(new string[] { "tsm", "csm" })]
        [Summary("Starts a tournament match between two teams.")]
        public async Task StartMatchCommand(
            [Summary("The first team.")] string team1, 
            [Summary("The second team.")] string team2, 
            [Summary("Whether or not to force start the match even if the player is already playing.")] bool force = false
        ) {
            if (!Program.Controller.IsTourneyActive) {
                await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed(
                    $"The selected tournament **{selectedTourney.Name}** is not active.")
                );
                return;
            }

            var res = await SkillCommands.StartMatch(team1, team2, force, true);
            await ReplyAsync("", false, res.Item2);   
            try {
                await selectedTourney.StartMatch(res.Item1);
                if (selectedTourney.IsChallongeLinked) {
                    await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed(
                        $"Marked the match **{res.Item1.Team1}** vs **{res.Item1.Team2}** as underway on Challonge."
                    ));
                }
            }
            catch (Exception ex) {
                await ReplyAsync("", false, EmbedHelper.GenerateWarnEmbed(ex.Message));
            }     
        }

        [RequirePermittedRole]
        [Command("endtournament")]
        [Alias(new string[] { "podium", "et" })]
        [Summary("Ends a tournament.")]
        public async Task EndTournamentCommand(
            [Remainder][Summary("A comma separated list of the rankings of the teams, starting from the winner to the loser." + 
                "Is optional, but will be autofilled if Challonge is linked.")] string rankings = null
        ) {
            if (!Program.Controller.IsTourneyActive) {
                await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed(
                    $"The selected tournament **{selectedTourney.Name}** is not active.")
                );
                return;
            }

            var t = selectedTourney;

            var rankingsList = new List<MatchRanking>();

            // get rankings
            if (!string.IsNullOrWhiteSpace(rankings)) {
                var teams = StrListToTeams(rankings);
                for (int i = 1; i <= teams.Count; ++i) {
                    rankingsList.Add(new MatchRanking(teams[i - 1], (uint)i));
                }
            }

            if (!(await t.FinalizeTournament(rankingsList))) {
                await ReplyAsync("", false, EmbedHelper.GenerateWarnEmbed("Could not finalise the tournament on Challonge."));
            }
            else if (t.IsChallongeLinked) {
                await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed("Finalised the tournament on Challonge."));
            }

            await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed($"The tournament **{t.Name}** has been marked as completed."));
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
                    $"The selected tournament **{selectedTourney.Name}** is not active.")
                );
                return;
            }
        
            var res = await SkillCommands.FullMatch(team1, team2, result, true);
            await ReplyAsync("", false, res.Item2);

            if (res.Item1.IsDraw) return;

            try {
                await selectedTourney.AddMatch(res.Item1);
                if (selectedTourney.IsChallongeLinked) {
                    await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed(
                        $"Reported **{res.Item1.Winner}** as the winner on Challonge."
                    ));
                }
            }
            catch (Exception ex) {
                await ReplyAsync("", false, EmbedHelper.GenerateWarnEmbed(ex.Message));
            }
        }

        public static List<Team> StrListToTeams(string players, char separator = ' ') {
            var playersSpl = Regex.Split(players, separator + "(?=(?:[^']*'[^']*')*[^']*$)");
            List<Team> returns = new List<Team>();
            foreach (var p in playersSpl) {
                returns.Add(SkillCommands.StrToTeam(p));
            }
            return returns;
        }
    }
}