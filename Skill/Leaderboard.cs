using System;
using System.Collections.Generic;

namespace OpenTrueskillBot.Skill
{
    public class Leaderboard
    {
        public List<Player> Players = new List<Player>();

        public Leaderboard() {

        }

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