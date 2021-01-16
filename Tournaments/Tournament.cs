using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Newtonsoft.Json;
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
        public Tournament()
        {

        }

        public Tournament(DateTime startTime, string name, TournamentType format)
        {
            this.StartTime = startTime;
            this.Name = name;
            this.Format = format;
            this.Id = Player.RandomString(20);
        }

        public TournamentType Format { get; set; }
        public DateTime StartTime { get; set; }
        public string Name { get; set; }
        // Challonge not yet implemeted
        public string ChallongeId { get; private set; }

        [JsonProperty]
        private List<string> playerUUIds { get; set; } = new List<string>();

        private List<Player> players;

        [JsonIgnore]
        public List<Player> Players
        {
            get
            {
                if (players == null)
                {
                    players = MatchAction.UUIDListToTeam(playerUUIds).Players;
                }
                return players;
            }
        }

        [JsonProperty]
        public string Id { get; private set; }

        [JsonProperty]
        public bool IsActive { get; private set; }

        public async Task SetIsActive(bool isActive) {
            IsActive = isActive;
            await SendMessage();
        }

        public async Task AddPlayer(Player p, bool silent = false)
        {
            Players.Add(p);
            playerUUIds.Add(p.UUId);
            if (!silent)
                await SendMessage();
        }

        public async Task RemovePlayer(Player p, bool silent = false)
        {
            Players.Remove(p);
            playerUUIds.Remove(p.UUId);
            if (!silent)
                await SendMessage();
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
                .WithFooter(Id)
                .WithColor(IsActive ? Discord.Color.Green : Discord.Color.Blue)
                .WithTitle(":crossed_swords: " + this.Name);

            eb.AddField("Time", GetTimeStr(), true);
            eb.AddField("Format", this.Format.ToString(), true);
            eb.AddField("Players", this.players.Count == 0 ? "Nobody has signed up yet." : string.Join(", ", this.Players.Select(p => p.IGN)));

            return eb.Build();
        }

        public async Task SendMessage() {
            var msg = await GetMessage();
            if (msg == null) {
                message = await Program.DiscordIO.SendMessage("", Program.Config.GetTourneysChannel(), GetEmbed());
                messageId = message.Id;
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
            return 13 * StartTime.GetHashCode() + 13 * ChallongeId.GetHashCode() + 7 * Name.GetHashCode() + 7 * Players.GetHashCode() + 3 * Id.GetHashCode();
        }
    }
}