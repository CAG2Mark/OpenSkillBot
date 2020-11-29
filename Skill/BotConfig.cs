using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OpenTrueskillBot.Skill
{
    public class BotConfig : BindableBase {
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
        private List<ulong> permittedUserIds;
        private List<ulong> permittedRoleIds;
        private string challongeToken;
        private ObservableCollection<Rank> ranks;
        public ulong unrankedId;

        public BotConfig() {
            Ranks.CollectionChanged += (o, e) => OnPropertyChanged(nameof(Ranks));
        }

        #region Skill-related

        public double DefaultSigma {
            get => defaultSigma; set => Set(ref defaultSigma, value);
        }
        public double DefaultMu {
            get => defaultMu; set => Set(ref defaultMu, value);
        }
        public double Tau {
            get => tau; set => Set(ref tau, value);
        }

        public double Beta {
            get => beta; set => Set(ref beta, value);
        }
        public double DrawProbability {
            get => drawProbability; set => Set(ref drawProbability, value);
        }
        public double TrueSkillDeviations {
            get => trueSkillDeviations; set => Set(ref trueSkillDeviations, value);
        }

        public ObservableCollection<Rank> Ranks { 
            get => ranks; set => Set(ref ranks, value); 
        }

        public ulong UnrankedId {
            get => UnrankedId; set => Set(ref unrankedId, value);
        }

        #endregion

        #region Discord related

        public string BotToken {
            get => botToken; set => Set(ref botToken, value);
        }
        public ulong LeaderboardChannelId {
            get => leaderboardChannelId; set => Set(ref leaderboardChannelId, value);
        }
        public ulong HistoryChannelId {
            get => historyChannelId; set => Set(ref historyChannelId, value);
        }
        public ulong CommandChannelId {
            get => commandChannelId; set => Set(ref commandChannelId, value);
        }

        public List<ulong> PermittedUserIds {
            get => permittedUserIds; set => Set(ref permittedUserIds, value);
        }
        public List<ulong> PermittedRoleIds {
            get => permittedRoleIds; set => Set(ref permittedRoleIds, value);
        }

        #endregion

        public string ChallongeToken {
            get => challongeToken; set => Set(ref challongeToken, value);
        }
    }
}