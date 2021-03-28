using System;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Moserware.Skills;
using System.Timers;
using System.Text;
using Newtonsoft.Json;
using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;
using Discord.WebSocket;
using System.Collections.Generic;
using OpenSkillBot.Achievements;
using OpenSkillBot.Tournaments;
using OpenSkillBot.Serialization;

namespace OpenSkillBot.Skill
{
    public class Player
    {

        /// <summary>
        /// The player's unique identifier.
        /// </summary>
        /// <value></value>
        public Player(string uUId, string iGN, string alias, Rank playerRank) 
        {
            this.UUId = uUId;
                this.IGN = iGN;
                this.Alias = alias;
                this.PlayerRank = playerRank;
               
        }


        /// <summary>
        /// Refers to whether the player is currently playing.
        /// </summary>
        /// <value></value>
        public bool IsPlaying { get; set; } = false;

        [JsonProperty]
        public string UUId { get; private set; }

        /// <summary>
        /// The player's IGN.
        /// </summary>
        /// <value></value>
        public string IGN { get; set; }

        /// <summary>
        /// The player's alias.
        /// </summary>
        /// <value></value>
        public string Alias { get; set; }

        /// <summary>
        /// Whether or not this player is marked for deletion and therefore cannot be added to tournaments or matches.
        /// </summary>
        /// <value></value>
        public bool MarkedForDeletion { get; set; } = false;


        [JsonProperty]
        /// <summary>
        /// The player's Discord ID.
        /// </summary>
        /// <value></value>
        public ulong DiscordId { 
            get => discordId; 
            private set { 
                discordId = value; 
            }
        }

        /* 
        
        The actions and tournaments are stored in a priority queue to make it fast to check for the latest tournament or match the player was in.

        This is to help the implementation of the built-in rank decay.

        */


        /// <summary>
        /// The actions this person is a part of.
        /// </summary>
        public PriorityQueue<ActionContainer> Actions { get; set; } = new PriorityQueue<ActionContainer>(true);

        /// <summary>
        /// The tournaments this person is a part of.
        /// </summary>
        public PriorityQueue<TourneyContainer> Tournaments { get; set; } = new PriorityQueue<TourneyContainer>(true);

        public uint TournamentsMissed { get; set; } = 0;

        public uint DecayCycle { get; set; } = 0;
        public uint LastDecay { get; set; } = 0;

        private SocketGuildUser discordUser;
        [JsonIgnoreAttribute]
        public SocketGuildUser DiscordUser {
            get {
                if (discordUser == null) discordUser = Program.DiscordIO.GetUser(DiscordId);
                return discordUser;
            }
        }

        /// <summary>
        /// The player's standard deviation.
        /// </summary>
        public double Sigma
        {
            get => sigma;
            set
            {
                // a standard deviation of zero or less is mathematically undefined
                if (sigma <= 0) throw new ArithmeticException("The standard deviation of a player cannot be less than or equal to zero.");
                sigma = value;

                if (!refreshingRank) QueueRankRefresh();

                Console.WriteLine("Sigma of " + IGN + " set to " + value);
            }
        }
        /// <summary>
        /// The player's mean skill value as shown on the normal distribution.
        /// </summary>
        public double Mu
        {
            get => mu;
            set
            {
                mu = value;
                if (!refreshingRank) QueueRankRefresh();
                Console.WriteLine("Mu of " + IGN + " set to " + value);
            }
        }


        [JsonProperty]
        private List<string> achievementUUIds { get; set; } = new List<string>();
        private List<Achievement> achievements;

        [JsonIgnoreAttribute]
        /// <summary>
        /// A list of the achievements the player has been given.
        /// </summary>
        /// <value></value>
        public List<Achievement> Achievements {
            get {
                if (achievements == null) achievements = Achievement.UUIDListToAchvs(achievementUUIds);
                return achievements;
            }
        }

        public void AddAchievement(Achievement a) {
            if (this.achievements == null) this.achievements = new List<Achievement>();
            achievements.Add(a);
            achievementUUIds.Add(a.Id);

            Program.Controller.SerializeLeaderboard();
        }

        public void RemoveAchievement(Achievement a) {
            if (this.achievements == null) this.achievements = new List<Achievement>();

            achievements.Remove(a);
            achievementUUIds.Remove(a.Id);

            Program.Controller.SerializeLeaderboard();
        }

        Timer rankRefreshTimer = new Timer(2000);
        private bool rankRefreshQueued = false;
        private bool hardRefreshQueued = false;

        private void initRankRefresh()
        {
            rankRefreshTimer.Start();
            rankRefreshTimer.Elapsed += async (o, e) =>
            {
                if (rankRefreshQueued) {
                    // makes sure any updates to the player's skill at the immediate moment
                    // come through before the rank is refreshed
                    await Task.Delay(100);
                    await UpdateRank(hardRefreshQueued);
                    hardRefreshQueued = false;
                    rankRefreshQueued = false;
                }
            };
        }
        public void QueueRankRefresh(bool hard = false)
        {
            hardRefreshQueued = hardRefreshQueued || hard;
            rankRefreshQueued = true;
        }


        [JsonIgnore]
        /// <summary>
        /// The displayed skill of the player.
        /// </summary>
        public double DisplayedSkill => this.Mu - Program.Config.TrueSkillDeviations * Sigma;

        public static Rank GetRank(double mu, double sigma) {
            if (Program.Config.UnrankedEnabled && sigma >= Program.Config.UnrankedRDThreshold) return Rank.GetUnrankedRank();

            return getRank(mu - sigma * Program.Config.TrueSkillDeviations);
        }
        private static Rank getRank(double skill) {

            foreach (var rank in Program.Config.Ranks)
            {
                // note: ranks are sorted in descending order
                if (rank.LowerBound <= skill)
                {
                    return rank;
                }
            }
            return null;
        }

        public void RefreshRank() {
            this.PlayerRank = GetRank(this.Mu, this.Sigma);
        } 

        public void LinkDiscord(ulong id) {
            this.discordId = id;
            this.discordUser = null;
            Program.Controller.SerializeLeaderboard();
        }

        public void UnlinkDiscord() {
            LinkDiscord(0);
            Program.Controller.SerializeLeaderboard();
        }

        public Rank PlayerRank { get; private set; } = null;

        public bool IsUnranked => Program.Config.UnrankedEnabled && this.Sigma >= Program.Config.UnrankedRDThreshold;

        private bool refreshingRank;

        /// <summary>
        /// Gets the current rank of the player.
        /// </summary>
        /// <value>The rank of the player, or null if not found.</value>
        public async Task UpdateRank(bool hardRefresh = false)
        {
            if (refreshingRank) return;

            refreshingRank = true;
            if (Program.Config.Ranks == null || Program.Config.Ranks.Count == 0) {
                refreshingRank = false;
                return;
            }

            var oldRank = this.PlayerRank;
            RefreshRank();

            if (this.discordId == 0 || !Program.DiscordIO.IsReady) {
                refreshingRank = false;
                return;
            }
            
            if (oldRank == null || !oldRank.Equals(this.PlayerRank) || hardRefresh) {
                // check if the player exists
                var player = DiscordUser;
                if (player == null) {
                    refreshingRank = false;
                    return;
                }

                try {
                    if (oldRank != null && oldRank.RoleId != 0&& !hardRefresh)
                        await Program.DiscordIO.RemoveRole(player, oldRank.RoleId);
                }
                catch (Exception ex) {
                    await Program.DiscordIO.Log(new Discord.LogMessage(
                        Discord.LogSeverity.Warning,
                        "Program",
                        ex.Message
                    ));
                }

                try {
                    // hard refresh
                    if (hardRefresh) {
                        await Program.DiscordIO.RemoveRoles(player, Program.Config.Ranks.Select(o => o.RoleId).Where(o => o != 0));
                    }
                }
                catch (Exception ex) {
                    await Program.DiscordIO.Log(new Discord.LogMessage(
                        Discord.LogSeverity.Warning,
                        "Program",
                        ex.Message
                    ));
                }
                try {
                    if (this.PlayerRank != null && PlayerRank.RoleId != 0)
                        await Program.DiscordIO.AddRole(player, PlayerRank.RoleId);
                }
                catch (Exception ex) {
                    await Program.DiscordIO.Log(new Discord.LogMessage(
                        Discord.LogSeverity.Warning,
                        "Program",
                        ex.Message
                    ));
                }
            }

            Program.Controller.SerializeLeaderboard();

            Console.WriteLine("Refreshed the rank of " + IGN);
            refreshingRank = false;
        }

        // Use when creating a new player
        public Player()
        {
            this.UUId = RandomString(20);
            initRankRefresh();
        }

        // Use when creating a new player with a non-standard starting trueskill
        public Player(string ign, double mu, double sigma = -1)
        {
            this.IGN = ign;
            this.UUId = RandomString(20);
            this.Mu = mu;

            if (sigma != -1) this.Sigma = sigma;

            initRankRefresh();
        }
 
        public string GenerateSummary() {
            StringBuilder sb = new StringBuilder();
            var nl = Environment.NewLine;
            sb.Append($"**Name**: {this.IGN}{nl}");
            sb.Append($"**Alias**: {(string.IsNullOrWhiteSpace(this.Alias) ? "None" : this.Alias)}{nl}");
            sb.Append($"**Skill**: {r(this.DisplayedSkill)} RD {r(this.Sigma)}{nl}");
            sb.Append($"**Rank**: {(PlayerRank == null ? "No rank" : PlayerRank.Name)}{nl}");
            sb.Append($"**Achievements**: {(Achievements.Count == 0 ? "No achievements." : string.Join(", ", Achievements))}{nl}");
            sb.Append($"**Matches:** {Actions.Where(a => a != null && a.Action.GetType() == typeof(MatchAction)).Count()}{nl}");
            sb.Append($"**Tournaments:** {Tournaments.Count() - 1}{nl}");
            var user = this.DiscordId == 0 ? null : DiscordUser;
            sb.Append($"**Discord Link**: {(user == null ? "None" : $"Linked as {user.Mention}")}{nl}");
            sb.Append($"**Bot ID:** {this.UUId}");

            return sb.ToString();
        }

        public async Task Deafen() {
            if (DiscordUser == null) return;
            await Program.DiscordIO.DeafenUser(DiscordUser);
        }

        public async Task Undeafen() {
            if (DiscordUser == null) return;
            await Program.DiscordIO.UndeafenUser(DiscordUser);
        }

        private static int r(double f) {
            return (int)Math.Round(f, 0);
        }


        // Source: https://stackoverflow.com/questions/1344221/how-can-i-generate-random-alphanumeric-strings
        private static Random random = new Random();
        private double sigma = Program.Config.DefaultSigma;
        private double mu;
        private ulong discordId = 0;

        public static string RandomString(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
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

            return ((Player)obj).UUId.Equals(this.UUId);
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return this.UUId.GetHashCode() + this.DiscordId.GetHashCode() + this.Sigma.GetHashCode() + this.Mu.GetHashCode();
        }

        public override string ToString()
        {
            return this.IGN;
        }

    }
}