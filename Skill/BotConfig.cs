using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace OpenSkillBot.Skill
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
        private ulong activeMatchesChannelId;

        private ulong logsChannelId;
        private ulong tourneysChannelId;
        private ulong signupLogsChannelId;
        private ulong signupsChannelId;
        private ulong achievementsChannelId;
        private List<string> permittedRoleNames = new List<string>();
        private string challongeToken;
        private List<Rank> ranks = new List<Rank>();
        public ulong unrankedId;


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
            get => leaderboardChannelId; set { Set(ref leaderboardChannelId, value); leaderboardChannel = null; }
        }

        private ISocketMessageChannel leaderboardChannel;
        public ISocketMessageChannel GetLeaderboardChannel() {
            if (leaderboardChannel == null) 
                leaderboardChannel = Program.DiscordIO.GetChannel(leaderboardChannelId);
            return leaderboardChannel;
        }

        public ulong HistoryChannelId
        {
            get => historyChannelId; set { Set(ref historyChannelId, value); historyChannel = null; }
        }

        private ISocketMessageChannel historyChannel;
        public ISocketMessageChannel GetHistoryChannel() {
            if (historyChannel == null) 
                historyChannel = Program.DiscordIO.GetChannel(historyChannelId);
            return historyChannel;
        }

        public ulong CommandChannelId
        {
            get => commandChannelId; set { Set(ref commandChannelId, value); commandChannel = null; }
        }

        private ISocketMessageChannel commandChannel;
        public ISocketMessageChannel GetCommandChannel() {
            if (commandChannel == null) 
                commandChannel = Program.DiscordIO.GetChannel(commandChannelId);
            return commandChannel;
        }


        public ulong ActiveMatchesChannelId
        {
            get => activeMatchesChannelId; set { Set(ref activeMatchesChannelId, value); activeMatchesChannel = null; }
        }

        private ISocketMessageChannel activeMatchesChannel;
        public ISocketMessageChannel GetActiveMatchesChannel() {
            if (activeMatchesChannel == null) 
                activeMatchesChannel = Program.DiscordIO.GetChannel(activeMatchesChannelId);
            return activeMatchesChannel;
        }


        public ulong LogsChannelId { get => logsChannelId; set { Set(ref logsChannelId, value); activeMatchesChannel = null; } }

        private ISocketMessageChannel logsChannel;
        public ISocketMessageChannel GetLogsChannel() {
            if (logsChannel == null) 
                logsChannel = Program.DiscordIO.GetChannel(logsChannelId);
            return logsChannel;
        }

        public ulong TourneysChannelId { get => tourneysChannelId; set { Set(ref tourneysChannelId, value); tourneysChannel = null; } }

        private ISocketMessageChannel tourneysChannel;
        public ISocketMessageChannel GetTourneysChannel() {
            if (tourneysChannel == null) 
                tourneysChannel = Program.DiscordIO.GetChannel(tourneysChannelId);
            return tourneysChannel;
        }

        public ulong SignupLogsChannelId { get => signupLogsChannelId; set { Set(ref signupLogsChannelId, value); leaderboardChannel = null; } }

        private ISocketMessageChannel signupLogsChannel;
        public ISocketMessageChannel GetSignupLogsChannel() {
            if (signupLogsChannel == null) 
                signupLogsChannel = Program.DiscordIO.GetChannel(signupLogsChannelId);
            return signupLogsChannel;
        }

        public ulong SignupsChannelId { get => signupsChannelId; set { Set(ref signupsChannelId, value); leaderboardChannel = null; } }

        private ISocketMessageChannel signupsChannel;
        public ISocketMessageChannel GetSignupsChannel() {
            if (signupsChannel == null) 
                signupsChannel = Program.DiscordIO.GetChannel(signupsChannelId);
            return signupsChannel;
        }

        public ulong AchievementsChannelId { get => achievementsChannelId; set { Set(ref achievementsChannelId, value); leaderboardChannel = null; } }

        private ISocketMessageChannel achievementsChannel;
        public ISocketMessageChannel GetAchievementsChannel() {
            if (achievementsChannel == null) 
                achievementsChannel = Program.DiscordIO.GetChannel(achievementsChannelId);
            return achievementsChannel;
        }

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