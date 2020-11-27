using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using OpenTrueskillBot.BotInputs;
using OpenTrueskillBot.Skill;


// Perms integer: 29486144
// Invite: https://discord.com/api/oauth2/authorize?client_id=781358879937789982&permissions=29486144&scope=bot

namespace OpenTrueskillBot
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

        static void Main(string[] args)
        		=> new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync() {

            if (File.Exists("config.json"))
                Config = SerializeHelper.Deserialize<BotConfig>("config.json");
            // Load the Discord token.
            if (Config != null) 
            {
                DiscordToken = Config.BotToken;
            }
            else {
                Console.WriteLine("Please enter your Discord Bot Token. If you don't have one, please create one at https://discord.com/developers/applications.");
                DiscordToken = Console.ReadLine().Trim();

                Config = new BotConfig() {BotToken = DiscordToken };
                SerializeHelper.Serialize(Config, "config.json");

                File.WriteAllText("token.txt", DiscordToken);
            }

            Controller = new BotController();

            Console.WriteLine("Bot Started");

            DiscordIO = new DiscordInput(DiscordToken);

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }
        public static void OutputPriv(string output) {
            Console.WriteLine(output);
        }

        public static void OutputPublic(string output) {
            Console.WriteLine(output);
        }
    }
}
