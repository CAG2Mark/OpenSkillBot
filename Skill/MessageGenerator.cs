using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord;

namespace OpenSkillBot.Skill
{
    public static class MessageGenerator
    {
        private static int r(double f) {
            return (int)Math.Round(f, 0);
        }

        public static string MatchDeltaGenerator(IEnumerable<OldPlayerData> oldPlayerDatas, IEnumerable<OldPlayerData> newPlayerData) {
            var sb = new StringBuilder();
            foreach (var old in oldPlayerDatas) {
                var newMatch = newPlayerData.FirstOrDefault(n => n.UUId.Equals(old.UUId));
                var player = Program.CurLeaderboard.FindPlayer(old.UUId);
                if (newMatch == null || player == null) return null;

                var tsDelta = r(DisplayedSkill(newMatch) - DisplayedSkill(old));
                var sigmaDelta = newMatch.Sigma - old.Sigma;

                string tsDelta_s = (tsDelta < 0 ? "" : "+") + r(tsDelta);
                string sigmaDelta_s = (sigmaDelta < 0 ? "" : "+") + r(sigmaDelta);

                sb.Append($"**{player.IGN} {tsDelta_s}, {sigmaDelta_s}** (*{r(DisplayedSkill(old))} RD {r(old.Sigma)}* → *{r(DisplayedSkill(newMatch))} RD {r(newMatch.Sigma)}*)");
                sb.Append(Environment.NewLine);
            }
            return sb.ToString();
        }

        public static double DisplayedSkill(OldPlayerData data) {
            return data.Mu - Program.Config.TrueSkillDeviations * data.Sigma;
        }

        public static Embed MakeMatchMessage(MatchAction action) {
            var winner = action.Winner;
            var loser = action.Loser;

            string w_s = string.Join(", ", winner.Players.Select(x => x.IGN));
            string l_s = string.Join(", ", loser.Players.Select(x => x.IGN));

            StringBuilder deltas = new StringBuilder();
            StringBuilder rankChanges = new StringBuilder();

            // Skill and rank changes
            foreach(var o in action.OldPlayerDatas) {
                var uuid = o.UUId;

                var oldRank = Player.GetRank(o.Mu, o.Sigma);
                var oldTs = r(o.Mu - o.Sigma * Program.Config.TrueSkillDeviations);

                var temp = winner.Players.FirstOrDefault(o => o.UUId == uuid);
                var player = temp == null ? loser.Players.First(o => o.UUId == uuid) : temp;

                var newRank = Player.GetRank(player.DisplayedSkill);

                int tsDelta = r(player.DisplayedSkill) - oldTs;
                int rdDelta = (r(player.Sigma) - r(o.Sigma));

                deltas.Append(
                    $"{player.IGN} **{(tsDelta < 0 ? "" : "+")}{tsDelta}, {(rdDelta < 0 ? "" : "+")}{rdDelta}** " + 
                    $"(_{r(oldTs)} RD {r(o.Sigma)}_ → _{r(player.DisplayedSkill)} RD {r(player.Sigma)}_){Environment.NewLine}"
                );

                // logically this works
                if (oldRank == null && newRank == null) continue;
                if ((oldRank == null && newRank != null) || !oldRank.Equals(newRank)) {          
                    rankChanges.Append($"**{player.IGN}**: _{(oldRank == null ? "None" : "" + oldRank.Name)}_ → _{(newRank == null ? "None" : "" + newRank.Name)}_{Environment.NewLine}");
                }
            }
            

            EmbedBuilder embed = new EmbedBuilder()
                .WithTimestamp(action.ActionTime)
                .WithColor(action.IsTourney ? Discord.Color.Purple : Discord.Color.Blue)
                .WithFooter("ID: " + action.ActionId);

            embed.Title = w_s + " vs " + l_s;

            embed.AddField($"Winner{(winner.Players.Count() == 1 ? "" : "s")}:", action.IsDraw ? "The match ended in a draw" : w_s);
            embed.AddField("Skill Changes", deltas);

            if (!string.IsNullOrWhiteSpace(rankChanges.ToString())) {
                embed.AddField("Rank Changes", rankChanges);
            }

            return embed.Build();
        }
    }
}