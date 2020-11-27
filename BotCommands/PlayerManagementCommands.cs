using System.Threading.Tasks;
using Discord.Commands;
using OpenTrueskillBot.Skill;

namespace OpenTrueskillBot.BotCommands
{
    [Name("Player Management Commands")]
    public class PlayerManagementCommands : ModuleBase<SocketCommandContext>
    {
        [Command("createplayer")]
        [Alias(new string[] {"cp"})]
        [Summary("Creates a player, but does not link them to Discord.")]
        public Task CreatePlayerCommand(
            [Summary("The player's name.")] string name,
            [Summary("The displayed TrueSkill of the player.")] double skill = double.NaN,
            [Summary("The RD of the player.")] double rd = double.NaN
        ) {

            if (double.IsNaN(skill)) skill = Program.Config.DefaultMu;
            else skill += Program.Config.TrueSkillDeviations * Program.Config.DefaultSigma;
            if (double.IsNaN(rd)) rd = Program.Config.DefaultSigma;

            var np = new Player(name, skill, rd);
            Program.Controller.CurLeaderboard.AddPlayer(np);

            return ReplyAsync($"Added player {name} with skill {skill -= Program.Config.TrueSkillDeviations * Program.Config.DefaultSigma} RD {rd}.");

        }
    }
}