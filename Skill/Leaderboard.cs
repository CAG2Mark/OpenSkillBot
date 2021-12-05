using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace OpenSkillBot.Skill
{
    public class Leaderboard
    {


        [JsonProperty("Players")]
        // for backwards compability, not used!
        private List<Player> playersObselete { get; set; } = null;

        /// <summary>
        /// All the players on the leaderboard.
        /// </summary>
        public Dictionary<string, Player> PlayersDic { get; set; } = new Dictionary<string, Player>();

        // sorted by TS instead of ID, only use for output - don't binary search
        private List<Player> players_byTs = new List<Player>();

        public Nullable<(string Name, ulong DiscordID)> LatestJoinedPlayer { get; set; } = null;

        public Leaderboard() {

        }

        public ICollection<Player> GetPlayerCollection() {
            return PlayersDic.Values;
        }

        public void AddPlayer(Player p) {

            if (PlayersDic.ContainsKey(p.UUId)) PlayersDic[p.UUId] = p;
            else PlayersDic.Add(p.UUId, p);

            // O(n) insertion into the player TS list
            bool added = false;
            for (int i = 0; i < players_byTs.Count; ++i) {
                if (p.DisplayedSkill >= players_byTs[i].DisplayedSkill) {
                    players_byTs.Insert(i, p);
                    added = true;
                    break;
                }
            }
            if (!added) {
                players_byTs.Add(p);
            }

            // Insert into dictionary for fast searching of player by discord ID

            InvokeChange();

        }

        public void RemovePlayer(Player p) {
            PlayersDic.Remove(p.UUId);
            players_byTs.Remove(p);

            InvokeChange();
        }

        public void Initialize() {
            // backwards compatibility for older versions still using binary search
            if (playersObselete != null) {
                foreach (var p in playersObselete) {
                    PlayersDic.TryAdd(p.UUId, p);
                }
            }

            players_byTs = GetPlayerCollection().Select(x => x).ToList();
            players_byTs.Sort((x, y) => y.DisplayedSkill.CompareTo(x.DisplayedSkill));
        }

        private void sortBoard() {
            players_byTs.Sort((x, y) => y.DisplayedSkill.CompareTo(x.DisplayedSkill));
        }

        public Player FindPlayer(ulong discordId) {
            return GetPlayerCollection().FirstOrDefault(p => p.DiscordId == discordId);
        }
        
        public Player FindPlayer(string uuid) {
            return PlayersDic.ContainsKey(uuid) ? PlayersDic[uuid] : null;
        }


        public event EventHandler LeaderboardChanged;
        public void InvokeChange(int changeCount = -1) {

            // code optimisations, choose the fastest out of quicksort or bubblesort
            var quickSortOps = BitOperations.Log2(Convert.ToUInt32(PlayersDic.Count + 1)) * PlayersDic.Count;
            var bubbleOps = changeCount * PlayersDic.Count;

            if (changeCount <= 0 || quickSortOps <= bubbleOps) {
                sortBoard();
            }
            else {
                // bubble sort n times 
                // todo: make this a more general purpose function..
                for (int j = 0; j < changeCount; j++) {
                    for (int i = players_byTs.Count - j - 1; i > 0; --i) {
                        if (players_byTs[i].DisplayedSkill > players_byTs[i - 1].DisplayedSkill) {
                            var temp = players_byTs[i - 1];
                            players_byTs[i - 1] = players_byTs[i];
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
                found.DecayCycle = old.DecayCycle;
                found.LastDecay = old.LastDecay;
            }
        }

        public async Task Reset() {
            var players = GetPlayerCollection();

            PlayersDic = new Dictionary<string, Player>();
            players_byTs = new List<Player>();

            foreach (var player in players) {
                if (player.MarkedForDeletion) continue;

                player.Mu = Program.Config.DefaultMu;
                player.Sigma = Program.Config.DefaultSigma;
                player.TournamentsMissed = 0;
                player.DecayCycle = 0;
                player.LastDecay = 0;
                player.Tournaments = new PriorityQueue<Serialization.TourneyContainer>(true);
                player.Actions = new PriorityQueue<Serialization.ActionContainer>(true);

                PlayersDic.TryAdd(player.UUId, player);
                players_byTs.Add(player);

                await Task.Delay(1000);
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

            var players = GetPlayerCollection().ToList();

            var nQuery = query.ToLower();
            // first search based on ign, discord ID or UUID
            var player = players.Find(p => {
                string name = p.IGN.Substring(0, Math.Min(query.Length, p.IGN.Length)).ToLower();
                return (nQuery.Equals(name) || query.Equals(p.DiscordId.ToString()) || query.Equals(p.UUId.ToString())) && !p.MarkedForDeletion;
            });
            if (player != null) return player;
            else return players.Find(p => {
                string alias = null;
                if (!string.IsNullOrWhiteSpace(p.Alias))
                    alias = p.Alias.Substring(0, Math.Min(query.Length, p.Alias.Length)).ToLower(); 
                return nQuery.Equals(alias) && !p.MarkedForDeletion; // do not return players marked for deletion
            });
        }

        private static string s(double f) {
            if (Program.Config.SkillDecimalPlaces == 0) return f.ToString("0");
             
            return f.ToString($"0.{"".PadLeft(Program.Config.SkillDecimalPlaces, '0')}");
        }

        // returns the leaderboard string, but splits it so that each string is less than a specified limit
        public IEnumerable<string> GenerateLeaderboardText(int charLimit) {
            var nl = Environment.NewLine;
            var sb = new StringBuilder();

            bool inUnrankedRegion = false;

            var copy = players_byTs.ToList();

            var ogCnt = copy.Count;

            int p = 0;
            for (int j = 0; j < copy.Count; ++j) {

                var player = copy[j];

                // do not display players marked for deletion on the leaderboard
                if (player.MarkedForDeletion) continue;

                inUnrankedRegion = j >= ogCnt;

                var nextStr = "";

                // print ranks in the correct position
                var ranks = Program.Config.Ranks;
                for (int i = p; i < ranks.Count; ++i) {
                    if (i == 0 || ranks[i - 1].CompareTo(player.PlayerRank) > 0) {
                        nextStr += "**" + ranks[i].Name + "**" + nl;
                        p = i + 1;
                    }
                }

                // ignore unranked. push to the end
                if (player.IsUnranked && !inUnrankedRegion) {
                    copy.Add(player);
                }
                else {
                    // unranked title
                    if (j == ogCnt) nextStr += $"**{Rank.GetUnrankedRank().Name}**{nl}";
                    
                    nextStr += $"{player.IGN}: {s(player.DisplayedSkill)} RD {s(player.Sigma)}{nl}";
                }

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