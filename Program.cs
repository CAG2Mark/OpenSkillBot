using System;
using System.Threading.Tasks;
using Discord;
using OpenTrueskillBot.BotInputs;
using OpenTrueskillBot.Skill;


// Perms integer: 29486144
// Invite: https://discord.com/api/oauth2/authorize?client_id=781358879937789982&permissions=29486144&scope=bot

namespace OpenTrueskillBot
{
    class Program
    {

        public static SkillConfig CurSkillConfig = new SkillConfig();
        public static Leaderboard CurLeaderboard;

        static void Main(string[] args)
        		=> new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync() {
            var test = new MatchAction(new Team());
            
            Console.WriteLine("Bot started.");

            // Test

            var player1 = new Player(1804, 30);
            var player2 = new Player(1814, 31);

            TrueskillWrapper.CalculateMatch(new Player[] {player1}, new Player[] {player2});
            Console.WriteLine(player1.Mu);
            Console.WriteLine(player1.Sigma);

            string line;
            // basic IO
            while (!(line = Console.ReadLine()).Equals("exit")) {
                
            }

            

            Console.WriteLine("Exiting.");
        }

        #region Discord.NET documentation boilerplate

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        #endregion

        public static void OutputPriv(string output) {
            Console.WriteLine(output);
        }

        public static void OutputPublic(string output) {
            Console.WriteLine(output);
        }
    }
}
