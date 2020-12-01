using OpenTrueskillBot.Skill;
using System;
using Newtonsoft.Json;
using System.IO;
using Discord;
using System.Linq;
using System.Timers;

namespace OpenTrueskillBot
{
    public class BotController
    {

        private const string lbFileName = "leaderboard.json";
        private const string ahFileName = "actionhistory.json";

        public Leaderboard CurLeaderboard;

        private Timer lbTimer = new Timer(3000);

        private bool lbChangeQueued = false; 

        public MatchAction LatestAction;
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
                LatestAction = SerializeHelper.Deserialize<MatchAction>(ahFileName);
                if (LatestAction != null)
                    LatestAction.RepopulateLinks();
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

                await Program.DiscordIO.PopulateChannel(Program.Config.LeaderboardChannelId, lbStrArr);

            }

            
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

            if (LatestAction != null) {
                // note: this does recalculate
                LatestAction.InsertAfter(action);
            }
            else {
                action.DoAction();
            }

            LatestAction = action;

            SerializeActions();

            return action;

        }
    }
}