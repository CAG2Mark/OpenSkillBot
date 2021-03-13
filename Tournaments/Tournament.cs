using System;
using System.Collections.Generic;
using System.Linq;
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

    public class Tournament
    {

        #region properties

        public TournamentType Format { get; set; }
        public DateTime StartTime { get; set; }
        public string Name { get; set; }

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

        [JsonProperty]
        public string Id { get; private set; }

        [JsonProperty]
        public bool IsActive { get; private set; }

        #endregion

        public Tournament() {

        }

        public Tournament(DateTime startTime, string name, TournamentType format) {
            this.StartTime = startTime;
            this.Name = name;
            this.Format = format;
            this.Id = Player.RandomString(20);
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

        public async Task RebuildParticipantsList() {
            if (!IsChallongeLinked) return;
            var participants = await Program.Challonge.GetParticipants((ulong)this.ChallongeId);
            // don't replace the actual value until it the integrity of the participants list is verified
            List<Team> newList = new List<Team>();
            foreach (var p in participants) {
                Team t = SkillCommands.strToTeam(p.Name);
                t.ChallongeId = (ulong)p.Id;
                newList.Add(t);
            }
            this.Teams = newList;

            await SendMessage();
            Program.Controller.SerializeTourneys();
        }

        public async Task SetIsActive(bool isActive) {
            IsActive = isActive;
            await SendMessage();
        }

        public async Task<(bool, ChallongeParticipant)> AddTeam(Team t, bool silent = false) {
            if (Teams.Contains(t)) return (false, null);
            Teams.Add(t);
 
            if (!silent)
                await SendMessage();

            // Add to challonge
            ChallongeParticipant cp = null;
            
            if (IsChallongeLinked) {
                cp = new ChallongeParticipant();
                cp.Name = t.ToString();
                cp = await Program.Challonge.CreateParticipant((ulong)ChallongeId, cp);
                t.ChallongeId = (ulong)cp.Id;

                // rebuild for safety
                await RebuildParticipantsList();
            }

            Program.Controller.SerializeTourneys();

            return (true, cp);
        }

        public async Task<bool> RemoveTeam(Team t, bool silent = false) {
            bool result = false;

            for (int i = 0; i < Teams.Count; ++i) {
                if (Teams[i].Equals(t)) {
                    var team = Teams[i];
                    Teams.RemoveAt(i);
                    if (IsChallongeLinked)
                        await Program.Challonge.DeleteParticipant((ulong)ChallongeId, team.ChallongeId);
                        // rebuild for safety
                        await RebuildParticipantsList();
                    result = true;
                    break;
                }
            }

            Program.Controller.SerializeTourneys();

            if (!silent)
                await SendMessage();

            return result;
        }

        public void AddMatch(MatchAction m) {
            this.Matches.Add(m);
            this.matchUUIds.Add(m.ActionId);
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

        public Embed GetEmbed() {
            var eb = new EmbedBuilder()
                .WithFooter($"ID: {Id}")
                .WithColor(IsActive ? Discord.Color.Green : Discord.Color.Blue)
                .WithTitle(":crossed_swords: " + this.Name);

            eb.AddField("Time", GetTimeStr(), true);
            eb.AddField("Format", this.Format.ToString(), true);
            eb.AddField("Players", this.Teams == null || this.Teams.Count == 0 ? "Nobody has signed up yet." : string.Join(Environment.NewLine, this.Teams.Select(p => p.ToString())));

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
                    if (format[1] == 'i') type = TournamentType.SingleElim;
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

            return new Tournament(time, name, type);
        }
    }
}