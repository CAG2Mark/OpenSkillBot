using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Discord.WebSocket;

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
        private ulong activeMatchesChannelId;
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

        private ISocketMessageChannel leaderboardChannel;
        public ISocketMessageChannel GetLeaderboardChannel() {
            if (leaderboardChannel == null) 
                leaderboardChannel = Program.DiscordIO.GetChannel(leaderboardChannelId);
            return leaderboardChannel;
        }

        public ulong HistoryChannelId
        {
            get => historyChannelId; set => Set(ref historyChannelId, value);
        }

        private ISocketMessageChannel historyChannel;
        public ISocketMessageChannel GetHistoryChannel() {
            if (historyChannel == null) 
                historyChannel = Program.DiscordIO.GetChannel(historyChannelId);
            return historyChannel;
        }

        public ulong CommandChannelId
        {
            get => commandChannelId; set => Set(ref commandChannelId, value);
        }

        private ISocketMessageChannel commandChannel;
        public ISocketMessageChannel GetCommandChannel() {
            if (commandChannel == null) 
                commandChannel = Program.DiscordIO.GetChannel(commandChannelId);
            return commandChannel;
        }


        public ulong ActiveMatchesChannelId
        {
            get => activeMatchesChannelId; set => Set(ref activeMatchesChannelId, value);
        }

        private ISocketMessageChannel activeMatchesChannel;
        public ISocketMessageChannel GetActiveMatchesChannel() {
            if (activeMatchesChannel == null) 
                activeMatchesChannel = Program.DiscordIO.GetChannel(activeMatchesChannelId);
            return activeMatchesChannel;
        }


        public ulong LogsChannelId { get => logsChannelId; set => Set(ref logsChannelId, value); }

        private ISocketMessageChannel logsChannel;
        public ISocketMessageChannel GetLogsChannel() {
            if (logsChannel == null) 
                logsChannel = Program.DiscordIO.GetChannel(logsChannelId);
            return logsChannel;
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