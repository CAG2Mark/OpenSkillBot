using System;
using OpenTrueskillBot.BotInputs;
using OpenTrueskillBot.Skill;

namespace OpenTrueskillBot
{
    class Program
    {

        public static SkillConfig CurSkillConfig;
        public static Leaderboard CurLeaderboard;

        static void Main(string[] args)
        {
            var test = new MatchAction(new Team());
            
            Console.WriteLine("Bot started.");

            string line;
            // basic IO
            while (!(line = Console.ReadLine()).Equals("exit")) {
                
            }

            Console.WriteLine("Exiting.");
        }

        public static void OutputPriv(string output) {
            Console.WriteLine(output);
        }

        public static void OutputPublic(string output) {
            Console.WriteLine(output);
        }
    }
}
