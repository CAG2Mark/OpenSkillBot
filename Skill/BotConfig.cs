using System;
using System.Collections.Generic;

namespace OpenTrueskillBot.Skill
{
    public class BotConfig
    {

        #region Skill-related

        public double DefaultSigma { get; set; } = 100;
        public double DefaultMu { get; set; } = 1475;

        public double Tau { get; set; } = 5;
        public double Beta { get; set; } = 50;

        public double DrawProbability { get; set; } = 0.05;

        public double TrueSkillDeviations { get; set; } = 3;

        #endregion
        
        #region Discord related

        public string BotToken { get; set; }
        public ulong LeaderboardChannelId { get; set; }
        public ulong HistoryChannelId { get; set; }
        public ulong CommandChannelId { get; set; }

        public List<ulong> PermittedUserIds { get; set; }
        public List<ulong> PermittedRoleIds { get; set; }

        #endregion

        public string ChallongeToken { get; set; }
    }
}