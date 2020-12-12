using Discord.Commands;
using Discord;
using System;
using System.Threading.Tasks;
using OpenTrueskillBot.Skill;
using System.Linq;

namespace OpenTrueskillBot.BotCommands
{
    [Name("Match Commands")]
    public class SkillCommands : ModuleBase<SocketCommandContext> {
        [Command("fullmatch")]
        [Alias(new string[] { "fm" })]
        [Summary("Calculates a full match between two teams.")]
        public Task FullMatchCommand([Summary("The first team.")] string team1, [Summary("The second team.")] string team2,
            [Summary("The result of a match. By default, the first team wins. Enter 0 for a draw.")] int result = 1) {

            try {
                var t1 = strToTeam(team1);
                var t2 = strToTeam(team2);

                var t1_s = string.Join(", ", t1.Players.Select(x => x.IGN));
                var t2_s = string.Join(", ", t2.Players.Select(x => x.IGN));

                Program.Controller.AddMatchAction(t1, t2, result);

                string output = $"The match between {t1_s} and {t2_s} has been calculated.";

                return ReplyAsync(output);
            }
            catch (Exception e) {
                return ReplyAsync(e.Message);
            }
        }

        [Command("addrank")]
        [Summary("Adds a new player rank.")]
        public Task AddRankCommand([Summary("The minimum skill to be in this rank.")] int lowerBound, 
            [Summary("The role ID that people in this rank should be assigned")] ulong roleId,
            [Remainder][Summary("The name of the rank.")] string rankName) {

            var rank = new Rank(lowerBound, roleId, rankName);

            Program.Config.Ranks.Add(rank);
            Program.Config.Ranks.Sort((x, y) => y.LowerBound.CompareTo(x.LowerBound));

            Program.SerializeConfig();

            return ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed($"Successfully added rank **{rankName}**"));
        }

        [Command("refreshrank")]
        [Summary("Refreshes the rank of all players.")]
        public async Task RefreshRankCommand() {

            var channel = Context.Channel;

            var total = Program.CurLeaderboard.Players.Count;

            var msg = await Program.DiscordIO.SendMessage("", channel, 
                EmbedHelper.GenerateInfoEmbed($":arrows_counterclockwise: Initialising..."));

            int i = 0;
            foreach (var p in Program.CurLeaderboard.Players) {
                var percent = ++i * 100 / total;
                await Program.DiscordIO.EditMessage(msg, "", 
                    EmbedHelper.GenerateInfoEmbed(
                        $":arrows_counterclockwise: Processing {p.IGN} {Environment.NewLine}{Environment.NewLine} {i} of {total} players processed ({percent}%)"));
                await p.UpdateRank(true);
            }

            await msg.DeleteAsync();
            
            await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed($"Refreshed the ranks of {Program.CurLeaderboard.Players.Count} players."));
        }

        // helpers

        public Team strToTeam(string teamStr) {
            var split = teamStr.Split(',');
            var players = new Player[split.Length];

            for (int i = 0; i < split.Length; ++i) {
                var player = Program.CurLeaderboard.FuzzySearch(split[i]);
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