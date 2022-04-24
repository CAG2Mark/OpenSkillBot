using OpenSkillBot.Skill;
using Newtonsoft.Json;
using System;

namespace OpenSkillBot.Serialization
{

    // dirty hack to help maintain referential integrity when serializing using multiple files
    public class ActionContainer : IComparable {

        public ActionContainer() {}

        public ActionContainer(BotAction b) {
            this.Action = b;
        }

        private BotAction action = null;

        [JsonIgnore]
        public BotAction Action {
            get {
                if (action == null && actionId != null) {
                    var test = Program.Controller.MatchHash;
                    if (Program.Controller.MatchHash.ContainsKey(actionId)) action = Program.Controller.MatchHash[actionId];
                }
                return action;
            }
            set {
                this.action = value;
                if (action != null) this.actionId = value.ActionId;
                else this.actionId = null;
            }
        }

        [JsonProperty]
        private string actionId { get; set; }

        public int CompareTo(object obj)
        {
            return ((IComparable)Action).CompareTo(((ActionContainer)(obj)).Action);
        }
    }
}