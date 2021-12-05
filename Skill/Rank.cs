using System;

namespace OpenSkillBot.Skill
{
    public class Rank : IComparable {
        public Rank(int lowerBound, ulong roleId, string name) {
            this.LowerBound = lowerBound;
            this.RoleId = roleId;
            this.Name = name;
        }

        /// <summary>
        /// Used for getting the unranked ID.
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="name"></param>
        private Rank(ulong roleId, string name) {
            this.RoleId = roleId;
            this.Name = name;
        }

        public int LowerBound { get; private set; }
        public ulong RoleId { get; private set;  }
        public string Name { get; private set; }

        public bool IsUnrankedRank { get; private set; }

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

            var rank = (Rank)obj;
            return this.IsUnrankedRank && rank.IsUnrankedRank || ( this.LowerBound == rank.LowerBound && this.Name.Equals(rank.Name) && this.RoleId == rank.RoleId );
        }
        
        // override object.GetHashCode
        public override int GetHashCode()
        {
            return this.LowerBound.GetHashCode() * 17 + this.RoleId.GetHashCode() * 7 + this.Name.GetHashCode();
        }


        public static Rank GetUnrankedRank() {
            return new Rank(Program.Config.UnrankedId, "Unranked") { IsUnrankedRank = true };
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return -1;
            
            var rank = (Rank)obj;
            if (this.IsUnrankedRank) return -1;
            return LowerBound.CompareTo(rank.LowerBound);
        }
    }
}