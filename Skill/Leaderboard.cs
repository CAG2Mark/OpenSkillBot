using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenTrueskillBot.Skill
{
    public class Rank {
        public Rank(int lowerBound, ulong roleId, string name) {
            this.LowerBound = lowerBound;
            this.RoleId = roleId;
            this.Name = name;
        }

        public int LowerBound { get; }
        public ulong RoleId { get; }
        public string Name { get; }

        // override object.Equals
        public override bool Equals(object obj)
        {
            //
            // See the full list of guidelines at
            //   http://go.microsoft.com/fwlink/?LinkID=85237
            // and also the guidance for operator== at
            //   http://go.microsoft.com/fwlink/?LinkId=85238
            //
            
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var rank = (Rank)obj;
            return this.LowerBound == rank.LowerBound && this.Name == rank.Name && this.RoleId == rank.RoleId;
        }
        
        // override object.GetHashCode
        public override int GetHashCode()
        {
            return this.LowerBound.GetHashCode() * 17 + this.RoleId.GetHashCode() * 7 + this.Name.GetHashCode();
        }
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
            sortBoard();

            InvokeChange();

        }

        private void sortBoard() {
            Players.Sort((x, y) => y.DisplayedSkill.CompareTo(x.DisplayedSkill));
        }

        public event EventHandler LeaderboardChanged;
        public void InvokeChange() {
            sortBoard();
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

        /// <summary>
        /// Fuzzy searches for a player.
        /// </summary>
        /// <param name="query">The player to search for.</param>
        /// <returns>The player that was found. Will return null if not found.</returns>
        public Player FuzzySearch(string query) {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 3) return null;

            query = query.ToLower();
            var player = Players.Find(p => {
                string name = p.IGN.Substring(0, Math.Min(query.Length, p.IGN.Length)).ToLower();
                return query.Equals(name);
            });
            if (player != null) return player;
            else return Players.Find(p => {
                string alias = null;
                if (!string.IsNullOrWhiteSpace(p.Alias))
                    alias = p.Alias.Substring(0, Math.Min(query.Length, p.Alias.Length)).ToLower(); 
                return query.Equals(alias);
            });
        }

        // returns the leaderboard string, but splits it so that each string is less than a specified limit
        public IEnumerable<string> GenerateLeaderboardText(int charLimit) {
            var nl = Environment.NewLine;
            var sb = new StringBuilder();

            int p = 0;
            foreach(var player in Players) {
                var nextStr = "";

                // print ranks in the correct position
                // looks to be O(n^2) but is actually O(n)
                var ranks = Program.Config.Ranks;
                for (int i = p; i < ranks.Count; ++i) {
                    if (i == 0 || ranks[i - 1].LowerBound > player.DisplayedSkill) {
                        nextStr += "**" + ranks[i].Name + "**" + nl;
                        p = i + 1;
                    }
                }
                
                nextStr += $"{player.IGN}: {Math.Round(player.DisplayedSkill).ToString("#")} RD {Math.Round(player.Sigma).ToString("#")}{nl}";

                if (sb.Length + nextStr.Length > charLimit) {
                    yield return sb.ToString();
                    sb = new StringBuilder();
                }

                sb.Append(nextStr);
            }

            yield return sb.ToString();

        }
        
    }
}