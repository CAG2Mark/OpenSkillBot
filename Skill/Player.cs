using System;
using System.Linq;

using Moserware.Skills;

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
        public long DiscordId { get; set; }

        /// <summary>
        /// The player's standard deviation.
        /// </summary>
        public double Sigma 
        { 
            get => sigma; 
            set 
            {
                // a standard deviation of zero or less is mathematically undefined
                if (sigma <= 0) throw new ArithmeticException("The standard deviation of a player cannot be less than zero.");

                sigma = value;
            } 
        }
        /// <summary>
        /// The player's mean skill value as shown on the normal distribution.
        /// </summary>
        public double Mu { get; set; }

        /// <summary>
        /// The displayed skill of the player.
        /// </summary>
        public double DisplayedSkill => this.Mu - Program.CurSkillConfig.TrueSkillDeviations * Sigma;

        // Use when creating a new player
        public Player()
        {
            this.UUId = RandomString(16);
        }

        // Use when creating a new player with a non-standard starting trueskill
        public Player(double mu, double sigma = -1)
        {
            this.UUId = RandomString(16);
            this.Mu = mu;

            if (sigma != -1) this.Sigma = sigma;
        }


        // Source: https://stackoverflow.com/questions/1344221/how-can-i-generate-random-alphanumeric-strings
        private static Random random = new Random();
        private double sigma = Program.CurSkillConfig.DefaultSigma;

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