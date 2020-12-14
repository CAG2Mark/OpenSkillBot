using System.Collections.Generic;
using Moserware.Skills;
using Moserware.Skills.TrueSkill;
using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Discord.Rest;
using Newtonsoft.Json;
using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;

namespace OpenTrueskillBot.Skill
{

    public struct Team {
        public List<Player> Players;
    }


    public struct OldPlayerData {
        public string UUId;
        public double Sigma;
        public double Mu;
    }

    public class MatchAction
    {
        public Team Winner { get; set; }
        public Team Loser { get; set; }
        public bool IsDraw { get; set; }
        
        protected async Task action()
        {
            TrueskillWrapper.CalculateMatch(this.Winner.Players, this.Loser.Players, this.IsDraw);
            
            if (Program.Config.HistoryChannelId == 0) return;

            // generate message
            var embed = MessageGenerator.MakeMatchMessage(this, this.IsDraw);
            var chnl = Program.DiscordIO.GetChannel(Program.Config.HistoryChannelId);
            if (this.discordMessageId == 0) {
                this.discordMessageId = (await Program.DiscordIO.SendMessage("", chnl, embed)).Id;
            }
            else {
                var msg = (RestUserMessage)await chnl.GetMessageAsync(this.discordMessageId);
                await Program.DiscordIO.EditMessage(msg, "", embed);
            }
        }

        protected void undoAction()
        {
        }

        // Currently only supports matches between two teams
        public MatchAction(Team winner, Team loser, bool isDraw = false) : base() {
            ActionTime = DateTime.UtcNow;
            this.ActionId = Player.RandomString(16);

            this.Winner = winner;
            this.Loser = loser;
            this.IsDraw = isDraw;

            foreach (var p in winner.Players) {
                oldPlayerDatas.Add(new OldPlayerData() { Sigma = p.Sigma, Mu = p.Mu, UUId = p.UUId});
            }
            foreach (var p in loser.Players) {
                oldPlayerDatas.Add(new OldPlayerData() { Sigma = p.Sigma, Mu = p.Mu, UUId = p.UUId});
            }
        }

        // empty ctor for serialisation purposes
        public MatchAction() {
            ActionTime = DateTime.UtcNow;
            this.ActionId = Player.RandomString(16);
        }

        #region copied from botaction class

        public List<OldPlayerData> oldPlayerDatas = new List<OldPlayerData>();

        public DateTime ActionTime;

        
        // Dont serialise this to avoid infinite recursion. Instead, repopulate on deserialization.
        [JsonIgnore]
        public MatchAction NextAction { get; set; }
        public MatchAction PrevAction { get; set; }

        public string ActionId { get; private set; }

        [JsonProperty]
        private ulong discordMessageId { get; set; } = 0;

        public async Task DoAction()
        {
            await action();
            Program.CurLeaderboard.InvokeChange();
        }


        private void mergeAllOld()
        {
            Program.CurLeaderboard.MergeOldData(getCumulativeOldData());
        }

        public async Task Undo()
        {
            mergeAllOld();
            // undoAction() just does extra things that may not be covered by the default one
            undoAction();

            // recalculate future values
            if (NextAction != null)
            {
                await NextAction.ReCalculateNext();
            }
            else
            {
                Program.CurLeaderboard.InvokeChange();
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

            Program.Controller.SerializeActions();

            if (Program.Config.HistoryChannelId == 0 || this.discordMessageId == 0) return;
            // delete message
            var msg = (RestUserMessage) await Program.DiscordIO.GetMessage(this.discordMessageId, Program.Config.HistoryChannelId);
            if (msg != null)
                await msg.DeleteAsync();
        }

        public async Task InsertAfter(MatchAction action)
        {
            if (this.NextAction != null)
            {
                this.NextAction.mergeAllOld();
            }
            else
            {

            }

            action.NextAction = this.NextAction;
            action.PrevAction = this;
            this.NextAction = action;

            await action.ReCalculateNext();

            Program.Controller.SerializeActions();
        }

        #region Recursive functions

        public Tuple<MatchAction, int> FindMatch(string id, int depth = 1) {
            // goes backwards
            if (this.ActionId.Equals(id)) return new Tuple<MatchAction, int>(this, depth);
            else return this.PrevAction.FindMatch(id, depth + 1);
        }

        public async Task ReCalculateNext()
        {

            await DoAction();

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
            foreach (var oldPlayerData in oldPlayerDatas)
            {
                cumulative.RemoveAll(o => o.UUId.Equals(oldPlayerData.UUId));
            }

            cumulative.AddRange(oldPlayerDatas);

            return cumulative;
        }

        #endregion
        
        public void RepopulateLinks() {
            if (this.PrevAction != null) {
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