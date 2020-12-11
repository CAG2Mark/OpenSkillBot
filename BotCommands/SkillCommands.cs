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
            Program.Config.Ranks.OrderByDescending(r => r.LowerBound);

            return ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed($"Successfully added rank **{rankName}**"));
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