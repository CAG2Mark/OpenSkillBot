using OpenSkillBot.Skill;
using System;
using Newtonsoft.Json;
using System.IO;
using Discord;
using System.Linq;
using System.Timers;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord.Rest;

namespace OpenSkillBot
{
    public class PendingMatch
    {
        private Team team1;
        private Team team2;

        public PendingMatch(Team team1, Team team2, bool isTourney = false)
        {
            Team1 = team1;
            team1ids = team1.Players.Select(p => p.UUId);
            Team2 = team2;
            team2ids = team2.Players.Select(p => p.UUId);

            IsTourney = isTourney;
        }

        public PendingMatch() {
            // empty ctor for serialization
        }

        public bool IsTourney { get; set; }

        [JsonProperty]
        private IEnumerable<string> team1ids { get; set; }
        [JsonIgnore]
        public Team Team1 {
            get {
                if (team1 == null) team1 = MatchAction.UUIDListToTeam(team1ids);
                return team1;
            }
            private set => team1 = value;
        }

        [JsonProperty]
        private IEnumerable<string> team2ids { get; set; }
        [JsonIgnore]
        public Team Team2 {
            get {
                if (team2 == null) team2 = MatchAction.UUIDListToTeam(team2ids);
                return team2;
            }
            private set => team2 = value;
        }

        [JsonProperty]
        private ulong messageId { get; set; } = 0;
        public async Task SendMessage() {
            if (Program.Config.ActiveMatchesChannelId == 0) return;

            var eb = new EmbedBuilder()
                .WithFooter(!IsTourney ? "Standard match" : "Tournament match")
                .WithColor(Discord.Color.Blue);

            eb.AddField("Team 1", $"{String.Join(", ", Team1.Players.Select(p => p.IGN))}");
            eb.AddField("Team 2", $"{String.Join(", ", Team2.Players.Select(p => p.IGN))}");
            var msg = await Program.DiscordIO.SendMessage(
                "", Program.Config.GetActiveMatchesChannel(), eb.Build()
            );
            this.messageId = msg.Id;
        }

        public async Task DeleteMessage() {
            if (messageId == 0) return;
            var msg = (RestUserMessage) await Program.DiscordIO.GetMessage(messageId, Program.Config.ActiveMatchesChannelId);
            await msg.DeleteAsync();
        }

        public bool IsSameMatch(Team team1, Team team2) {
            return (team1.IsSameTeam(this.Team1) && team2.IsSameTeam(this.Team2)) || (team1.IsSameTeam(this.Team2) && team2.IsSameTeam(this.Team1));
        }
        
    }
    public class BotController
    {

        private const string lbFileName = "leaderboard.json";
        private const string ahFileName = "actionhistory.json";

        private const string pmFileName = "pendingmatches.json";

        public Leaderboard CurLeaderboard;

        private Timer lbTimer = new Timer(3000);

        private bool lbChangeQueued = false; 

        private string resetToken;
        public string GenerateResetToken() {
            resetToken = Player.RandomString(6);;
            return resetToken;
        }
        public bool CheckResetToken(string token) {
            return token.Equals(this.resetToken);
        }

        public MatchAction LatestAction;
        public BotController() {
            // get leaderboard
            if (File.Exists(lbFileName)) {
                CurLeaderboard = SerializeHelper.Deserialize<Leaderboard>(lbFileName);
                CurLeaderboard.Initialize();
            }
            else {
                CurLeaderboard = new Leaderboard();
                UpdateLeaderboard();
            }

            // get action history
            if (File.Exists(ahFileName)) {
                LatestAction = SerializeHelper.Deserialize<MatchAction>(ahFileName);
                if (LatestAction != null)
                    LatestAction.RepopulateLinks();
            }

            // get pending matches
            if (File.Exists(pmFileName)) {
                pendingMatches = SerializeHelper.Deserialize<List<PendingMatch>>(pmFileName);
            }

            CurLeaderboard.LeaderboardChanged += (o,e) => {
                UpdateLeaderboard();
            };

            // have the leaderboard update be set on a timer so that it doesn't fire multiple times a second unnecessarily
            lbTimer.Elapsed += (o, e) => {
                if (lbChangeQueued) UpdateLeaderboard();
            };
            lbTimer.Start();
        }

        public async void UpdateLeaderboard() {
            lbChangeQueued = false;
            
            // only send if the leaderboard channel is set
            if (Program.Config.LeaderboardChannelId != 0) {

                // split the message size so it's less than discord's message limit
                var lbStr = CurLeaderboard.GenerateLeaderboardText(2500);
                var lbStrArr = lbStr.ToArray();
            
                if (lbStrArr.Length != 0 && !string.IsNullOrWhiteSpace(lbStrArr[0]))
                    await Program.DiscordIO.PopulateChannel(Program.Config.LeaderboardChannelId, lbStrArr);

            }
            SerializeLeaderboard();
        }

        public void SerializeAll() {
            SerializeLeaderboard();
            SerializeActions();
            SerializePending();
        }

        public bool SerializeLeaderboard() {
            try
            {
                SerializeHelper.Serialize(CurLeaderboard, lbFileName);

                return true;
            }
            catch (System.Exception)
            {
                Console.WriteLine("WARNING: Failed to save leaderboard!");
                return false;
            }
        }

        public bool SerializeActions() {
            try {
                SerializeHelper.Serialize(LatestAction, ahFileName);
                return true;
            }
            catch (System.Exception) {
                Console.WriteLine("WARNING: Failed to save action history!");
                return false;
            }
        }

        public bool SerializePending() {
            // Convert the pending players to a list of UUIDs
            try {
                SerializeHelper.Serialize(pendingMatches, pmFileName);
                return true;
            }
            catch (System.Exception) {
                Console.WriteLine("WARNING: Failed to save pending matches!");
                return false;
            }
        }

        private List<PendingMatch> pendingMatches = new List<PendingMatch>();

        public async Task<PendingMatch> StartMatchAction(Team team1, Team team2, bool isTourney = false, bool force = false) {
            
            if (!force) {
                foreach (var p in team1.Players) {
                    if (p.IsPlaying) return null;
                } 
                foreach (var p in team2.Players) {
                    if (p.IsPlaying) return null;
                } 
            }

            var pm = new PendingMatch(team1, team2, isTourney);
            await pm.SendMessage();
            pendingMatches.Add(pm);
            // deafen in discord
            foreach (var p in team1.Players) {
                await p.Deafen();
                p.IsPlaying = true;
            } 
            foreach (var p in team2.Players) {
                await p.Deafen();
                p.IsPlaying = true;
            } 

            SerializePending();

            return pm;

        }

        public async Task<MatchAction> AddMatchAction(Team team1, Team team2, int result, bool isTourney = false) {
            // swap if result is 2
            if (result == 2) {
                var temp = team1;
                team1 = team2;
                team2 = temp;
            }

            // mark as not playing
            foreach (var p in team1.Players) {
                p.IsPlaying = false;
            }
            foreach (var p in team2.Players) {
                p.IsPlaying = false;
            }

            // Find the pending match containing the same team.
            foreach (var p in pendingMatches) {
                if (p.IsSameMatch(team1, team2)) {
                    pendingMatches.Remove(p);
                    await p.DeleteMessage();
                    SerializePending();
                    break;
                }
            }

            MatchAction action = new MatchAction(team1, team2, result == 0, isTourney);

            if (LatestAction != null) {
                // note: this does recalculate
                await LatestAction.InsertAfter(action);
            }
            else {
                await action.DoAction();
            }

            LatestAction = action;

            SerializeActions();

            return action;

        }

        public async Task<MatchAction> UndoAction() {
            if (LatestAction == null) return null;
            
            var action = LatestAction;
            await action.Undo();
            LatestAction = action.PrevAction;
            Program.Controller.SerializeActions();
            return action;
        }
    }
}