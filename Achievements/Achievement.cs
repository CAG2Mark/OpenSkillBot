using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Newtonsoft.Json;
using OpenSkillBot.Skill;

namespace OpenSkillBot.Achievements
{
    /// <summary>
    /// Contains information about achievements.
    /// </summary>
    public class Achievement
    {
        [JsonProperty]
        /// <summary>
        /// The name of the achievement.
        /// </summary>
        /// <value></value>
        public string Name { get; private set; }

        [JsonProperty]
        /// <summary>
        /// The description of this achievement.
        /// </summary>
        /// <value></value>
        public string Description { get; private set; }

        [JsonProperty]
        /// <summary>
        /// The ID of the achievement.
        /// </summary>
        /// <value></value>
        public string Id { get; private set;}

        // player list

        [JsonProperty]
        private List<string> playerUUIDs = new List<string>();

        private List<Player> players;
        [JsonIgnore]
        /// <summary>
        /// The list of players who have this achievement.
        /// </summary>
        /// <value></value>
        public List<Player> Players {
            get {
                if (players == null) players = MatchAction.UUIDListToPlayers(playerUUIDs).ToList();
                return players;
            }
        }

        #region Discord message

        /// <summary>
        /// The achievements channel.
        /// </summary>
        /// <returns></returns>
        public static ISocketMessageChannel AchvsChannel => Program.DiscordIO.GetChannel(Program.Config.AchievementsChannelId);

        [JsonProperty]
        internal ulong DiscordMsgId { get; set; }

        private async Task<RestUserMessage> getMessage() {
            return this.DiscordMsgId == 0 ? null : (RestUserMessage)(await Program.DiscordIO.GetMessage(this.DiscordMsgId, AchvsChannel));
        }

        public async Task SendMessage(bool serialize = true) {

            ISocketMessageChannel chnl;
            // return if channel not linked or found
            if ((chnl = AchvsChannel) == null) {
                if (serialize)
                    Program.Controller.SerializeAchievements();
                return;
            };

            var msg = await getMessage();
            if (msg == null) {
                msg = await Program.DiscordIO.SendMessage("", AchvsChannel, GetEmbed());
                
                if (msg != null)
                    this.DiscordMsgId = msg.Id;
            }
            else {
                await Program.DiscordIO.EditMessage(msg, "", GetEmbed());
            }

            if (serialize)
                Program.Controller.SerializeAchievements();
        }

        public async Task DeleteMessage() {
            var msg = await getMessage();
            if (msg != null) {
                await msg.DeleteAsync();
                this.DiscordMsgId = 0;
            }

            Program.Controller.SerializeAchievements();
        }

        public Embed GetEmbed() {
            var eb = new EmbedBuilder()
                .WithTitle(this.Name)
                .WithColor(Discord.Color.Blue)
                .WithFooter("ID: " + this.Id);
            
            eb.AddField("Description", this.Description, false);

            eb.AddField($"Completed ({this.Players.Count}):", 
                this.Players.Count == 0 ? "Nobody has completed this achievement yet." : string.Join(", ", this.Players));

            return eb.Build();
        }

        #endregion

        /// <summary>
        /// Constructor for the achievement.
        /// </summary>
        /// <param name="name">The name of the achievement.</param>
        /// <param name="description">The description of the achievement.</param>
        public Achievement(string name, string description) {
            this.Name = name;
            this.Description = description;

            this.Id = Player.RandomString(28);

        }

        /// <summary>
        /// Empty constructor for serialization
        /// </summary>
        public Achievement() {}

        public async Task Edit(string name, string description = null) {
            if (name != null) this.Name = name;
            if (description != null) this.Description = description;

            await SendMessage();
        }

        /// <summary>
        /// Adds a player to the achievement.
        /// </summary>
        /// <param name="p">The player to add.</param>
        /// <returns>Whether or not the player was successfully added.</returns>
        public async Task<bool> AddPlayer(Player p) {
            if (this.Players.Contains(p)) return false;

            this.Players.Add(p);
            this.playerUUIDs.Add(p.UUId);

            p.AddAchievement(this);

            await SendMessage();

            return true;
        }

        /// <summary>
        /// Removes a player to the achievement.
        /// </summary>
        /// <param name="p">The player to remove.</param>
        /// <returns>Whether or not the player was successfully remove.</returns>
        public async Task<bool> RemovePlayer(Player p) {
            if (!this.Players.Contains(p)) return false;

            this.Players.Remove(p);
            this.playerUUIDs.Remove(p.UUId);

            p.RemoveAchievement(this);

            await SendMessage();

            return true;
        }

        /// <summary>
        /// Converts a list of UUIDs into achievements.
        /// </summary>
        /// <param name="ids">The IDs to convert.</param>
        /// <returns>The converted list of achievements.</returns>
        public static List<Achievement> UUIDListToAchvs(IEnumerable<string> ids) {
            List<Achievement> returns = new List<Achievement>();
            foreach (var id in ids) {
                var found = Program.Controller.Achievements.FindAchievementByUUID(id);
                if (found != null) {
                    returns.Add(found);
                }
            }
            return returns;
        }

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

            return this.Id.Equals(((Achievement)obj).Id);
        }
        
        // override object.GetHashCode
        public override int GetHashCode()
        {
            return 7 * this.playerUUIDs.GetHashCode() + 13 * this.Id.GetHashCode() + 17 * this.Name.GetHashCode() + 19 * this.Description.GetHashCode();
        }

        public override string ToString()
        {
            return this.Name;
        }

    }
}