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

                await Program.Controller.AddMatchAction(t1, t2, result);

                string output = $"The match between **{t1_s}** and **{t2_s}** has been calculated.";

                await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed(output));
            }
            catch (Exception e) {
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


        [Command("refreshrank")]
        [Summary("Refreshes the rank of all players, or a specific list of players.")]
        public async Task RefreshRankCommand([Summary("A comma separated list of the players to update.")] string players) {

            var channel = Context.Channel;

            var msg = await Program.DiscordIO.SendMessage("", channel, 
                EmbedHelper.GenerateInfoEmbed($":arrows_counterclockwise: Initialising..."));

            List<Player> playersList;

            if (string.IsNullOrEmpty(players)) {
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
                await Task.Delay(100);
            }

            await msg.DeleteAsync();

            Program.CurLeaderboard.InvokeChange();
            
            await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed($"Refreshed the ranks of {playersList.Count} players."));
        }



        [Command("resetleaderboard")]
        [Summary("Resets the leaderboard.")]
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

            StringBuilder sb = new StringBuilder();
            sb.Append($"**The following {(count == 1 ? "match was" : "matches were")} undone:**{Environment.NewLine}{Environment.NewLine}");
            for (int i = 0; i < count; ++i) {
                var match = await Program.Controller.UndoAction();
                var winnerText = string.Join(", ", match.Winner.Players.Select(x => x.IGN));
                var loserText = string.Join(", ", match.Loser.Players.Select(x => x.IGN));
                sb.Append($"**{winnerText}** vs **{loserText}**{Environment.NewLine}");
            }
            
            await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed(sb.ToString()));
        }

        [Command("undo")]
        [Summary("Undos a specific match.")]
        public async Task UndoCommand([Summary("The ID of the match.")] string id) {

            // special case
            if (Program.Controller.LatestAction.ActionId.Equals(id)) {
                await UndoCommand(1);
                return;
            }

            // find the match
            var matchTuple = Program.Controller.LatestAction.FindMatch(id);
            var match = matchTuple.Item1;
            await match.Undo();

            StringBuilder sb = new StringBuilder();
            sb.Append($"**The following match was undone:**{Environment.NewLine}{Environment.NewLine}");

            var winnerText = string.Join(", ", match.Winner.Players.Select(x => x.IGN));
            var loserText = string.Join(", ", match.Loser.Players.Select(x => x.IGN));
            sb.Append($"**{winnerText}** vs **{loserText}**{Environment.NewLine}{Environment.NewLine}");

            sb.Append($"**{matchTuple.Item2 - 1}** subsequent match{(matchTuple.Item2 == 2 ? "" : "es")} were re-calculated.");
            
            await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed(sb.ToString()));
        }

        // helpers

        public Team strToTeam(string teamStr) {
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