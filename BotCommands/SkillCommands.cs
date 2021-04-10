using Discord.Commands;
using Discord;
using System;
using System.Threading.Tasks;
using OpenSkillBot.Skill;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenSkillBot.BotCommands
{
    [Name("Matches")]
    [Summary("Start, calculate and manage matches.")]
    public class SkillCommands : ModuleBase<SocketCommandContext> {

        [RequirePermittedRole]
        [Command("startmatch")]
        [Alias(new string[] { "sm" })]
        [Summary("Starts a match between two teams.")]
        public async Task StartMatchCommand(
            [Summary("The first team.")] string team1, 
            [Summary("The second team.")] string team2, 
            [Summary("Whether or not to force start the match even if the player is already playing.")] bool force = false
        ) {
            await ReplyAsync("", false, (await StartMatch(team1, team2, force)).Item2);        
        }

        // allow this code to also be used for !tournamentstartmatch (!tsm)
        public static async Task<Tuple<PendingMatch, Embed>> StartMatch(string team1, string team2, bool force, bool isTourney = false) {
            try {
                Team t1;
                Team t2;
                try {
                    t1 = StrToTeam(team1);
                    t2 = StrToTeam(team2);
                }
                catch (Exception e) {
                    return new Tuple<PendingMatch, Embed>(null, EmbedHelper.GenerateErrorEmbed(e.Message));
                }

                var t1_s = string.Join(", ", t1.Players.Select(x => x.IGN));
                var t2_s = string.Join(", ", t2.Players.Select(x => x.IGN));

                var match = await Program.Controller.StartMatchAction(t1, t2, isTourney, force);

                if (match == null) {
                    return new Tuple<PendingMatch, Embed>(null, EmbedHelper.GenerateErrorEmbed(
                        $"One or more of the players you specified are already playing. Please set `force` to `true` if you wish to override this protection.")); 
                }

                string output = $"Started the match between **{t1_s}** and **{t2_s}**.";

                return new Tuple<PendingMatch, Embed>(match, EmbedHelper.GenerateSuccessEmbed(output));
            }
            catch (Exception e) {
                return new Tuple<PendingMatch, Embed>(null, EmbedHelper.GenerateErrorEmbed(e.Message));
            }
        }

        
        public static IEnumerable<OldPlayerData> ToOldPlayerData(IDictionary<Player, (double Mu, double Sigma)> data) {
            foreach (var k in data.Keys) {
                yield return new OldPlayerData() { UUId = k.UUId, Mu = data[k].Mu, Sigma = data[k].Sigma};
            }
        }

        public static IEnumerable<OldPlayerData> ToOldPlayerData(IEnumerable<Team> teams) {
            foreach (var t in teams) {
                foreach (var p in t.Players) {
                    yield return new OldPlayerData() { UUId = p.UUId, Mu = p.Mu, Sigma = p.Sigma };
                }
            }
        }


        [RequirePermittedRole]
        [Command("fullmatch")]
        [Alias(new string[] { "fm" })]
        [Summary("Calculates a full match between two teams.")]
        public async Task FullMatchCommand([Summary("The first team.")] string team1, [Summary("The second team.")] string team2,
            [Summary("The result of a match. By default, the first team wins. Enter 0 for a draw. Enter -1 to cancel the match.")] int result = 1
        ) {
            await ReplyAsync("", false, (await FullMatch(team1, team2, result)).Item2);
        }

        [RequirePermittedRole]
        [Command("decay")]
        [Summary("Decays players' ranks.")]
        public async Task DecayPlayersCommand(
            [Remainder][Summary("The players to decay. Leave empty to automatically determine them, or type \"all\" to decay all players.")] string players = null
        ) {

            List<Player> toDecay = new List<Player>();

            if (string.IsNullOrEmpty(players)) {
                toDecay = DecayAction.GetPlayersToDecay().ToList();
            } else if (players.ToLower().Trim().Equals("all")) {
                toDecay = Program.CurLeaderboard.Players;
            } else {
                var teams = TournamentCommands.StrListToTeams(players);

                foreach (var t in teams) {
                    foreach (var p in t.Players) {
                        toDecay.Add(p);
                    }
                }
            }

            if (toDecay.Count == 0) {
                await ReplyAsync("", false, EmbedHelper.GenerateInfoEmbed(
                    "No players to decay."));
                return;
            }

            var da = new DecayAction(toDecay);
            await Program.Controller.AddAction(da);

            var output = string.Join(", ", toDecay.Select(p => p.IGN));

            await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed(
                "Decayed the following players: " + Environment.NewLine + Environment.NewLine + output));
        }

        // allow this code to also be used for !tournamentfullmatch (!tfm)
        public static async Task<Tuple<MatchAction, Embed>> FullMatch(string team1, string team2, int result = 1, bool isTourney = false) {
            try {
                Team t1;
                Team t2;
                try {
                    t1 = StrToTeam(team1);
                    t2 = StrToTeam(team2);
                }
                catch (Exception e) {
                    return new Tuple<MatchAction, Embed>(null, EmbedHelper.GenerateErrorEmbed(e.Message));
                }

                var t1_s = string.Join(", ", t1.Players.Select(x => x.IGN));
                var t2_s = string.Join(", ", t2.Players.Select(x => x.IGN));

                var match = await Program.Controller.AddMatchAction(t1, t2, result, isTourney);

                string output = result != -1 ? 
                    $"The match between **{t1_s}** and **{t2_s}** has been calculated." : $"The match between **{t1_s}** and **{t2_s}** has been cancelled.";

                return new Tuple<MatchAction, Embed>(match, EmbedHelper.GenerateSuccessEmbed(output, $"ID: {(match == null ? "n/a" : match.ActionId)}"));
            }
            catch (Exception e) {
                return new Tuple<MatchAction, Embed>(null, EmbedHelper.GenerateErrorEmbed(e.Message));
            }
        }

        [RequirePermittedRole]
        [Command("insertmatch")]
        [Summary("Inserts a match **before** a specified match/action and recalculates the following matches.")]
        public async Task InsertMatchCommand([Summary("The match/action that will follow the match to be inserted.")] string id, [Summary("The first team.")] string team1, [Summary("The second team.")] string team2,
            [Summary("The result of the match to insert. By default, the first team wins. Enter 0 for a draw.")] int result = 1) {

            // this code is to find the match to insert before

            var msg = await Program.DiscordIO.SendMessage("", 
                Context.Channel, 
                EmbedHelper.GenerateInfoEmbed($":arrows_counterclockwise: Finding the match **{id}**..."));

            var match = FindMatch(id);

            if (match == null) {
                await msg.DeleteAsync();
                await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed($"Could not find a match with the ID **{id}**."
                    ));
                return;
            }

            await Program.DiscordIO.EditMessage(msg, "", 
                EmbedHelper.GenerateInfoEmbed(
                $":arrows_counterclockwise: Calculating the match and re-calculating subsequent matches... (this might take a while)"));


            try {
                // Create the match.
                Team t1;
                Team t2;
                try {
                    t1 = StrToTeam(team1);
                    t2 = StrToTeam(team2);
                }
                catch (Exception e) {
                    await msg.DeleteAsync();
                    await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed(e.Message));
                    return;
                }

                var t1_s = string.Join(", ", t1.Players.Select(x => x.IGN));
                var t2_s = string.Join(", ", t2.Players.Select(x => x.IGN));

                MatchAction newMatch = new MatchAction(t1, t2, result == 0);

                // Insert the new match before the specified match
                int depth = await match.InsertBefore(newMatch) - 1;

                // Output it
                string output = $"The match between **{t1_s}** and **{t2_s}** has been calculated." + 
                        Environment.NewLine + Environment.NewLine +
                        $"**{depth}** subsequent match{(depth == 1 ? " was" : "es were")} re-calculated.";

                await msg.DeleteAsync();
                await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed(output, $"ID: {newMatch.ActionId}"));
            }
            catch (Exception e) {
                await msg.DeleteAsync();
                await ReplyAsync(e.Message);
            }
        }
    

        [RequirePermittedRole]
        [Command("editmatch")]
        [Summary("Edits a given match.")]
        public async Task EditMatchCommand([Summary("The match that will be edited.")] string id, [Summary("The first team.")] string team1, [Summary("The second team.")] string team2,
            [Summary("The result of a match. By default, the first team wins. Enter 0 for a draw.")] int result = 1) {

            if (result == -1) {
                await ReplyAsync("", false, EmbedHelper.GenerateWarnEmbed("As -1 was given as the result, the given match will be undone."));
                await this.UndoCommand(id);
                return;
            }

            // this code is to find the match to insert before
            var msg = await Program.DiscordIO.SendMessage("", 
                Context.Channel, 
                EmbedHelper.GenerateInfoEmbed($":arrows_counterclockwise: Finding the match **{id}**..."));

            var matchFound = FindMatch(id);

            if (matchFound == null) {
                await msg.DeleteAsync();
                await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed($"Could not find a match with the ID **{id}**."
                    ));
                return;
            }

            MatchAction match;
            try {
                match = (MatchAction)matchFound;
            }
            catch (InvalidCastException) {
                await msg.DeleteAsync();
                await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed($"The action with ID **{id}** is not a match action."
                    ));
                return;
            }

            await Program.DiscordIO.EditMessage(msg, "", 
                EmbedHelper.GenerateInfoEmbed(
                $":arrows_counterclockwise: Re-calculating the match and re-calculating subsequent matches... (this might take a while)"));

            try {
                // make sure they are in the correct order
                if (result == 2) {
                    var temp = team2;
                    team2 = team1;
                    team1 = temp;
                }

                // Edit the match.
                Team t1;
                Team t2;
                try {
                    t1 = StrToTeam(team1);
                    t2 = StrToTeam(team2);
                }
                catch (Exception e) {
                    await msg.DeleteAsync();
                    await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed(e.Message));
                    return;
                }

                var t1_s = string.Join(", ", t1.Players.Select(x => x.IGN));
                var t2_s = string.Join(", ", t2.Players.Select(x => x.IGN));

                // Edit the match and recalculate.
                int depth = await match.Edit(t1, t2, result == 0);

                // Output it
                string output = $"The match has been edited to **{t1_s}** vs **{t2_s}**." + 
                        Environment.NewLine + Environment.NewLine +
                        $"**{depth}** subsequent match{(depth == 1 ? " was" : "es were")} re-calculated.";

                await msg.DeleteAsync();
                await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed(output, $"ID: {match.ActionId}"));
            }
            catch (Exception e) {
                await msg.DeleteAsync();
                await ReplyAsync(e.Message);
            }
        }

        [RequirePermittedRole]
        [Command("addrank")]
        [Summary("Adds a new player rank. If you want ranks to be updated, you must run !refreshrank after this.")]
        public Task AddRankCommand([Summary("The minimum skill to be in this rank.")] int lowerBound, 
            [Summary("The role ID that people in this rank should be assigned")] ulong roleId,
            [Remainder][Summary("The name of the rank.")] string rankName) {

            var rank = new Rank(lowerBound, roleId, rankName);

            Program.Config.Ranks.Add(rank);
            Program.Config.Ranks.Sort((x, y) => y.LowerBound.CompareTo(x.LowerBound));

            Program.SerializeConfig();

            return ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed($"Successfully added rank **{rankName}**"));
        }

        [RequirePermittedRole]
        [Command("deleterank")]
        [Summary("Removes a player rank. If you want ranks to be updated, you must run !refreshrank after this.")]
        public Task DeleteRank([Remainder][Summary("The exact name of the rank (capitalisation does not matter).")] string rankName) {

            var rank = Program.Config.Ranks.FirstOrDefault(r => r.Name.ToLower().Equals(rankName.ToLower()));

            Program.Config.Ranks.Remove(rank);

            Program.SerializeConfig();

            return ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed($"Removed the rank **{rankName}**"));
        }

        [RequirePermittedRole]
        [Command("viewranks")]
        [Summary("Displays a list of all the rank and their boundaries.")]
        public async Task ViewRanks() {
            var sb = new StringBuilder();
            foreach (var r in Program.Config.Ranks) {
                sb.Append($"**{r.Name}** - {r.LowerBound}+{Environment.NewLine}");
            }

            await ReplyAsync("", false, EmbedHelper.GenerateInfoEmbed(sb.ToString()));
        }

        [RequirePermittedRole]        
        [Command("refreshrank", RunMode = RunMode.Async)]
        [Summary("Refreshes the rank of all players, or a specific list of players.")]
        public async Task RefreshRankCommand([Summary(
            "A comma separated list of the players to update. Type \"all\" to refresh all players.")][Remainder] 
            string players = "all") {

            var channel = Context.Channel;

            var msg = await Program.DiscordIO.SendMessage("", channel, 
                EmbedHelper.GenerateInfoEmbed($":arrows_counterclockwise: Initialising..."));

            List<Player> playersList;

            if (players.Equals("all")) {
                playersList = Program.CurLeaderboard.Players;
            }
            else {
                playersList = new List<Player>();

                var playersSpl = Regex.Split(players, ",(?=(?:[^']*'[^']*')*[^']*$)");

                foreach (var p in playersSpl) {
                    var player = PlayerManagementCommands.FindPlayer(p);
                    if (player == null) {
                        await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed($"Could not find the player **{p}**."));
                        return;
                    }
                    playersList.Add(player);
                }
            }

            var total = playersList.Count;

            int i = 0;
            foreach (var p in playersList) {
                var percent = ++i * 100 / total;
                await Program.DiscordIO.EditMessage(msg, "", 
                    EmbedHelper.GenerateInfoEmbed(
                        $":arrows_counterclockwise: Processing {p.IGN} {Environment.NewLine}{Environment.NewLine} {i} of {total} players processed ({percent}%)"));
                p.QueueRankRefresh(true);
                // don't spam the api
                await Task.Delay(100);
            }

            await msg.DeleteAsync();

            Program.CurLeaderboard.InvokeChange();
            
            await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed($"Refreshed the ranks of {playersList.Count} players."));
        }


        [RequirePermittedRole]
        [Command("resetleaderboard")]
        [Summary("Resets the leaderboard. Requires a token for safety. If no token is provided, one is generated and you must re-type the command with the token.")]
        public async Task ResetLeaderboardCommand([Summary("The provided reset token.")]string token = null) {
            if (string.IsNullOrEmpty(token)) {
                await ReplyAsync("", false, EmbedHelper.GenerateInfoEmbed($"Please type **{Program.prefix}resetleaderboard** `{Program.Controller.GenerateResetToken()}` to confirm this action."));
            }
            else if (Program.Controller.CheckResetToken(token)) {
                var msg = await Program.DiscordIO.SendMessage("", Context.Channel, EmbedHelper.GenerateInfoEmbed("Resetting the leaderboard... (this might take a while to avoid spamming the API)"));
                await Program.CurLeaderboard.Reset();
                await msg.DeleteAsync();
                await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed($"The leaderboard has been reset."));
            }
            else {
                await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed("Invalid token."));
            }
        }

        [RequirePermittedRole]
        [Command("undo")]
        [Summary("Undos a given number of matches.")]
        public async Task UndoCommand([Summary("The number of matches to undo (default is 1).")] int count = 1) {
            if (count == 0) {
                await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed("Couldn't undo any matches/actions because you didn't tell me to undo any, nerd. :sunglasses:"));
                return;
            }

            var msg = await Program.DiscordIO.SendMessage("", 
                Context.Channel, 
                EmbedHelper.GenerateInfoEmbed($":arrows_counterclockwise: Initialising..."));

            StringBuilder sb = new StringBuilder();
            sb.Append($"**The following {(count == 1 ? "match/action was" : "matches/actions were")} undone:**{Environment.NewLine}{Environment.NewLine}");
            for (int i = 0; i < count; ++i) {

                var editAction = Program.DiscordIO.EditMessage(msg, "",
                    EmbedHelper.GenerateInfoEmbed($":arrows_counterclockwise: Undoing match/action {i + 1} of {count}..."));

                var undoAction = Program.Controller.UndoAction();

                var match = await undoAction;
                await editAction;

                if (match == null) break;

                sb.Append($"**{match.ToString()}**{Environment.NewLine}");
            }

            await Task.WhenAll(
                msg.DeleteAsync(),
                ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed(sb.ToString()))
            );
            
            // reset the pointer if the currently focused match has been affected
            if (count >= p_depth) await PtrResetCommand();
        }

        [RequirePermittedRole]
        [Command("undo")]
        [Summary("Undos a specific match or action.")]
        public async Task UndoCommand([Summary("The ID of the match or action.")] string id) {

            // special case
            if (Program.Controller.LatestAction.ActionId.Equals(id)) {
                await UndoCommand(1);
                return;
            }

            var match = FindMatch(id);

            if (match == null) {
                await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed($"Could not find a match or action with the ID **{id}**."
                    ));
                return;
            }

            var msg = await Program.DiscordIO.SendMessage("", 
                Context.Channel, 
                EmbedHelper.GenerateInfoEmbed($":arrows_counterclockwise: Undoing action and re-calculating subsequent matches/actions... (this might take a while)"));

            int depth = await match.Undo();

            StringBuilder sb = new StringBuilder();
            sb.Append($"**The following match/action was undone:**{Environment.NewLine}{Environment.NewLine}");

            sb.Append($"**{match.ToString()}**{Environment.NewLine}{Environment.NewLine}");

            sb.Append($"**{depth}** subsequent action{(depth == 1 ? " was" : "s were")} re-calculated.");
            
            await msg.DeleteAsync();

            await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed(sb.ToString()));

            // reset the pointer if the currently focused match has been affected
            if (id.Equals(ptrIndicator)) await PtrResetCommand();
        }

        #region match pointer

        /*

        The purpose of the "match pointer" is to allow for commands such as !undo <id>, !insertmatch, !editmatch etc without
        needing to copy the ID. This is an advantage for mobile users, who are unable to easily copy the ID, or copy the ID at all.

        */

        async Task viewPtrPos() {
            if (p_match == null) {
                p_match = Program.Controller.LatestAction;
                p_depth = 1;
            }
            if (p_match == null) {
                await ReplyAsync("", false, EmbedHelper.GenerateInfoEmbed("There are no actions/matches."));
                return;
            }

            await ReplyAsync("", false, EmbedHelper.GenerateInfoEmbed(
                "**The pointer is currently on the match/action:**" + Environment.NewLine + Environment.NewLine +
                $"**{p_match.ToString()}**", "ID: " + p_match.ActionId));
        }

        private static BotAction p_match;
        private static int p_depth = 1;

        [RequirePermittedRole]
        [Command("ptrreset")]
        [Summary("Resets the pointer to the latest match/action.")]
        public async Task PtrResetCommand() {
            p_match = Program.Controller.LatestAction;
            p_depth = 1;
            await ReplyAsync("", false, EmbedHelper.GenerateInfoEmbed(
                "**The pointer has been reset to the latest match/action.**"));
        }

        [RequirePermittedRole]
        [Command("viewptr")]
        [Summary("Outputs the match/action currently focused on by the pointer.")]
        public async Task ViewPtrCommand() {
            await viewPtrPos();
        }

        [RequirePermittedRole]
        [Command("ptrmv")]
        [Summary("Moves the pointer by a specified amount.")]
        public async Task PtrMoveCommand(
            [Summary("The amount of actions to move the pointer by. +ve values move it closer to the latest action.")] int delta
        ) {
            if (p_match == null) p_match = Program.Controller.LatestAction;
            if (p_match == null) {
                await ReplyAsync("", false, EmbedHelper.GenerateInfoEmbed("There are no actions/matches."));
                return;
            }
            
            if (delta > 0) {
                while (delta != 0 && p_match.NextAction != null) {
                    p_match = p_match.NextAction;
                    --delta;
                    --p_depth;
                }
            }
            else if (delta < 0) {
                while (delta != 0 && p_match.PrevAction != null) {
                    p_match = p_match.PrevAction;
                    ++delta;
                    ++p_depth;
                }
            }
            await viewPtrPos();
        }


        #endregion

        private const string ptrIndicator = "ptr";
        public static BotAction FindMatch(string id) {
            if (id.Equals(ptrIndicator)) return p_match;
            if (Program.Controller.LatestAction.ActionId.Equals(id)) 
                return Program.Controller.LatestAction;
            return Program.Controller.FindAction(id);
        }

        // helpers

        public static Team StrToTeam(string teamStr) {
            teamStr = Regex.Replace(teamStr, ", (?=(?:[^']*'[^']*')*[^']*$)", ",");
            var split = Regex.Split(teamStr, ",(?=(?:[^']*'[^']*')*[^']*$)");
            var players = new Player[split.Length];

            for (int i = 0; i < split.Length; ++i) {

                // remove speech marks
                if (split[i][0] == '"' && split[i][split[i].Length - 1] == '"') 
                    split[i] = split[i].Substring(1, split[i].Length - 2);

                var player = PlayerManagementCommands.FindPlayer(split[i]);
                if (player != null) {
                    players[i] = player;
                }
                else {
                    throw new Exception($"Could not find player \"{split[i]}\".");
                }
            }

            return new Team() { Players = players };
        }
    }
}