using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenTrueskillBot.Skill
{
    public struct Rank {
        public Rank(int lowerBound, ulong roleId, string name) {
            this.LowerBound = lowerBound;
            this.RoleId = roleId;
            this.Name = name;
        }

        public int LowerBound { get; }
        public ulong RoleId { get; }
        public string Name { get; }
    }

    public class Leaderboard
    {

        /// <summary>
        /// All the players on the leaderboard.
        /// </summary>
        public List<Player> Players { get; set; } = new List<Player>();

        public Leaderboard() {

        }

        public void AddPlayer(Player p) {

            Players.Add(p);
            // Todo: implement a faster insertion algorithm
            Players.OrderByDescending(p => p.DisplayedSkill);

            InvokeChange();

        }

        public event EventHandler LeaderboardChanged;
        public void InvokeChange() {
            LeaderboardChanged?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Merges data stored in the OldPlayerData struct into the leaderboard.
        /// </summary>
        /// <param name="oldData">An IEnumerable of the old data.</param>
        public void MergeOldData(IEnumerable<OldPlayerData> oldData) {
            foreach (var old in oldData)
            {
                var found = Players.Find(p => p.UUId.Equals(old.UUId));
                if (found == null) continue;

                found.Sigma = old.Sigma;
                found.Mu = old.Mu;
            }
        }

        public Player FuzzySearch(string query) {
            query = query.ToLower();
            return Players.Find(p => {
                string name = p.IGN.Substring(0, Math.Min(query.Length, p.IGN.Length)).ToLower();
                string alias = null;
                if (p.Alias != null)
                    p.Alias.Substring(0, query.Length);

                Console.WriteLine(query);
                Console.WriteLine(name);
                
                return query.Equals(name) || query.Equals(alias);
            });
        }

        public string GenerateLeaderboardText() {
            var nl = Environment.NewLine;
            var sb = new StringBuilder();

            foreach(var player in Players) {
                sb.Append($"{player.IGN}: {Math.Round(player.DisplayedSkill).ToString("#")} RD {Math.Round(player.Sigma).ToString("#")}{nl}");
            }

            return sb.ToString();
        }
        
    }
}