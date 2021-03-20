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
using OpenSkillBot.Tournaments;
using OpenSkillBot.Achievements;
using OpenSkillBot.Serialization;

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

        public PendingMatch(bool isTourney, ulong messageId) 
        {
            this.IsTourney = isTourney;
                this.messageId = messageId;
               
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
                .WithColor(!IsTourney ? Discord.Color.Blue : Discord.Color.Purple);

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

    // lazy serialization technique so i dont have to write code to re-populate all the matches. instead the hash and the linked list
    // are stored together so json.net is able to serialize each by reference :)))
    public class MatchesStruct {
        public BotAction LatestAction { get; set; }
        // for O(1) lookup of matches :)
        public Dictionary<string, BotAction> MatchHash { get; set; } = new Dictionary<string, BotAction>();
    }

    // support for old type, dont break old version deserialization
    public class MatchesStructOld {
        public MatchAction LatestAction { get; set; }
        // for O(1) lookup of matches :)
        public Dictionary<string, MatchAction> MatchHash { get; set; } = new Dictionary<string, MatchAction>();
    }

    public class TourneyStruct {

        public IDictionary<string, Tournament> CompletedTournaments { get; set; } = new Dictionary<string, Tournament>();

        public List<Tournament> Tournaments { get; set; } = new List<Tournament>();

        // referential integrity after restart
        private Tournament activeTournament;
        [JsonIgnore]
        public Tournament ActiveTournament {
            get {
                if (activeTournament == null) {
                    // search
                    activeTournament = Tournaments.FirstOrDefault(t => t.Id.Equals(activeTournamentId));
                }
                return activeTournament;
            }
            set {
                activeTournament = value;
                activeTournamentId = value == null ? null : value.Id;
                Program.Controller.SerializeTourneys();
            }
        }

        [JsonProperty]
        private string activeTournamentId { get; set; }

    }

    public class BotController
    {

        private const string lbFileName = "leaderboard.json";
        private const string ahFileName = "actionhistory.json";

        private const string pmFileName = "pendingmatches.json";

        private const string tourneyFileName = "tourneys.json";

        private const string achvsFileName = "achievements.json";

        public Leaderboard CurLeaderboard;

        public AchievementsContainer Achievements = new AchievementsContainer();

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

        public MatchesStruct Matches = new MatchesStruct();
        public BotAction LatestAction { get => Matches.LatestAction; set => Matches.LatestAction = value; }
        // for O(1) lookup of matches :)
        public Dictionary<string, BotAction> MatchHash { get => Matches.MatchHash; set => Matches.MatchHash = value; }
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
                try {
                    Matches = SerializeHelper.Deserialize<MatchesStruct>(ahFileName);
                }
                catch (JsonException) {
                    Console.WriteLine("Could not deserialise action history. Trying to deserialise in old format.");
                    // backwards compatibility for old version
                    var oldVer = SerializeHelper.Deserialize<MatchesStructOld>(ahFileName);
                    Matches.LatestAction = oldVer.LatestAction;
                    foreach (var k in oldVer.MatchHash.Keys) {
                        Matches.MatchHash.Add(k, oldVer.MatchHash[k]);
                    }
                }

                if (LatestAction != null)
                    LatestAction.RepopulateLinks();
            }

            // get pending matches
            if (File.Exists(pmFileName)) {
                pendingMatches = SerializeHelper.Deserialize<List<PendingMatch>>(pmFileName);
            }

            // get tournaments
            if (File.Exists(tourneyFileName)) {
                Tourneys = SerializeHelper.Deserialize<TourneyStruct>(tourneyFileName);
            }

            // get achievements
            if (File.Exists(achvsFileName)) {
                Achievements = SerializeHelper.Deserialize<AchievementsContainer>(achvsFileName);
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
            SerializeTourneys();
            SerializeAchievements();
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
                SerializeHelper.Serialize(Matches, ahFileName);
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

        public bool SerializeTourneys() {
            // Convert the pending players to a list of UUIDs
            try {
                SerializeHelper.Serialize(Tourneys, tourneyFileName);
                return true;
            }
            catch (System.Exception) {
                Console.WriteLine("WARNING: Failed to save tournaments!");
                return false;
            }
        }

        public bool SerializeAchievements() {
            // Convert the pending players to a list of UUIDs
            try {
                SerializeHelper.Serialize(Achievements, achvsFileName);
                return true;
            }
            catch (System.Exception) {
                Console.WriteLine("WARNING: Failed to save achievements!");
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

            MatchAction action = new MatchAction(team1, team2, result == 0, isTourney, result == -1);

            if (isTourney) {
                action.TourneyId = Tourneys.ActiveTournament.Id;
            }

            if (result != -1) {
                if (LatestAction != null) {
                    // note: this does recalculate
                    await LatestAction.InsertAfter(action);
                }
                else {
                    await action.DoAction();
                }

                LatestAction = action;
            }
            else {
                await action.UndeafenPlayers();
            }

            SerializeActions();

            return action;
        }

        public BotAction FindAction(string id) {
            if (MatchHash.ContainsKey(id)) return MatchHash[id];
            return null;
        }

        public void AddActionToHash(BotAction m) {
            if (MatchHash.ContainsKey(m.ActionId)) MatchHash[m.ActionId] = m;
            else MatchHash.Add(m.ActionId, m);
        }

        public void RemoveActionFromHash(BotAction m) {
            if (MatchHash.ContainsKey(m.ActionId)) MatchHash.Remove(m.ActionId);
        }

        // compatibility with older code
        public List<Tournament> Tournaments { get => Tourneys.Tournaments; set => Tourneys.Tournaments = value; }
        public TourneyStruct Tourneys = new TourneyStruct();

        public bool IsTourneyActive => Tourneys.ActiveTournament.IsActive;
        public async Task AddTournament(Tournament t) {
            Tournaments.Add(t);
            SerializeTourneys();
            await t.SendMessage();
        }

        public async Task RemoveTournament(Tournament t) {
            Tournaments.Remove(t);
            SerializeTourneys();
            await t.DeleteMessage();
        }

        public async Task<bool> StartTournament(Tournament t) {
            if (t.IsActive) return false;
            await t.Start(true);
            // Tourneys.ActiveTournament = t;
            SerializeTourneys();
            return true;
        }

        public async Task<bool> ForceStop(Tournament t) {
            // if (!Tourneys.ActiveTournament.IsActive) return false;
            await t.Start(false);
            
            SerializeTourneys();

            return true;
        }

        public async Task<BotAction> UndoAction() {
            if (LatestAction == null) return null;
            
            var action = LatestAction;
            await action.Undo();
            LatestAction = action.PrevAction;
            Program.Controller.SerializeActions();
            return action;
        }
    }
}