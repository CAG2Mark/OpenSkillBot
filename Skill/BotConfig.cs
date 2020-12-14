using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace OpenTrueskillBot.Skill
{
    public class BotConfig : PropertyNotifier
    {

        private double defaultSigma = 100;
        private double defaultMu = 1475;
        private double tau = 5;
        private double beta = 50;
        private double drawProbability = 0.05;
        private double trueSkillDeviations = 3;
        private string botToken;
        private ulong leaderboardChannelId;
        private ulong historyChannelId;
        private ulong commandChannelId;
        private List<string> permittedRoleNames = new List<string>();
        private string challongeToken;
        private List<Rank> ranks = new List<Rank>();
        public ulong unrankedId;
        private ulong logsChannelId = 0;

        public BotConfig()
        {
            // Ranks.CollectionChanged += (o, e) => OnPropertyChanged(nameof(Ranks));
        }

        #region Skill-related

        public double DefaultSigma
        {
            get => defaultSigma; set => Set(ref defaultSigma, value);
        }
        public double DefaultMu
        {
            get => defaultMu; set => Set(ref defaultMu, value);
        }
        public double Tau
        {
            get => tau; set => Set(ref tau, value);
        }

        public double Beta
        {
            get => beta; set => Set(ref beta, value);
        }
        public double DrawProbability
        {
            get => drawProbability; set => Set(ref drawProbability, value);
        }
        public double TrueSkillDeviations
        {
            get => trueSkillDeviations; set => Set(ref trueSkillDeviations, value);
        }

        public List<Rank> Ranks
        {
            get => ranks; set => Set(ref ranks, value);
        }

        public ulong UnrankedId
        {
            get => unrankedId; set => Set(ref unrankedId, value);
        }

        #endregion

        #region Discord related

        public string BotToken
        {
            get => botToken; set => Set(ref botToken, value);
        }
        public ulong LeaderboardChannelId
        {
            get => leaderboardChannelId; set => Set(ref leaderboardChannelId, value);
        }
        public ulong HistoryChannelId
        {
            get => historyChannelId; set => Set(ref historyChannelId, value);
        }
        public ulong CommandChannelId
        {
            get => commandChannelId; set => Set(ref commandChannelId, value);
        }

        public ulong LogsChannelId { get => logsChannelId; set => Set(ref logsChannelId, value); }

        public List<string> PermittedRoleNames
        {
            get => permittedRoleNames; set => Set(ref permittedRoleNames, value);
        }

        #endregion

        public string ChallongeToken
        {
            get => challongeToken; set => Set(ref challongeToken, value);
        }
    }
}