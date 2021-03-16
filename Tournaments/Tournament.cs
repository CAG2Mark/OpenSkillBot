using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Newtonsoft.Json;
using OpenSkillBot.BotCommands;
using OpenSkillBot.ChallongeAPI;
using OpenSkillBot.Skill;

namespace OpenSkillBot.Tournaments
{
    public enum TournamentType {
        SingleElim = 0,
        DoubleElim = 1,
        RoundRobin = 2,
        Swiss = 3
    }

    public struct MatchRanking {

        [JsonProperty]
        public Team Team { get; private set; }
        [JsonProperty]
        public uint Ranking { get; private set; }
        public MatchRanking(Team team, uint ranking)
        {
            Team = team;
            Ranking = ranking;
        }
    }

    public class MatchInfo {
        public ulong ChallongeId;
        public ulong Team1;
        public ulong Team2;

        public int Team1Score;
        public int Team2Score;

        public MatchInfo(ulong challongeId, ulong team1, ulong team2)
        {
            ChallongeId = challongeId;
            Team1 = team1;
            Team2 = team2;
        }
    }

    public class Tournament
    {

        #region properties

        // moderation

        public List<string> Allowlist { get; set; } = new List<string>();
        public List<string> Denylist { get; set; } = new List<string>();


        public TournamentType Format { get; set; }
        public DateTime StartTime { get; set; }
        public string Name { get; set; }

        [JsonProperty]
        public int Index { get; private set; }

        [JsonIgnore]
        public bool IsChallongeLinked => ChallongeId != null;

        // Challonge not yet implemeted
        [JsonProperty]
        public Nullable<ulong> ChallongeId { get; private set; }
        [JsonProperty]
        public string ChallongeUrl { get; private set; }


        [JsonProperty]
        private List<string> matchUUIds { get; set; } = new List<string>();
        
        private List<MatchAction> matches;
        [JsonIgnore]
        public List<MatchAction> Matches {
            get {
                if (matches == null) matches = MatchAction.UUIDListToMatches(matchUUIds);
                return matches;
            }
            private set => matches = value;
        }

        public List<Team> Teams { get; set; } = new List<Team>();

        public List<MatchInfo> MatchInfos { get; set; } = new List<MatchInfo>();

        [JsonProperty]
        public string Id { get; private set; }

        [JsonProperty]
        public bool IsActive { get; private set; }

        [JsonProperty]
        public bool IsCompleted { get; private set; } = false;

        [JsonProperty]
        private List<OldPlayerData> oldData { get; set; } = new List<OldPlayerData>();
        
        // for archival purposes
        [JsonProperty]
        private List<(string UUId, Rank oldRank, Rank newRank)> rankChanges { get; set; } = new List<(string UUId, Rank oldRank, Rank newRank)>();

        #endregion

        public Tournament() {

        }

        public Tournament(DateTime startTime, string name, TournamentType format, int selector) {
            this.StartTime = startTime;
            this.Name = name;
            this.Format = format;
            this.Id = Player.RandomString(20);
            this.Index = selector;
        }

        public async Task<ChallongeTournament> SetUpChallonge() {
            var ct = new ChallongeTournament();
            ct.Name = this.Name;
            ct.TournamentType = TournamentTypeStr(this.Format);
            ct.StartAt = ChallongeConnection.ToChallongeTime(this.StartTime);

            ct = await Program.Challonge.CreateTournament(ct);

            this.ChallongeId = ct.Id;
            this.ChallongeUrl = ct.FullChallongeUrl;
            
            Program.Controller.SerializeTourneys();

            return ct;
        }

        public async Task RebuildIndex() {
            await RebuildParticipants(true);
            await RebuildMatches(true);

            await SendMessage();
            Program.Controller.SerializeTourneys();
        }

        public async Task<List<ChallongeParticipant>> RebuildParticipants(bool silent = false, bool order = false) {
            if (!IsChallongeLinked) return null;
            var participants = await Program.Challonge.GetParticipants((ulong)this.ChallongeId);
            // don't replace the actual value until it the integrity of the participants list is verified
            List<Team> newList = new List<Team>();
            foreach (var p in participants) {
                Team t = SkillCommands.StrToTeam(p.Name);
                t.ChallongeId = (ulong)p.Id;
                if (p.FinalRank != null) t.Ranking = (uint)p.FinalRank;
                newList.Add(t);
            }
            this.Teams = newList;

            if (order) {
                this.Teams = Teams.OrderBy(p => p.Ranking).ToList();
            }

            if (!silent) {
                await SendMessage();
                Program.Controller.SerializeTourneys();
            }

            return participants;
        }

        public async Task RebuildMatches(bool silent = true) {
            if (!IsChallongeLinked) return;
            var matches = await Program.Challonge.GetMatches((ulong)this.ChallongeId, "open");
            List<MatchInfo> newList = new List<MatchInfo>();

            foreach (var m in matches)
                newList.Add(
                    new MatchInfo((ulong)m.Id, (ulong)m.Player1Id, (ulong)m.Player2Id)
                );

            this.MatchInfos = newList;
        }

        public async Task SetIsActive(bool isActive) {
            IsActive = isActive;
            await SendMessage();


            if (isActive) {
                // store old player data

                foreach (var t in Teams) {
                    foreach (var p in t.Players) {
                        oldData.Add(new OldPlayerData() { UUId = p.UUId, Mu = p.Mu, Sigma = p.Sigma});
                    }
                }

                // update challonge
                if (IsChallongeLinked) {
                    await Program.Challonge.StartTournament((ulong)this.ChallongeId);
                    await RebuildIndex();
                }
            }
        }

        /// <summary>
        /// Whitelists a player.
        /// </summary>
        /// <param name="p">The player to whitelist.</param>
        /// <returns>Whether or not the player was removed from the blacklist automatically.</returns>
        public bool AllowPlayer(Player p) {
            if (!Allowlist.Contains(p.UUId)) Allowlist.Add(p.UUId);
            var result = Denylist.Remove(p.UUId);
            Program.Controller.SerializeTourneys();
            return result;
        }

        /// <summary>
        /// Blacklist a player.
        /// </summary>
        /// <param name="p">The player to blacklist.</param>
        /// <returns>Whether or not the player was removed from the whitelist automatically.</returns>
        public bool DenyPlayer(Player p) {
            if (!Denylist.Contains(p.UUId)) Denylist.Add(p.UUId);
            var result = Allowlist.Remove(p.UUId);
            Program.Controller.SerializeTourneys();
            return result;
        }


        /// <summary>
        /// Allows a player to sign up to a tournament by themselves.
        /// </summary>
        /// <param name="p">The player trying to sign up.</param>
        /// <returns>The result. 0 means no success, 1 means they were already in, 2 means no permission.</returns>
        public async Task<int> Signup(Player p, string message) {
            // check whitelist
            if (Program.Config.DenyByDefault) {
                if (!Allowlist.Contains(p.UUId)) return 2;
            } else {
                if (Denylist.Contains(p.UUId)) return 2;
            }

            var team = new Team();
            team.AddPlayer(p);

            return (await AddTeam(team, false, message)).Item1 ? 0 : 1;
        }


        public async Task<(bool, ChallongeParticipant)> AddTeam(Team t, bool silent = false, string message = null) {
            if (this.IsActive || this.IsCompleted) 
                throw new Exception("You cannot add teams to a tournament that is active or completed.");

            if (Teams.Contains(t)) return (false, null);
            Teams.Add(t);

            // Add to challonge
            ChallongeParticipant cp = null;
            
            if (IsChallongeLinked) {
                cp = new ChallongeParticipant();
                cp.Name = t.ToString();
                cp = await Program.Challonge.CreateParticipant((ulong)ChallongeId, cp);
                t.ChallongeId = (ulong)cp.Id;

                // rebuild for safety
                await RebuildIndex();
            }

            if (!silent) {
                await SendMessage();
            }

            // get signup log channel and send
            var chnl = Program.DiscordIO.GetChannel(Program.Config.SignupLogsChannelId);
            if (chnl != null) {
                var msg = $"**{t.ToString()}** joined the tournament **{this.Name}**";

                if (string.IsNullOrWhiteSpace(message)) msg += "!";
                else msg += ": " + message;

                await Program.DiscordIO.SendMessage("", chnl, EmbedHelper.GenerateInfoEmbed(
                    msg));
            }

            Program.Controller.SerializeTourneys();

            return (true, cp);
        }

        public async Task<bool> RemoveTeam(Team t, bool silent = false) {
            bool result = false;

            for (int i = 0; i < Teams.Count; ++i) {
                if (Teams[i].IsSameTeam(t)) {
                    var team = Teams[i];
                    Teams.RemoveAt(i);
                    if (IsChallongeLinked) {
                        await Program.Challonge.DeleteParticipant((ulong)ChallongeId, team.ChallongeId);
                        // rebuild for safety
                        await RebuildIndex();
                    }
                    result = true;
                    break;
                }
            }

            Program.Controller.SerializeTourneys();

            if (!silent)
                await SendMessage();

            return result;
        }

        /// <summary>
        /// Finds the Challonge match given two teams, then updates the original team's Challonge ID.
        /// </summary>
        /// <param name="team1">The first team.</param>
        /// <param name="team2">The second team.</param>
        /// <returns></returns>
        public async Task<MatchInfo> FindMatch(Team team1, Team team2) {

            await RebuildIndex();

            Team team1found = null;
            Team team2found = null;

            // find teams with the challonge id
            foreach (var t in Teams) {
                if (t.IsSameTeam(team1)) {
                    team1found = t;
                    team1.ChallongeId = t.ChallongeId;
                }
                if (t.IsSameTeam(team2)) {
                    team2found = t;
                    team2.ChallongeId = t.ChallongeId;
                } 
            }

            if (team1found == null || team2found == null) {
                throw new Exception("Could not find the specified teams within the tournament.");
            }

            MatchInfo matchFound = null;
            // find the match with the correct teams
            foreach (var m in MatchInfos) {
                if ((m.Team1 == team1found.ChallongeId && m.Team2 == team2found.ChallongeId) ||
                    (m.Team2 == team1found.ChallongeId && m.Team1 == team2found.ChallongeId)) {
                    matchFound = m;
                    break;
                }
            }

            return matchFound;
        }

        /// <summary>
        /// Marks a match as underway on Challonge.
        /// </summary>
        /// <param name="m">The pending match to mark as underway.</param>
        public async Task StartMatch(PendingMatch m) {
            var found = await FindMatch(m.Team1, m.Team2);
            await Program.Challonge.MarkMatchUnderway((ulong)this.ChallongeId, found.ChallongeId);
        }

        /// <summary>
        /// Adds a match. Throws an exception if the match was unable to be updated on Challonge. Can accept cancelled matches.
        /// </summary>
        /// <param name="m">The match to be added.</param>
        public async Task AddMatch(MatchAction m) {
            if (!m.IsCancelled) {
                this.Matches.Add(m);
                this.matchUUIds.Add(m.ActionId);
            }
            
            // update on challonge
            if (this.IsChallongeLinked) {
                var foundMatch = await FindMatch(m.Winner, m.Loser);
                if (foundMatch == null) {
                    throw new Exception("Could not find the given match on Challonge. Match change was not reported to Challonge.");
                }
                if (!m.IsDraw) {
                    if (!m.IsCancelled) {
                        // find winner and loser and update scores
                        var winnerId = foundMatch.Team2;
                        var loserId = foundMatch.Team1;

                        if (m.Winner.ChallongeId == foundMatch.Team1) {
                            ++foundMatch.Team1Score;
                            winnerId = foundMatch.Team1;
                            loserId = foundMatch.Team2;
                        } 
                        else ++foundMatch.Team2Score;

                        // upload to challonge
                        ChallongeMatch toSend = new ChallongeMatch();
                        toSend.WinnerId = winnerId;
                        toSend.ScoresCsv = $"{foundMatch.Team1Score}-{foundMatch.Team2Score}";

                        await Program.Challonge.UpdateMatch((ulong)this.ChallongeId, foundMatch.ChallongeId, toSend);
                    }
                    else {
                        await Program.Challonge.UnmarkMatchUnderway((ulong)this.ChallongeId, foundMatch.ChallongeId);
                    }
                }
            }
        }

        public void RemoveMatch(MatchAction m) {
            this.Matches.Remove(m);
            this.matchUUIds.Remove(m.ActionId);
        }

        // treat like a stack, but in reality cannot be a c# stack because of serialization
        public MatchAction PopMatch() {
            var i = this.Matches.Count - 1;
            var m = this.Matches[i];

            this.Matches.RemoveAt(i);
            this.matchUUIds.RemoveAt(i);

            return m;
        }


        /// <summary>
        /// Finalises the tournament.
        /// </summary>
        /// <returns>False if the match was not properly finalised on Challonge.</returns>
        public async Task<bool> FinaliseTournament(List<MatchRanking> rankings = null) {
            if (this.IsCompleted) return true;

            this.IsCompleted = true;

            // finalise on challonge
            if (IsChallongeLinked) {
                try {
                    await Program.Challonge.FinalizeTournament((ulong)this.ChallongeId);

                    // get podium if no rankings were provided
                    // todo: make faster than O(n*m)
                    if (rankings == null || rankings.Count == 0) {
                        var parts = await RebuildParticipants(false, true);
                    }
                }
                catch (Exception) {
                    await SendMessage();
                    return false;
                }
            }
            else {
                // todo: dry here
                if (rankings != null && rankings.Count != 0) {
                    foreach (var r in rankings) {
                        var found = this.Teams.FirstOrDefault(x => x.Equals(r.Team));
                        if (found == null) continue;
                        found.Ranking = r.Ranking;
                    }

                    this.Teams = this.Teams.OrderBy(p => p.Ranking).ToList();
                }
            }

            // get rank changes
            foreach (var op in oldData) {
                var p = Program.CurLeaderboard.FindPlayer(op.UUId);
                var oldRank = Player.GetRank(op.Mu, op.Sigma);
                var newRank = p.PlayerRank;

                if (!oldRank.Equals(newRank)) {
                    // add to rank changes
                    rankChanges.Add((p.UUId, oldRank, newRank));
                }
            }

            Program.Controller.SerializeTourneys();
            // move this tournaments to the completed section for archival
            Program.Controller.Tourneys.Tournaments.Remove(this);
            Program.Controller.Tourneys.CompletedTournaments.Add(this);
            Program.Controller.Tourneys.ActiveTournament = null;

            await SendMessage();

            return true;
        }

        [JsonProperty]
        private ulong messageId { get; set; }
        private RestUserMessage message;
        public async Task<RestUserMessage> GetMessage() {
            if (messageId == 0) return null;
            if (message == null) message = (RestUserMessage) await Program.DiscordIO.GetMessage(messageId, Program.Config.TourneysChannelId);

            return message;
        }

        public string GetTimeStr() {
            string[] months = {"Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"};
            return $"{months[StartTime.Month - 1]} {StartTime.Day}, {StartTime.Year} {StartTime.Hour.ToString("00")}:{StartTime.Minute.ToString("00")}UTC";
        }

        public Embed GetEmbed(bool forStaff = false) {
            var eb = new EmbedBuilder()
                .WithFooter($"ID: {Id}")
                .WithColor(IsCompleted ? Discord.Color.LightGrey : (IsActive ? Discord.Color.Purple : Discord.Color.Blue));

            if (!this.IsActive && !this.IsCompleted) eb.Title = $":crossed_swords: [{this.Index}] {this.Name}";
            else eb.Title = ":crossed_swords: " + this.Name;

            eb.AddField("Time", GetTimeStr(), true);
            eb.AddField("Format", this.Format.ToString(), true);
            eb.AddField("Players", 
                (this.Teams == null || this.Teams.Count == 0) ? "Nobody has signed up yet." : string.Join(Environment.NewLine, this.Teams.Select(p => p.GetPodiumString())));

            if (IsCompleted && rankChanges != null && rankChanges.Count != 0) {
                var sb = new StringBuilder();
                foreach (var rc in rankChanges) {
                    sb.Append($"**{Program.CurLeaderboard.FindPlayer(rc.UUId).IGN}**: *{rc.oldRank.Name}* → *{rc.newRank.Name}*{Environment.NewLine}");
                }
                eb.AddField("Rank Changes", sb.ToString(), true);
            }

            if (forStaff) {
                eb.AddField("Allowed list", this.Allowlist == null || this.Allowlist.Count == 0 ? "Nothing here." : 
                    string.Join(Environment.NewLine, this.Allowlist.Select(p => Program.CurLeaderboard.FindPlayer(p).IGN)), true);
                eb.AddField("Denied list", this.Denylist == null || this.Denylist.Count == 0 ? "Nothing here." : 
                    string.Join(Environment.NewLine, this.Denylist.Select(p => Program.CurLeaderboard.FindPlayer(p).IGN)), true);
            }

            if (IsChallongeLinked)
                eb.AddField($"Bracket", ChallongeUrl);

            return eb.Build();
        }

        public async Task SendMessage() {
            var msg = await GetMessage();
            if (msg == null) {
                message = await Program.DiscordIO.SendMessage("", Program.Config.GetTourneysChannel(), GetEmbed());
                messageId = message.Id;
                Program.Controller.SerializeTourneys();
            }
            else {
                await Program.DiscordIO.EditMessage(msg, "", GetEmbed());
            }
        }

        public async Task DeleteMessage() {
            RestUserMessage message;
            if ((message = await GetMessage()) == null) return;
            await message.DeleteAsync();
        }


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

            // TODO: write your implementation of Equals() here
            return this.Id.Equals(((Tournament)obj).Id);
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            // TODO: write your implementation of GetHashCode() here
            // throw new System.NotImplementedException();
            return 13 * StartTime.GetHashCode() + 13 * ChallongeId.GetHashCode() + 7 * Name.GetHashCode() + 7 * Teams.GetHashCode() + 3 * Id.GetHashCode();
        }

        public static string TournamentTypeStr(TournamentType type) {
            switch (type) {
                case TournamentType.DoubleElim:
                    return "double elimination";
                case TournamentType.SingleElim:
                    return "single elimination";
                case TournamentType.Swiss:
                    return "swiss";
                case TournamentType.RoundRobin:
                    return "round robin";
            }
            return null;
        }


        public static Tournament GenerateTournament(string name, ushort utcTime, string calendarDate, string format="double_elimimation") {
                        var now = DateTime.UtcNow;

            // parse date
            int[] date = {now.Day, now.Month, now.Year};
            var dateSpl = calendarDate.Split('/', 3, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < dateSpl.Length; ++i) {
                date[i] = Convert.ToInt32(dateSpl[i]);
            }

            // Auto date
            var time = new DateTime(
                date[2], date[1], date[0],
                (utcTime / 100) % 24,
                (utcTime % 100) % 60,
                0);

            if (dateSpl.Length >= 3 && time < now) {
                throw new Exception("The tournament cannot be in the past.");
            }

            DateTime AddDays(DateTime time, int days) {
                return time.AddDays(days);
            }
            DateTime AddMonths(DateTime time, int months) {
                return time.AddMonths(months);
            }
            DateTime AddYears(DateTime time, int years) {
                return time.AddYears(years);
            }

            // Delegate function to add the required amount of time given how many parameters were given
            Func<DateTime, int, DateTime> Add;
            switch (dateSpl.Length) {
                case 0:
                    Add = AddDays;
                break;
                case 1:
                    Add = AddMonths;
                break;
                case 2:
                    Add = AddYears;
                break;
                default: // default case to prevent compiler from complaining of missing case
                    Add = (a,b) => DateTime.Now;
                    break;
            }

            // Keep adding until the tournament is in the future.
            while (time < now) {
                time = Add(time, 1);
            }

            TournamentType type;
            // parse tournament type, use fuzzy matching
            switch (format[0]) {
                case 's':
                    if (format.Length == 1 || format[1] == 'i') type = TournamentType.SingleElim;
                    else if (format[1] == 'w') type = TournamentType.Swiss;
                    else throw new Exception($"Tournament type \"{format}\" not recognised.");
                break;
                case 'd':
                    type = TournamentType.DoubleElim;
                break;
                case 'r':
                    type = TournamentType.RoundRobin;
                break;
                default:
                    throw new Exception($"Tournament type \"{format}\" not recognised.");
            }

            return new Tournament(time, name, type, Program.Controller.Tourneys.Tournaments.Count + 1);
        }
    }
}