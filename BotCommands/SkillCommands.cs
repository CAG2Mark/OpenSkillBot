using Discord.Commands;
using Discord;
using System;
using System.Threading.Tasks;
using OpenTrueskillBot.Skill;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace OpenTrueskillBot.BotCommands
{
    [RequirePermittedRole]
    [Name("Skill Commands")]
    public class SkillCommands : ModuleBase<SocketCommandContext> {

        [RequirePermittedRole]
        [Command("startmatch")]
        [Alias(new string[] { "sm" })]
        [Summary("Starts a match between two teams.")]
        public async Task StartMatchCommand(
            [Summary("The first team.")] string team1, 
            [Summary("The second team.")] string team2, 
            [Summary("Whether or not to force start the match even if the player is already playing.")] bool force = false) {

            try {
                Team t1;
                Team t2;
                try {
                    t1 = strToTeam(team1);
                    t2 = strToTeam(team2);
                }
                catch (Exception e) {
                    await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed(e.Message));
                    return;
                }

                var t1_s = string.Join(", ", t1.Players.Select(x => x.IGN));
                var t2_s = string.Join(", ", t2.Players.Select(x => x.IGN));

                var match = await Program.Controller.StartMatchAction(t1, t2, false, force);

                if (match == null) {
                    await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed(
                        $"One or more of the players you specified are already playing. Please set `force` to `true` if you wish to override this protection.")); 
                    return; 
                }

                string output = $"Started the match between **{t1_s}** and **{t2_s}**.";

                await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed(output));
            }
            catch (Exception e) {
                await ReplyAsync(e.Message);
            }
        }

        [RequirePermittedRole]
        [Command("fullmatch")]
        [Alias(new string[] { "fm" })]
        [Summary("Calculates a full match between two teams.")]
        public async Task FullMatchCommand([Summary("The first team.")] string team1, [Summary("The second team.")] string team2,
            [Summary("The result of a match. By default, the first team wins. Enter 0 for a draw.")] int result = 1) {

            try {
                Team t1;
                Team t2;
                try {
                    t1 = strToTeam(team1);
                    t2 = strToTeam(team2);
                }
                catch (Exception e) {
                    await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed(e.Message));
                    return;
                }

                var t1_s = string.Join(", ", t1.Players.Select(x => x.IGN));
                var t2_s = string.Join(", ", t2.Players.Select(x => x.IGN));

                var match = await Program.Controller.AddMatchAction(t1, t2, result);

                string output = $"The match between **{t1_s}** and **{t2_s}** has been calculated.";

                await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed(output, $"ID: {match.ActionId}"));
            }
            catch (Exception e) {
                await ReplyAsync(e.Message);
            }
        }

        [RequirePermittedRole]
        [Command("insertmatch")]
        [Summary("Inserts a match **before** a specified match and recalculates the following matches.")]
        public async Task InsertMatchCommand([Summary("The match that will follow the match to be inserted.")] string id, [Summary("The first team.")] string team1, [Summary("The second team.")] string team2,
            [Summary("The result of a match. By default, the first team wins. Enter 0 for a draw.")] int result = 1) {

            // this code is to find the match to insert before

            var msg = await Program.DiscordIO.SendMessage("", 
                Context.Channel, 
                EmbedHelper.GenerateInfoEmbed($":arrows_counterclockwise: Finding the match **{id}**..."));

            var matchTuple = FindMatch(id);

            MatchAction match = matchTuple.Item1;
            int depth = matchTuple.Item2;

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
                    t1 = strToTeam(team1);
                    t2 = strToTeam(team2);
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
                await match.InsertBefore(newMatch);

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

            // this code is to find the match to insert before
            var msg = await Program.DiscordIO.SendMessage("", 
                Context.Channel, 
                EmbedHelper.GenerateInfoEmbed($":arrows_counterclockwise: Finding the match **{id}**..."));

            var matchTuple = FindMatch(id);

            MatchAction match = matchTuple.Item1;
            int depth = matchTuple.Item2 - 1;

            if (match == null) {
                await msg.DeleteAsync();
                await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed($"Could not find a match with the ID **{id}**."
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
                    t1 = strToTeam(team1);
                    t2 = strToTeam(team2);
                }
                catch (Exception e) {
                    await msg.DeleteAsync();
                    await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed(e.Message));
                    return;
                }

                var t1_s = string.Join(", ", t1.Players.Select(x => x.IGN));
                var t2_s = string.Join(", ", t2.Players.Select(x => x.IGN));

                // Edit the match and recalculate.
                match.Winner = t1;
                match.Loser = t2;
                match.IsDraw = result == 0;
                await match.ReCalculateSelf();

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

        [Command("deleterank")]
        [Summary("Removes a player rank. If you want ranks to be updated, you must run !refreshrank after this.")]
        public Task DeleteRank([Remainder][Summary("The exact name of the rank (capitalisation does not matter).")] string rankName) {

            var rank = Program.Config.Ranks.FirstOrDefault(r => r.Name.ToLower().Equals(rankName.ToLower()));

            Program.Config.Ranks.Remove(rank);

            Program.SerializeConfig();

            return ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed($"Removed the rank **{rankName}**"));
        }

        [Command("viewranks")]
        [Summary("Displays a list of all the rank and their boundaries.")]
        public async Task ViewRanks() {
            var sb = new StringBuilder();
            foreach (var r in Program.Config.Ranks) {
                sb.Append($"**{r.Name}** - {r.LowerBound}+{Environment.NewLine}");
            }

            await ReplyAsync("", false, EmbedHelper.GenerateInfoEmbed(sb.ToString()));
        }

        
        [Command("refreshrank", RunMode = RunMode.Async)]
        [Summary("Refreshes the rank of all players, or a specific list of players.")]
        public async Task RefreshRankCommand([Summary("A comma separated list of the players to update.")] string players = "all") {

            var channel = Context.Channel;

            var msg = await Program.DiscordIO.SendMessage("", channel, 
                EmbedHelper.GenerateInfoEmbed($":arrows_counterclockwise: Initialising..."));

            List<Player> playersList;

            if (players.Equals("all")) {
                playersList = Program.CurLeaderboard.Players;
            }
            else {
                playersList = new List<Player>();

                players = players.Replace(" ", "");
                var playersSpl = players.Split(',');

                foreach (var p in playersSpl) {
                    var player = Program.CurLeaderboard.FuzzySearch(p);
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

        [Command("undo")]
        [Summary("Undos a given number of matches.")]
        public async Task UndoCommand([Summary("The number of matches to undo (default is 1).")] int count = 1) {
            if (count == 0) {
                await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed("Couldn't undo any matches because you didn't tell me to undo any, nerd. :sunglasses:"));
                return;
            }

            var msg = await Program.DiscordIO.SendMessage("", 
                Context.Channel, 
                EmbedHelper.GenerateInfoEmbed($":arrows_counterclockwise: Initialising..."));

            StringBuilder sb = new StringBuilder();
            sb.Append($"**The following {(count == 1 ? "match was" : "matches were")} undone:**{Environment.NewLine}{Environment.NewLine}");
            for (int i = 0; i < count; ++i) {

                await Program.DiscordIO.EditMessage(msg, "",
                    EmbedHelper.GenerateInfoEmbed($":arrows_counterclockwise: Undoing match {i + 1} of {count}..."));

                var match = await Program.Controller.UndoAction();
                if (match == null) break;

                var winnerText = string.Join(", ", match.Winner.Players.Select(x => x.IGN));
                var loserText = string.Join(", ", match.Loser.Players.Select(x => x.IGN));
                sb.Append($"**{winnerText}** vs **{loserText}**{Environment.NewLine}");
            }

            await msg.DeleteAsync();
            await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed(sb.ToString()));
            
            // reset the pointer if the currently focused match has been affected
            if (count >= p_depth) await PtrResetCommand();
        }

        [Command("undo")]
        [Summary("Undos a specific match.")]
        public async Task UndoCommand([Summary("The ID of the match.")] string id) {

            // special case
            if (Program.Controller.LatestAction.ActionId.Equals(id)) {
                await UndoCommand(1);
                return;
            }

            var matchTuple = FindMatch(id);

            MatchAction match = matchTuple.Item1;

            if (match == null) {
                await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed($"Could not find a match with the ID **{id}**."
                    ));
                return;
            }

            var msg = await Program.DiscordIO.SendMessage("", 
                Context.Channel, 
                EmbedHelper.GenerateInfoEmbed($":arrows_counterclockwise: Undoing match and re-calculating subsequent matches... (this might take a while)"));

            await match.Undo();

            StringBuilder sb = new StringBuilder();
            sb.Append($"**The following match was undone:**{Environment.NewLine}{Environment.NewLine}");

            var winnerText = string.Join(", ", match.Winner.Players.Select(x => x.IGN));
            var loserText = string.Join(", ", match.Loser.Players.Select(x => x.IGN));
            sb.Append($"**{winnerText}** vs **{loserText}**{Environment.NewLine}{Environment.NewLine}");

            sb.Append($"**{matchTuple.Item2 - 1}** subsequent match{(matchTuple.Item2 == 2 ? " was" : "es were")} re-calculated.");
            
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
                await ReplyAsync("", false, EmbedHelper.GenerateInfoEmbed("There are no matches."));
                return;
            }

            var t1Str = string.Join(", ", p_match.Winner.Players.Select(p => p.IGN));
            var t2Str = string.Join(", ", p_match.Loser.Players.Select(p => p.IGN));

            if (!p_match.IsDraw) t1Str = "**" + t1Str + "**";

            await ReplyAsync("", false, EmbedHelper.GenerateInfoEmbed(
                "**The pointer is currently on the match:**" + Environment.NewLine + Environment.NewLine +
                t1Str + Environment.NewLine + t2Str, "ID: " + p_match.ActionId));
        }

        private static MatchAction p_match;
        private static int p_depth = 1;

        [Command("ptrreset")]
        [Summary("Resets the pointer to the latest match.")]
        public async Task PtrResetCommand() {
            p_match = Program.Controller.LatestAction;
            p_depth = 1;
            await ReplyAsync("", false, EmbedHelper.GenerateInfoEmbed(
                "**The pointer has been reset to the latest match.**"));
        }

        [Command("viewptr")]
        [Summary("Outputs the match currently focused on by the pointer.")]
        public async Task ViewPtrCommand() {
            await viewPtrPos();
        }

        [Command("ptrmv")]
        [Summary("Moves the pointer by a specified amount.")]
        public async Task PtrMoveCommand(
            [Summary("The amount of matches to move the pointer by. +ve values move it closer to the latest match.")] int delta
        ) {
            if (p_match == null) p_match = Program.Controller.LatestAction;
            if (p_match == null) {
                await ReplyAsync("", false, EmbedHelper.GenerateInfoEmbed("There are no matches."));
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
        public static Tuple<MatchAction, int> FindMatch(string id) {
            if (id.Equals(ptrIndicator)) return new Tuple<MatchAction, int>(p_match, p_depth);
            if (Program.Controller.LatestAction.ActionId.Equals(id)) 
                return new Tuple<MatchAction, int>(Program.Controller.LatestAction, 1);
            return Program.Controller.LatestAction.FindMatch(id);
        }

        // helpers

        public static Team strToTeam(string teamStr) {
            var split = teamStr.Split(',');
            var players = new Player[split.Length];

            for (int i = 0; i < split.Length; ++i) {
                var player = PlayerManagementCommands.FindPlayer(split[i]);
                if (player != null) {
                    players[i] = player;
                }
                else {
                    throw new Exception($"Could not find player \"{split[i]}\".");
                }
            }

            return new Team() { Players = players.ToList() };
        }
    }
}