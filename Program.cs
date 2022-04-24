using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using OpenSkillBot.BotInputs;
using OpenSkillBot.ChallongeAPI;
using OpenSkillBot.Skill;
using OpenSkillBot.Serialization;
using System.Linq;


// Perms integer: 29486144
// Invite: https://discord.com/api/oauth2/authorize?client_id=<INSERT CLIENT ID>&permissions=29486144&scope=bot

namespace OpenSkillBot
{
    class Program
    {

        public const char prefix = '!';

        public static BotConfig Config = null;

        // for compatibility with older code
        public static Leaderboard CurLeaderboard => Controller.CurLeaderboard;

        public static DiscordInput DiscordIO;

        // Todo: remove
        public static string DiscordToken;

        public static BotController Controller;

        public static ChallongeConnection Challonge;

        public static bool TestMode = false;



        /*

        "Test mode" starts the bot in a blank and default state for unit testing.

        Any Discord and Challonge integration is completely disabled. Tests here are designed to test the core logic and functionality of the bot.
        They are not designed to test the fucntionality of the APIs and response times.

        */

        static void Main(string[] args)
        		=> new Program().MainAsync(args).GetAwaiter().GetResult();

        public static int InitTime = 0;
        public async Task MainAsync(string[] args) {

            TestMode = (args.Contains("--test-mode"));

            if (File.Exists("config.json"))
                Config = SerializeHelper.Deserialize<BotConfig>("config.json");
            // Load the Discord token.
            if (Config != null) {
                DiscordToken = Config.BotToken;
            }
            else if (!TestMode) {
                Console.WriteLine("Please enter your Discord Bot Token. If you don't have one, please create one at https://discord.com/developers/applications.");
                DiscordToken = Console.ReadLine().Trim();

                Config = new BotConfig() { BotToken = DiscordToken };
                SerializeConfig();
            }

            Config.PropertyChanged += (o, e) => SerializeConfig();

            DiscordIO = new DiscordInput(DiscordToken, TestMode);

            var initTime = DateTime.UtcNow;

            Controller = new BotController(false, TestMode);
            Controller.Initialize();
            // Controller.Initialize();

            var doneTime = DateTime.UtcNow;

            InitTime = (int)((doneTime - initTime).Ticks / TimeSpan.TicksPerMillisecond);

            // Log in to challonge.

            Challonge = new ChallongeConnection(Config.ChallongeToken);

            var chTime = DateTime.UtcNow;
            // Test connection with a basic api call
            try {
                var rs = await Challonge.GetTournaments();
                
                // log response time
                var chTimeNow = DateTime.UtcNow;
                var ticks = (chTimeNow - chTime).Milliseconds;
                await DiscordIO.Log(new LogMessage(LogSeverity.Info, "Challonge", "Challonge connection OK. API response time: " + ticks + "ms"));
            } catch(ChallongeException e) {
                // connected but error on challonge
                await DiscordIO.Log(new LogMessage(LogSeverity.Error, "Challonge", 
                "Could not log into Challonge. Error(s) are as follows: " + Environment.NewLine + Environment.NewLine +
                String.Join(Environment.NewLine, e.Errors.ToArray())));
            } catch (Exception e) {
                // most likely network error
                await DiscordIO.Log(new LogMessage(LogSeverity.Error, "Challonge", "Error connecting to Challonge.", e));
            }

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        public static void SerializeConfig() {
            SerializeHelper.Serialize(Config, "config.json", true);
        }
    }
}
