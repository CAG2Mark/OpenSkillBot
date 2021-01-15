using System.Collections.Generic;
using Moserware.Skills;
using Moserware.Skills.TrueSkill;
using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Rest;
using Newtonsoft.Json;
using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;

namespace OpenTrueskillBot.Skill
{

    public class Team {
        public List<Player> Players = new List<Player>();

        public bool IsSameTeam(Team t) {
            // O(n) solution for checking equality
            // Idea from: https://stackoverflow.com/questions/14236672/fastest-way-to-check-if-two-listt-are-equal
            Dictionary<string, int> hash = new Dictionary<string, int>();
            foreach (var p in Players) {
                if (hash.ContainsKey(p.UUId)) ++hash[p.UUId];
                else hash.Add(p.UUId, 1);
            } 
            foreach (var p in t.Players) {
                if (!hash.ContainsKey(p.UUId) || hash[p.UUId] == 0) return false;
                --hash[p.UUId];
            } 
            return true;
        }
    }


    public struct OldPlayerData {
        public string UUId;
        public double Sigma;
        public double Mu;
    }

    public class MatchAction
    {
        public static Team UUIDListToTeam(IEnumerable<string> uuids) {
            var t = new Team();
            foreach (var uuid in uuids)
            {
                t.Players.Add(Program.CurLeaderboard.FindPlayer(uuid));
            }
            
            return t;
        }

        [JsonIgnoreAttribute]
        public Team Winner { 
            get {
                if (winner == null) winner = UUIDListToTeam(winnerUUIDs);
                return winner;
            }
            set => winner = value; 
        }

        [JsonProperty]
        private IEnumerable<string> winnerUUIDs = new List<string>();

        [JsonIgnoreAttribute]
        public Team Loser { 
            get {
                if (loser == null) loser = UUIDListToTeam(loserUUIds);
                return loser;
            }
            set => loser = value; 
        }

        [JsonProperty]
        private IEnumerable<string> loserUUIds = new List<string>();

        public bool IsDraw { get; set; }

        [JsonProperty]
        public bool IsTourney { get; private set; } = false;

        private async Task sendMessage()
        {
            if (Program.Config.HistoryChannelId == 0) return;

            // generate message
            var embed = MessageGenerator.MakeMatchMessage(this, this.IsDraw);
            var chnl = Program.Config.GetHistoryChannel();
            if (this.discordMessageId == 0)
            {
                this.discordMessageId = (await Program.DiscordIO.SendMessage("", chnl, embed)).Id;
            }
            else
            {
                var msg = (RestUserMessage)await chnl.GetMessageAsync(this.discordMessageId);

                await Program.DiscordIO.EditMessage(msg, "", embed);
            }
        }
        protected async Task action()
        {
            TrueskillWrapper.CalculateMatch(this.Winner.Players, this.Loser.Players, this.IsDraw);
            await sendMessage();
            // undeafen the users
            foreach (var p in Winner.Players) await p.Undeafen();
            foreach (var p in Loser.Players) await p.Undeafen();
        }

        protected void undoAction()
        {
        }

        // Currently only supports matches between two teams
        public MatchAction(Team winner, Team loser, bool isDraw = false, bool isTourney = false)
        {
            ActionTime = DateTime.UtcNow;
            this.ActionId = Player.RandomString(16);

            this.Winner = winner;
            this.Loser = loser;
            this.IsDraw = isDraw;

            this.IsTourney = isTourney;

            this.winnerUUIDs = winner.Players.Select(p => p.UUId);
            this.loserUUIds = loser.Players.Select(p => p.UUId);

            setOldPlayerDatas();
        }

        private void setOldPlayerDatas()
        {

            OldPlayerDatas = new List<OldPlayerData>();

            foreach (var p in Winner.Players)
            {
                OldPlayerDatas.Add(new OldPlayerData() { Sigma = p.Sigma, Mu = p.Mu, UUId = p.UUId });
            }
            foreach (var p in Loser.Players)
            {
                OldPlayerDatas.Add(new OldPlayerData() { Sigma = p.Sigma, Mu = p.Mu, UUId = p.UUId });
            }
        }

        // empty ctor for serialisation purposes
        public MatchAction()
        {
            //ActionTime = DateTime.UtcNow;
            //this.ActionId = Player.RandomString(16);
        }

        #region copied from botaction class

        public List<OldPlayerData> OldPlayerDatas = new List<OldPlayerData>();

        public DateTime ActionTime;
        private Team winner;
        private Team loser;


        // Dont serialise this to avoid infinite recursion. Instead, repopulate on deserialization.
        [JsonIgnore]
        public MatchAction NextAction { get; set; }
        public MatchAction PrevAction { get; set; }

        [JsonProperty]
        public string ActionId { get; private set; }

        [JsonProperty]
        private ulong discordMessageId { get; set; } = 0;

        public async Task DoAction(bool invokeChange = true)
        {
            setOldPlayerDatas();
            await action();
            if (invokeChange) {
                Program.CurLeaderboard.InvokeChange(Winner.Players.Count + Loser.Players.Count);
            }
        }


        private int mergeForwardOld()
        {
            var data = getCumulativeOldData();
            Program.CurLeaderboard.MergeOldData(data);
            Console.WriteLine("Merged old data");
            return data.Count;
        }

        public async Task Undo()
        {
            int count = this.Winner.Players.Count + this.Loser.Players.Count;

            count += mergeForwardOld();
            // undoAction() just does extra things that may not be covered by the default one
            undoAction();

            // recalculate future values
            if (NextAction != null)
            {
                await NextAction.ReCalculateNext();
            }

            // Unlink this node
            var tempPrev = PrevAction;
            var tempNext = NextAction;
            if (PrevAction != null)
            {
                tempPrev.NextAction = tempNext;
            }
            if (NextAction != null)
            {
                tempNext.PrevAction = tempPrev;
            }

            Program.CurLeaderboard.InvokeChange(count);

            await deleteMessage();
        }

        public async Task ReCalculateSelf() {
            int count = Winner.Players.Count + Loser.Players.Count;
            if (this.NextAction != null)
            {
                count += this.mergeForwardOld();
            }

            await ReCalculateNext();

            Program.Controller.SerializeActions();
            Program.CurLeaderboard.InvokeChange(count);
        }

        public async Task InsertAfter(MatchAction action)
        {
            int count = action.Winner.Players.Count + action.Loser.Players.Count;
            if (this.NextAction != null)
            {
                count += this.NextAction.mergeForwardOld();
            }

            action.NextAction = this.NextAction;
            action.PrevAction = this;
            this.NextAction = action;

            await action.ReCalculateAndReSend();

            Program.CurLeaderboard.InvokeChange(count);
            Program.Controller.SerializeActions();
        }

        public async Task InsertBefore(MatchAction action)
        {
            int count = action.Winner.Players.Count + action.Loser.Players.Count;

            action.PrevAction = this.PrevAction;
            action.NextAction = this;
            this.PrevAction = action;

            count += this.mergeForwardOld();

            await action.ReCalculateAndReSend();

            Program.CurLeaderboard.InvokeChange(count);
            Program.Controller.SerializeActions();
        }

        private async Task deleteMessage()
        {
            if (Program.Config.HistoryChannelId == 0 || this.discordMessageId == 0) return;
            // delete message
            var msg = (RestUserMessage)await Program.DiscordIO.GetMessage(this.discordMessageId, Program.Config.HistoryChannelId);
            if (msg != null)
            {
                this.discordMessageId = 0;
                await msg.DeleteAsync();
            }
        }

        #region Recursive functions

        public Tuple<MatchAction, int> FindMatch(string id, int depth = 1)
        {
            // goes backwards
            if (this.ActionId.Equals(id)) return new Tuple<MatchAction, int>(this, depth);
            else
            {
                if (PrevAction != null) return this.PrevAction.FindMatch(id, depth + 1);
                else return new Tuple<MatchAction, int>(null, depth);
            }
        }


        public async Task ReCalculateAndReSend()
        {
            await deleteMessage();
            await DoAction(false);
            if (this.NextAction != null)
            {
                await this.NextAction.ReCalculateAndReSend();
            }
        }
        public async Task ReCalculateNext()
        {
            await DoAction(false);

            if (this.NextAction != null)
            {
                await this.NextAction.ReCalculateNext();
            }
        }


        /// <summary>
        /// Returns all the cumulative old player data for recalculation of TrueSkill.
        /// </summary>
        /// <returns></returns>
        private List<OldPlayerData> getCumulativeOldData()
        {
            List<OldPlayerData> cumulative;

            // If this is the head, start the chain
            if (this.NextAction == null)
            {
                cumulative = new List<OldPlayerData>();
            }
            else
            {
                cumulative = NextAction.getCumulativeOldData();
            }

            // remove the later old player datas
            // because we want the one closest to the start
            foreach (var oldPlayerData in OldPlayerDatas)
            {
                cumulative.RemoveAll(o => o.UUId.Equals(oldPlayerData.UUId));
            }

            cumulative.AddRange(OldPlayerDatas);

            return cumulative;
        }

        #endregion

        public void RepopulateLinks()
        {
            if (this.PrevAction != null)
            {
                PrevAction.NextAction = this;
                PrevAction.RepopulateLinks();
            }
        }

        #endregion

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

            return this.ActionId.Equals(((MatchAction)obj).ActionId);
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return 7 * this.Winner.GetHashCode() + 17 * this.Loser.GetHashCode() + 19 * this.ActionId.GetHashCode();
        }
    }

    public static class TrueskillWrapper {
        public static GameInfo info = new GameInfo(
            Program.Config.DefaultMu,
            Program.Config.DefaultSigma,
            Program.Config.Beta,
            Program.Config.Tau,
            Program.Config.DrawProbability
            );

        private static TwoTeamTrueSkillCalculator calculator = new TwoTeamTrueSkillCalculator();

        public static Tuple<
            IEnumerable<Dictionary<Moserware.Skills.Player, Rating>>,
            int[]
            > 
        ConvertToMoserTeams(IEnumerable<Tuple<IEnumerable<Player>, int>> teams) {

            var mosTeams = new List<Dictionary<Moserware.Skills.Player, Rating>>();
            var teamStates = new List<int>();

            foreach (var val in teams) {
                var mosTeam = new Dictionary<Moserware.Skills.Player, Rating>();
                var team = val.Item1;
                foreach(var player in team) {
                    mosTeam.Add(new Moserware.Skills.Player(player.UUId), new Rating(player.Mu, player.Sigma));
                }
                mosTeams.Add(mosTeam);
                
                teamStates.Add(val.Item2);
            }

            return new Tuple<IEnumerable<Dictionary<Moserware.Skills.Player, Rating>>, int[]>(mosTeams, teamStates.ToArray());
        }

        public static void CalculateMatch(IEnumerable<Player> winTeam, IEnumerable<Player> loseTeam, bool isDraw = false) {

            var teams = new Tuple<IEnumerable<Player>, int>[] { 
                new Tuple<IEnumerable<Player>, int>(winTeam, 0), 
                new Tuple<IEnumerable<Player>, int>(loseTeam, 1)};

            var mosTeams = ConvertToMoserTeams(teams);

            // calculate ratings
            var results = calculator.CalculateNewRatings(info, mosTeams.Item1, isDraw ? new int[] {1, 1} : mosTeams.Item2);
            
            foreach (var result in results) {
                var id = (string)result.Key.Id;

                // search for the player on each team
                foreach (var val in teams)
                {
                    // team state is now redundant
                    var team = val.Item1;
                    foreach (var player in team)
                    {
                        if (player.UUId.Equals(id)) {

                            player.Mu = result.Value.Mean;
                            player.Sigma = result.Value.StandardDeviation;
                        }
                    }
                }
            }
        }
    }
}