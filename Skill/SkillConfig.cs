using System;

namespace OpenTrueskillBot.Skill
{
    public class SkillConfig
    {
        public double DefaultSigma { get; set; } = 1475;
        public double DefaultMu { get; set; } = 100;

        public double Tau { get; set; } = 5;
        public double Beta { get; set; } = 50;

        public double DrawProbability { get; set; } = 0.05;

        public double TrueSkillDeviations { get; set; } = 3;
    }
}