using System;

namespace OpenTrueskillBot.Skill
{
    public class Player
    {
        private double mu = Program.CurSkillConfig.DefaultMu;

        public double Sigma { get; set; } = Program.CurSkillConfig.DefaultSigma;
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
        public double RawSkill => this.Sigma - Program.CurSkillConfig.TrueSkillDeviations * Mu;


    }
}