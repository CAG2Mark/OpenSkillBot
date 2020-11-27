using OpenTrueskillBot.Skill;
using System;
using Newtonsoft.Json;
using System.IO;

namespace OpenTrueskillBot
{
    public class BotController
    {

        private const string lbFileName = "leaderboard.json";
        private const string ahFileName = "actionhistory.json";

        public Leaderboard CurLeaderboard;

        public BotAction LatestAction;
        public BotController() {
            // get leaderboard
            if (File.Exists(lbFileName)) {
                CurLeaderboard = SerializeHelper.Deserialize<Leaderboard>(lbFileName);
            }
            else {
                CurLeaderboard = new Leaderboard();
                UpdateLeaderboard();
            }

            // get action history
            if (File.Exists(ahFileName)) {
                LatestAction = SerializeHelper.Deserialize<BotAction>(ahFileName);
            }

            CurLeaderboard.LeaderboardChanged += (o,e) => {
                UpdateLeaderboard();
            };
        }

        public void UpdateLeaderboard() {
            Console.WriteLine("Leaderboard changed");
            SerializeLeaderboard();
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
            try
            {
                SerializeHelper.Serialize(LatestAction, ahFileName);
                return true;
            }
            catch (System.Exception)
            {
                Console.WriteLine("WARNING: Failed to save action history!");
                return false;
            }
        }

        public MatchAction AddMatchAction(Team team1, Team team2, int result) {
            // swap if result is 2
            if (result == 2) {
                var temp = team1;
                team1 = team2;
                team2 = temp;
            }

            MatchAction action = new MatchAction(team1, team2, result == 0);
            // note: this does recalculate
            if (LatestAction != null) {
                LatestAction.InsertAfter(action);
            }
            else {
                action.DoAction();
                SerializeActions();
            }
            LatestAction = action;

            return action;

        }
    }
}