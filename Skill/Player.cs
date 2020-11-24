using System;
using System.Linq;

using Discord;

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
        /// The player's Discord ID.
        /// </summary>
        /// <value></value>
        public long DiscordId { get; set; }

        public double mu = Program.CurSkillConfig.DefaultMu;


        /// <summary>
        /// The player's standard deviation.
        /// </summary>
        public double Sigma { get; set; } = Program.CurSkillConfig.DefaultSigma;

        /// <summary>
        /// The player's mean skill value as shown on the normal distribution.
        /// </summary>
        public double Mu 
        { 
            get => mu; 
            set { 
                if (mu <= 0) {                   
                    return;
                }
                mu = value;
            } 
        }

        /// <summary>
        /// The displayed skill of the player.
        /// </summary>
        public double DisplayedSkill => this.Mu - Program.CurSkillConfig.TrueSkillDeviations * Sigma;

        // Use when creating a new player
        public Player() {
            this.UUId = RandomString(16);
        }

        // Use when creating a new player with a non-standard starting trueskill
        public Player(double sigma, double mu = -1) {
            this.UUId = RandomString(16);
            this.Sigma = sigma;

            if (mu != -1) this.Mu = mu;
        }


        // Source: https://stackoverflow.com/questions/1344221/how-can-i-generate-random-alphanumeric-strings
        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
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