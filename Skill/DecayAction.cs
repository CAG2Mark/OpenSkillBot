using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Newtonsoft.Json;
using OpenSkillBot.Serialization;

namespace OpenSkillBot.Skill
{
    public class DecayAction : BotAction
    {

        [JsonProperty]
        private List<string> decayedPlayerUUIDs { get; set; }
        private List<Player> decayedPlayers;

        [JsonIgnore]
        public IEnumerable<Player> DecayedPlayers {
            get {
                if (decayedPlayers == null) {
                    decayedPlayers = MatchAction.UUIDListToPlayers(decayedPlayerUUIDs);
                }
                // copy
                return decayedPlayers.ToList();
            }
            set {
                this.decayedPlayers = value.ToList();
                this.decayedPlayerUUIDs = value.Select(p => p.UUId).ToList();
            }
        }

        public DecayAction() {}

        public DecayAction(IEnumerable<Player> players = null) : base() {
            this.DecayedPlayers = players;
        }

        protected override async Task action()
        {
            foreach (var p in DecayedPlayers) {
                var newSkill = SkillWrapper.Decay(p);
                p.Mu = newSkill.Mu;
                p.Sigma = newSkill.Sigma;
                p.LastDecay = p.DecayCycle;
            }
        }

        protected override void addToPlayerActions()
        {
            // Don't add decay actions to the player's action history.

            /*
            foreach (var p in this.DecayedPlayers) {
                p.Actions.Insert(p => p.Action.Equals(this), new ActionContainer(this));
            }
            */
        }

        protected async override Task deleteMessage()
        {
            if (Program.Config.HistoryChannelId == 0 || this.discordMessageId == 0) return;
            // delete message
            var msg = (RestUserMessage)await Program.DiscordIO.GetMessage(this.discordMessageId, Program.Config.HistoryChannelId);
            if (msg != null)
            {
                this.discordMessageId = 0;
                await msg.DeleteAsync();
            }
        }

        protected override int getChangeCount()
        {
            return this.DecayedPlayers.Count();
        }

        protected override void removeFromPlayerActions()
        {
            foreach (var p in this.DecayedPlayers) {
                p.Actions.Delete(p => p.Action.Equals(this));
            }
        }

        protected override async Task sendMessage()
        {
            if (Program.Config.HistoryChannelId == 0) return;

            // generate message
            var embed = GenerateEmbed();
            var chnl = Program.Config.GetHistoryChannel();
            if (this.discordMessageId == 0)
            {
                this.discordMessageId = (await Program.DiscordIO.SendMessage("", chnl, embed)).Id;
            }
            else
            {
                var msg = (RestUserMessage)await chnl.GetMessageAsync(this.discordMessageId);

                await Program.DiscordIO.EditMessage(msg, "", embed);
            }
        }

        protected override void setOldPlayerDatas()
        {
            OldPlayerDatas = GetPlayerDatas();
        }

        protected override void undoAction()
        {
        }

        public List<OldPlayerData> GetPlayerDatas() {
            var data = new List<OldPlayerData>();

            foreach (var p in this.DecayedPlayers)
            {
                data.Add(new OldPlayerData() { Sigma = p.Sigma, Mu = p.Mu, UUId = p.UUId, DecayCycle = p.DecayCycle, LastDecay = p.LastDecay });
            }

            return data;
        }

        public Embed GenerateEmbed() {
            var eb = new EmbedBuilder()
                .WithColor(Discord.Color.Blue)
                .WithTitle("Rank Decays")
                .WithCurrentTimestamp()
                .WithFooter("ID: " + this.ActionId);

            var text = MessageGenerator.MatchDeltaGenerator(
                this.OldPlayerDatas, this.GetPlayerDatas());

            eb.AddField("Decays", text.SkillChanges);

            if (!string.IsNullOrEmpty(text.RankChanges)) eb.AddField("Rank Changes", text.RankChanges);

            return eb.Build();
        }

        public static IEnumerable<Player> GetPlayersToDecay() {
            foreach (var player in Program.CurLeaderboard.Players) {
                if (player.DecayCycle != 0 &&
                    (player.LastDecay - player.DecayCycle) / Program.Config.DecayCyclesUntilDecay != 0 &&
                    player.DecayCycle % Program.Config.DecayCyclesUntilDecay == 0 &&
                    player.DisplayedSkill > Program.Config.DecayThreshold) yield return player;
            }
        }

        public override string ToString()
        {
            return $"Decay {this.DecayedPlayers.Count()} player(s)";
        }
    }
}