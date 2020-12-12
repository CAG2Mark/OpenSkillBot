using System;
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

            if (double.IsNaN(rd)) rd = Program.Config.DefaultSigma;
            if (double.IsNaN(skill)) skill = Program.Config.DefaultMu;
            else skill += Program.Config.TrueSkillDeviations * rd;

            var np = new Player(name, skill, rd);
            Program.Controller.CurLeaderboard.AddPlayer(np);

            return ReplyAsync($"Added player {name} with skill {skill - Program.Config.TrueSkillDeviations * rd} RD {rd}.");
        }

        [Command("link")]
        [Summary("Links a player to Discord.")]
        public async Task LinkPlayerCommand(
            [Summary("The player's name.")] string name,
            [Summary("The Discord ID of the player.")] ulong id
        ) {
            var player = Program.CurLeaderboard.FuzzySearch(name);
            if (player == null) {
                await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed($"Could not find the player **{name}**."));
                return;
            }

            var user = Program.DiscordIO.GetUser(id);
            if (user == null) {
                await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed($"The ID `{id}` is invalid."));
                return;
            }

            player.DiscordId = id;
            await player.UpdateRank(true);

            Program.Controller.SerializeLeaderboard();

            await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed($"**{player.IGN}** is now linked to **{user.Username}#{user.DiscriminatorValue}**"));
        }

    }
}