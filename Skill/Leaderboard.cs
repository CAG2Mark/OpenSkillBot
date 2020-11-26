using System;
using System.Collections.Generic;

namespace OpenTrueskillBot.Skill
{
    public class Leaderboard
    {

        /// <summary>
        /// All the players on the leaderboard.
        /// </summary>
        public List<Player> Players { get; set; } = new List<Player>();

        public Leaderboard() {

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
        
    }
}