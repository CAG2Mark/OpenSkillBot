using System;
using System.Linq;
using System.Text;
using Discord;

namespace OpenTrueskillBot.Skill
{
    public static class MessageGenerator
    {
        private static int r(double f) {
            return (int)Math.Round(f, 0);
        }

        public static Embed MakeMatchMessage(MatchAction action, bool isDraw) {
            var winner = action.Winner;
            var loser = action.Loser;

            string w_s = string.Join(", ", winner.Players.Select(x => x.IGN));
            string l_s = string.Join(", ", loser.Players.Select(x => x.IGN));

            StringBuilder deltas = new StringBuilder();

            foreach(var o in action.oldPlayerDatas) {
                var uuid = o.UUId;
                var oldTs = r(o.Mu - o.Sigma * Program.Config.TrueSkillDeviations);

                var temp = winner.Players.FirstOrDefault(o => o.UUId == uuid);
                var player = temp == null ? loser.Players.First(o => o.UUId == uuid) : temp;

                int tsDelta = r(player.DisplayedSkill) - oldTs;
                int rdDelta = (r(player.Sigma) - r(o.Sigma));

                deltas.Append(
                    $"{player.IGN} **{(tsDelta < 0 ? "" : "+")}{tsDelta}, {(rdDelta < 0 ? "" : "+")}{rdDelta}** " + 
                    $"(_{r(oldTs)} RD {r(o.Sigma)}_ → _{r(player.DisplayedSkill)} RD {r(player.Sigma)}_){Environment.NewLine}"
                );
            }

            EmbedBuilder embed = new EmbedBuilder();

            embed.Title = w_s + " vs " + l_s;

            embed.AddField($"Winner{(winner.Players.Count == 1 ? "" : "s")}:", isDraw ? "The match ended in a draw" : w_s);
            embed.AddField("Skill Changes", deltas);

            embed.Color = new Color(69, 128, 237);

            return embed.Build();
        }
    }
}