using System;
using Newtonsoft.Json;
using OpenSkillBot.Tournaments;

namespace OpenSkillBot.Serialization
{
    public class TourneyContainer : IComparable
    {

        public TourneyContainer() {}

        public TourneyContainer(Tournament t) {
            this.Tournament = t;
        }

        private Tournament tournament = null;

        [JsonIgnore]
        public Tournament Tournament {
            get {
                if (tournament == null && tourneyId != null) {
                    if (Program.Controller.Tourneys.CompletedTournaments.ContainsKey(tourneyId)) this.tournament = Program.Controller.Tourneys.CompletedTournaments[tourneyId];
                }
                return tournament;
            }
            set {
                this.tournament = value;
                if (tournament != null) this.tourneyId = value.Id;
                else this.tourneyId = null;
            }
        }

        [JsonProperty]
        private string tourneyId { get; set; }

        public int CompareTo(object obj)
        {
            return ((IComparable)Tournament).CompareTo(((TourneyContainer)obj).Tournament);
        }
    }
}