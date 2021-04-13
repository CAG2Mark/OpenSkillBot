using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord;
using OpenSkillBot.BotCommands;

namespace OpenSkillBot.Skill
{
    public static class MessageGenerator
    {
        private static double r(double f) {
            return Math.Round(f, Program.Config.SkillDecimalPlaces);
        }

        private static string s(double f) {
            if (Program.Config.SkillDecimalPlaces == 0) return f.ToString("0");
             
            return f.ToString($"0.{"".PadLeft(Program.Config.SkillDecimalPlaces, '0')}");
        }

        public static (string SkillChanges, string RankChanges) MatchDeltaGenerator(IEnumerable<OldPlayerData> oldPlayerDatas, IEnumerable<OldPlayerData> newPlayerData) {
            var sb = new StringBuilder();
            var r_sb = new StringBuilder();
            
            foreach (var old in oldPlayerDatas) {
                var newMatch = newPlayerData.FirstOrDefault(n => n.UUId.Equals(old.UUId));
                var player = Program.CurLeaderboard.FindPlayer(old.UUId);
                if (newMatch == null || player == null) throw new Exception("Could not match players when generating skill deltas.");

                var oldRank = Player.GetRank(old.Mu, old.Sigma);
                var newRank = Player.GetRank(newMatch.Mu, newMatch.Sigma);

                // logically this works
                if (oldRank == null && newRank == null) continue;
                if ((oldRank == null && newRank != null) || !oldRank.Equals(newRank)) {          
                    r_sb.Append($"{player.IGN}: *{(oldRank == null ? "None" : "" + oldRank.Name)}* → *{(newRank == null ? "None" : "" + newRank.Name)}*{Environment.NewLine}");
                }

                var displayedNew = r(DisplayedSkill(newMatch));
                var displayedOld = r(DisplayedSkill(old));

                var sigmaNew = r(newMatch.Sigma);
                var sigmaOld = r(old.Sigma);

                double tsDelta = displayedNew - displayedOld;
                double sigmaDelta = sigmaNew - sigmaOld;

                string tsDelta_s = (tsDelta < 0 ? "" : "+") + s(tsDelta);
                string sigmaDelta_s = (sigmaDelta < 0 ? "" : "+") + s(sigmaDelta);

                sb.Append($"{player.IGN} **{tsDelta_s}, {sigmaDelta_s}** (*{s(displayedOld)} RD {s(sigmaOld)}* → *{s(displayedNew)} RD {s(sigmaNew)}*)");
                sb.Append(Environment.NewLine);
            }
            return (sb.ToString(), r_sb.ToString());
        }

        public static double DisplayedSkill(OldPlayerData data) {
            return data.Mu - Program.Config.TrueSkillDeviations * data.Sigma;
        }

        public static Embed MakeMatchMessage(MatchAction action) {
            var winner = action.Winner;
            var loser = action.Loser;

            string w_s = string.Join(", ", winner.Players.Select(x => x.IGN));
            string l_s = string.Join(", ", loser.Players.Select(x => x.IGN));

            var changes = MatchDeltaGenerator(
                action.OldPlayerDatas, 
                SkillCommands.ToOldPlayerData(new Team[] {action.Winner, action.Loser}));
            

            EmbedBuilder embed = new EmbedBuilder()
                .WithTimestamp(action.ActionTime)
                .WithColor(action.IsTourney ? Discord.Color.Purple : Discord.Color.Blue)
                .WithFooter("ID: " + action.ActionId);

            embed.Title = w_s + " vs " + l_s;

            embed.AddField($"Winner{(winner.Players.Count() == 1 ? "" : "s")}:", action.IsDraw ? "The match ended in a draw" : w_s);
            embed.AddField("Skill Changes", changes.SkillChanges);

            if (!string.IsNullOrWhiteSpace(changes.RankChanges)) {
                embed.AddField("Rank Changes", changes.RankChanges);
            }

            return embed.Build();
        }
    }
}