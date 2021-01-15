using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OpenSkillBot.Skill
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

        // sorted by TS instead of ID, only use for output - don't binary search
        private List<Player> players_byTs = new List<Player>();

        public Leaderboard() {

        }

        public void AddPlayer(Player p) {

            // O(n) insertion rather than n log n
            for (int i = 0; i < Players.Count; ++i) {
                if (p.UUId.CompareTo(Players[i].UUId) >= 0) {
                    Players.Insert(i, p);
                    break;
                }
            }

            for (int i = 0; i < players_byTs.Count; ++i) {
                if (p.DisplayedSkill >= players_byTs[i].DisplayedSkill) {
                    players_byTs.Insert(i, p);
                    break;
                }
            }

            // Todo: implement a faster insertion algorithm
            // sortBoard();

            InvokeChange();

        }

        public void Initialize() {
            players_byTs = Players.Select(x => x).ToList();
            Players.Sort((x, y) => x.UUId.CompareTo(y.UUId));
            players_byTs.Sort((x, y) => y.DisplayedSkill.CompareTo(x.DisplayedSkill));
        }

        private void sortBoard() {
            Players.Sort((x, y) => x.UUId.CompareTo(x.UUId));
            players_byTs.Sort((x, y) => y.DisplayedSkill.CompareTo(x.DisplayedSkill));
        }
        
        public Player FindPlayer(string uuid) {

            // Binary search
            int min = 0;
            int max = Players.Count - 1; 
            while (min <= max) {  
                int mid = (min + max) / 2;  
                if (uuid.Equals(Players[mid].UUId)) {  
                    return Players[mid];
                }  
                else if (uuid.CompareTo(Players[mid].UUId) < 0) {  
                    max = mid - 1;  
                }  
                else {  
                    min = mid + 1;  
                }  
            }

            return null;
        }


        public event EventHandler LeaderboardChanged;
        public void InvokeChange(int changeCount = -1) {

            // code optimisations, choose the fastest out of quicksort or bubblesort
            var quickSortOps = BitOperations.Log2(Convert.ToUInt32(Players.Count + 1)) * Players.Count;
            var bubbleOps = changeCount * Players.Count;

            if (changeCount <= 0 || quickSortOps <= bubbleOps) {
                sortBoard();
            }
            else {
                // Bubble sort n times.
                changeCount = Math.Min(changeCount, Players.Count);

                for (int j = 0; j < changeCount; j++) {
                    for (int i = 0; i < Players.Count - 1; i++) {
                        if (Players[i].UUId.CompareTo(Players[i + 1].UUId) >= 0) {
                            var temp = Players[i + 1];
                            Players[i + 1] = Players[i];
                            Players[i] = temp;
                        }
                    }
                }

                // todo: make this a more general purpose function..
                for (int j = 0; j < changeCount; j++) {
                    for (int i = 0; i < players_byTs.Count - 1; i++) {
                        if (players_byTs[i].DisplayedSkill.CompareTo(players_byTs[i + 1].DisplayedSkill) < 0) {
                            var temp = players_byTs[i + 1];
                            players_byTs[i + 1] = players_byTs[i];
                            players_byTs[i] = temp;
                        }
                    }
                }

            }

            LeaderboardChanged?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Merges data stored in the OldPlayerData struct into the leaderboard.
        /// </summary>
        /// <param name="oldData">An IEnumerable of the old data.</param>
        internal void MergeOldData(IEnumerable<OldPlayerData> oldData) {
            foreach (var old in oldData) {

                Player found = FindPlayer(old.UUId);

                if (found == null) continue;

                found.Sigma = old.Sigma;
                found.Mu = old.Mu;
            }
        }

        public async Task Reset() {
            foreach (var player in Players) {
                player.Mu = Program.Config.DefaultMu;
                player.Sigma = Program.Config.DefaultSigma;
                await Task.Delay(200);
            }
            this.InvokeChange();
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
            foreach(var player in players_byTs) {
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