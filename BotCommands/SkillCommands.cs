using Discord.Commands;
using Discord;
using System;
using System.Threading.Tasks;
using OpenTrueskillBot.Skill;

namespace OpenTrueskillBot.BotCommands
{
    [Name("Match Commands")]
    public class SkillCommands : ModuleBase<SocketCommandContext>
    {
        [Command("fullmatch")]
        [Alias(new string[] {"fm"})]
        [Summary("Calculates a full match between two teams.")]
        public Task FullMatchCommand(string team1, string team2, int result = 1) {

                var t1 = strToTeam(team1);
                var t2 = strToTeam(team2);

                Program.Controller.AddMatchAction(t1, t2, result);

                string output = "New Ratings:" + Environment.NewLine;

                // temporary
                foreach (var player in t1.Players)
                {
                    output += $"{player.IGN}: {player.DisplayedSkill.ToString("#.#")} RD {player.Sigma.ToString("#.#")}{Environment.NewLine}";
                }
                foreach (var player in t2.Players)
                {
                    output += $"{player.IGN}: {player.DisplayedSkill.ToString("#.#")} RD {player.Sigma.ToString("#.#")}{Environment.NewLine}";
                }
                
                return ReplyAsync(output);

            try {

            }
            catch (Exception e) {
                return ReplyAsync(e.Message);
            }

        }

        public Team strToTeam(string teamStr) {
            var split = teamStr.Split(',');
            var players = new Player[teamStr.Length];

            for (int i = 0; i < split.Length; ++i) {
                var player = Program.CurLeaderboard.FuzzySearch(split[i]);
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