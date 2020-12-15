using System;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Moserware.Skills;
using System.Timers;
using System.Text;
using Newtonsoft.Json;
using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;

namespace OpenTrueskillBot.Skill
{
    public class Player
    {

        /// <summary>
        /// The player's unique identifier.
        /// </summary>
        /// <value></value>
        public string UUId { get; set; }

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
        /// The player's Discord ID.
        /// </summary>
        /// <value></value>
        public ulong DiscordId { get => discordId; set { discordId = value; }}
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

                QueueRankRefresh();
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
                QueueRankRefresh();
            }
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
            return GetRank(mu - sigma * Program.Config.TrueSkillDeviations);
        }
        public static Rank GetRank(double skill) {
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
            this.PlayerRank = GetRank(DisplayedSkill);
        } 


        public Rank PlayerRank { get; set; } = null;

        /// <summary>
        /// Gets the current rank of the player.
        /// </summary>
        /// <value>The rank of the player, or null if not found.</value>
        public async Task UpdateRank(bool hardRefresh = false)
        {
            if (Program.Config.Ranks == null || Program.Config.Ranks.Count == 0) return;

            var oldRank = this.PlayerRank;
            RefreshRank();

            if (this.discordId == 0 || !Program.DiscordIO.IsReady) return;
            
            if (oldRank == null || !oldRank.Equals(this.PlayerRank) || hardRefresh) {
                // check if the player exists
                var player = Program.DiscordIO.GetUser(this.DiscordId);
                if (player == null) return;

                try {
                    if (oldRank != null && !hardRefresh)
                        await Program.DiscordIO.RemoveRole(player, oldRank.RoleId);
                }
                catch (Exception) {
                    // todo: log error
                }

                try {
                    // hard refresh
                    if (hardRefresh) {
                        await Program.DiscordIO.RemoveRoles(player, Program.Config.Ranks.Select(o => o.RoleId));
                    }
                }
                catch (Exception) {

                }
                try {
                    if (this.PlayerRank != null)
                        await Program.DiscordIO.AddRole(player, PlayerRank.RoleId);
                }
                catch (Exception) {
                }
            }


        }

        // Use when creating a new player
        public Player()
        {
            this.UUId = RandomString(16);
            initRankRefresh();
        }

        // Use when creating a new player with a non-standard starting trueskill
        public Player(string ign, double mu, double sigma = -1)
        {
            this.IGN = ign;
            this.UUId = RandomString(16);
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
            var user = this.DiscordId == 0 ? null : Program.DiscordIO.GetUser(this.DiscordId);
            sb.Append($"**Discord Link**: {(user == null ? "None" : $"Linked as {user.Username}#{user.DiscriminatorValue} with ID {this.DiscordId}")}{nl}");
            sb.Append($"**Bot ID:** {this.UUId}");

            return sb.ToString();
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
            return this.UUId.GetHashCode() * 17 + this.DiscordId.GetHashCode() + this.Sigma.GetHashCode() + this.Mu.GetHashCode();
        }

    }
}