using System.Collections.Generic;
using Moserware.Skills;
using Moserware.Skills.TrueSkill;
using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Rest;
using Newtonsoft.Json;
using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;
using OpenSkillBot.Serialization;

namespace OpenSkillBot.Skill {

    public class OldPlayerData {
        public string UUId;
        public double Sigma;
        public double Mu;

        public uint DecayCycle;

        public uint LastDecay;
    }

    public class MatchAction : BotAction
    {
        public static Team UUIDListToTeam(IEnumerable<string> uuids) {
            var t = new Team();
            t.Players = UUIDListToPlayers(uuids).ToArray();

            return t;
        }


        public static List<Player> UUIDListToPlayers(IEnumerable<string> uuids) {
            var t = new List<Player>();
            foreach (var uuid in uuids)
            {
                t.Add(Program.CurLeaderboard.FindPlayer(uuid));
            }
            
            return t;
        }


        public static List<BotAction> UUIDListToMatches(IEnumerable<string> uuids) {
            var m = new List<BotAction>();
            foreach (var uuid in uuids)
            {
                m.Add(Program.Controller.FindAction(uuid));
            }
            
            return m;
        }

        public Team Winner { get; set; }

        public Team Loser { get; set; }

        public bool IsDraw { get; set; }


        [JsonProperty]
        public bool IsTourney { get; private set; } = false;
        public string TourneyId { get; set; }

        protected override async Task sendMessage()
        {
            if (Program.Config.HistoryChannelId == 0) return;

            // generate message
            var embed = MessageGenerator.MakeMatchMessage(this);
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

        public async Task UndeafenPlayers() {
            foreach (var p in Winner.Players) await p.Undeafen();
            foreach (var p in Loser.Players) await p.Undeafen();
        }

        protected override async Task action()
        {
            SkillWrapper.CalculateMatch(this.Winner.Players, this.Loser.Players, this.IsDraw);
            // undeafen the users
            await UndeafenPlayers();
        }

        protected override void undoAction()
        {
        }

        // Currently only supports matches between two teams
        public MatchAction(Team winner, Team loser, bool isDraw = false, bool isTourney = false, bool cancelled = false) : base()
        {

            this.Winner = winner;
            this.Loser = loser;
            this.IsDraw = isDraw;

            this.IsTourney = isTourney;

            this.IsCancelled = IsCancelled;

            setOldPlayerDatas();
        }

        protected override void setOldPlayerDatas()
        {

            OldPlayerDatas = new List<OldPlayerData>();

            foreach (var p in Winner.Players)
            {
                OldPlayerDatas.Add(new OldPlayerData() { Sigma = p.Sigma, Mu = p.Mu, UUId = p.UUId, DecayCycle = p.DecayCycle });
            }
            foreach (var p in Loser.Players)
            {
                OldPlayerDatas.Add(new OldPlayerData() { Sigma = p.Sigma, Mu = p.Mu, UUId = p.UUId, DecayCycle = p.DecayCycle });
            }
        }

        // empty ctor for serialisation purposes
        public MatchAction()
        {
            //ActionTime = DateTime.UtcNow;
            //this.ActionId = Player.RandomString(16);
        }

        Func<ActionContainer, bool> predicate => p => p.Action.Equals(this);

        protected override void addToPlayerActions() {
            foreach (var p in this.Winner.Players) {
                p.Actions.Insert(predicate, new ActionContainer(this));
            }
            foreach (var p in this.Loser.Players) {
                p.Actions.Insert(predicate, new ActionContainer(this));
            }
        }
        protected override void removeFromPlayerActions() {
            foreach (var p in this.Winner.Players) {
                p.Actions.Delete(predicate);
            }
            foreach (var p in this.Loser.Players) {
                p.Actions.Delete(predicate);
            }
        }


        protected override int getChangeCount() {
            return Winner.Players.Count() + Loser.Players.Count();
        }


        protected async override Task deleteMessage()
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

        public async Task<int> Edit(Team winner, Team loser, bool isDraw) {
            removeFromPlayerActions();

            this.Winner = winner;
            this.Loser = loser;

            addToPlayerActions();

            this.IsDraw = isDraw;
            return await this.ReCalculateSelf() - 1;
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

            return this.ActionId.Equals(((MatchAction)obj).ActionId);
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return 7 * this.Winner.GetHashCode() + 17 * this.Loser.GetHashCode() + 19 * this.ActionId.GetHashCode();
        }

        public override string ToString()
        {
            return this.Winner.ToString() + " vs " + this.Loser.ToString();
        }
    }

    public static class SkillWrapper {
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
            var results = GetMatchResult(winTeam, loseTeam, isDraw);
            foreach (var val in results.Keys) {
                var mu = results[val].Mu;
                var sigma = results[val].Sigma;

                val.Mu = mu;
                val.Sigma = sigma;
            }
        }

        public static IDictionary<Player, (double Mu, double Sigma)> GetMatchResult(IEnumerable<Player> winTeam, IEnumerable<Player> loseTeam, bool isDraw = false) {
            var teams = new Tuple<IEnumerable<Player>, int>[] { 
                new Tuple<IEnumerable<Player>, int>(winTeam, 0), 
                new Tuple<IEnumerable<Player>, int>(loseTeam, 1)};

            Dictionary<Player, (double Mu, double Sigma)> returns = new Dictionary<Player, (double Mu, double Sigma)>();

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
                            returns.Add(player, (result.Value.Mean, result.Value.StandardDeviation));
                        }
                    }
                }
            }

            return returns;
        }

        public static (double Mu, double Sigma) Decay(Player original) {
            uint t = original.DecayCycle / Program.Config.DecayCyclesUntilDecay;

            double mu = original.Mu;
            double sigma = original.Sigma;


            double ogSigma = sigma / Gompertz(original.LastDecay / Program.Config.decayCyclesUntilDecay);
            double ogMu = mu + Program.Config.TrueSkillDeviations * (sigma - ogSigma);

            double newSigma = ogSigma * Gompertz(t);

            double newMu = ogMu - Program.Config.TrueSkillDeviations * (newSigma - ogSigma);

            return (newMu, newSigma);
        }

        private static double Gompertz(uint t) {
            if (t == 0) return 1;
            return Math.Exp(-10 * Math.Exp(-0.6 * t)) * 17 / 18 + 1;
        }
    }
}