using System;
using System.Threading.Tasks;
using Discord.Commands;
using System.Linq;
using OpenTrueskillBot.Skill;
using Discord;

namespace OpenTrueskillBot.BotCommands
{
    [RequirePermittedRole]
    [Name("Player Management Commands")]
    public class PlayerManagementCommands : ModuleBase<SocketCommandContext>
    {
        [Command("createplayer")]
        [Alias(new string[] {"cp"})]
        [Summary("Creates a player, but does not link them to Discord.")]
        public async Task CreatePlayerCommand(
            [Summary("The player's name.")] string name,
            [Summary("The displayed TrueSkill of the player.")] double skill = double.NaN,
            [Summary("The RD of the player.")] double rd = double.NaN
            
        ) {

            var np = createPlayer(name, skill, rd);
            await np.UpdateRank(true);
            
            Program.Controller.CurLeaderboard.AddPlayer(np);

            await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed(
                $"Added player **{name}** with skill {np.DisplayedSkill} RD {np.Sigma}.")
                );
        }
        
        [Command("createlinkedplayer")]
        [Alias(new string[] {"clp"})]
        [Summary("Creates a player, and links them to Discord.")]
        public async Task CreateLinkedPlayerCommand(
            [Summary("The player's name.")] string name,
            [Summary("The ID of the player.")] ulong id,
            [Summary("The displayed TrueSkill of the player.")] double skill = double.NaN,
            [Summary("The RD of the player.")] double rd = double.NaN
            
        ) {

            var np = createPlayer(name, skill, rd);

            var user = Program.DiscordIO.GetUser(id);
            if (user == null) {
                await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed($"The ID `{id}` is invalid."));
                return;
            }

            np.DiscordId = id;
            await np.UpdateRank(true);
            
            Program.Controller.CurLeaderboard.AddPlayer(np);

            await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed(
                $"Added player **{name}** with skill {np.DisplayedSkill} RD {np.Sigma}," 
                + $" and linked them to the Discord User **{user.Username}#{user.DiscriminatorValue}**.")
                );
        }

        private Player createPlayer(string name, double skill, double rd) {
            if (double.IsNaN(rd)) rd = Program.Config.DefaultSigma;
            if (double.IsNaN(skill)) skill = Program.Config.DefaultMu;
            else skill += Program.Config.TrueSkillDeviations * rd;

            return new Player(name, skill, rd);
        }

        [Command("link")]
        [Summary("Links a player to Discord, or changes the link of the player if they are already linked.")]
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

        [Command("unlink")]
        [Summary("Unlinks a player from Discord.")]
        public async Task UnlinkPlayerCommand(
            [Summary("The player's name, ID, or Discord ID.")] string name
        ) {
            var player = FindPlayer(name);

            if (player == null) {
                await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed($"Could not find the user with name or ID `{name}`."));
                return;
            }

            player.DiscordId = 0;

            Program.Controller.SerializeLeaderboard();

            await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed($"**{player.IGN}** has been unlinked."));
        }

        [Command("editplayer")]
        [Summary("Edits a player. Leave fields empty or type **~** to leave them unchanged.")]
        public async Task EditPlayerCommand(
            [Summary("The player's name, ID, or Discord ID.")] string name,
            [Summary("What to change the player's name to.")] string newName,
            [Summary("What to change the player's TrueSkill to.")] string newTs = "~",
            [Summary("What to change the player's RD to.")] string newRd = "~",
            [Summary("What to change the player's alias to.")] string newAlias = "~"
        ) {
            var player = FindPlayer(name);

            if (player == null) {
                await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed($"Could not find the user with name or ID `{name}`."));
                return;
            }

            if (!newName.Equals("~")) {
                player.IGN = newName;
            }

            double d_newSigma;
            if (!newTs.Equals("~") && Double.TryParse(newRd, out d_newSigma)) {
                player.Sigma = d_newSigma;
            }

            double d_newTs;
            if (!newTs.Equals("~") && Double.TryParse(newTs, out d_newTs)) {
                player.Mu = d_newTs + player.Sigma * Program.Config.TrueSkillDeviations;
            }

            if (!newAlias.Equals("~")) {
                player.Alias = newAlias;
            }

            Program.CurLeaderboard.InvokeChange();

            await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed($"**{player.IGN}**'s data has been updated to the following: {Environment.NewLine}{Environment.NewLine}"
                + player.GenerateSummary()));
        }

        [Command("findplayer")]
        [Summary("Finds a player and displays information about them.")]
        public async Task FindPlayerCommand([Summary("The player's name, ID, or Discord ID.")] string name) {
            var player = FindPlayer(name);
            if (player == null) {
                await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed($"Could not find the user with name or ID `{name}`."));
                return;
            }

            var embed = new EmbedBuilder().WithTitle(player.IGN).WithColor(Discord.Color.Green);
            embed.AddField("Info", player.GenerateSummary());
            await ReplyAsync("", false, embed.Build());
        }


        [Command("deleteplayer")]
        [Summary("Deletes a player from the leaderboard.")]
        public async Task DeletePlayer([Summary("The player's name, ID, or Discord ID.")] string name) {
            var player = FindPlayer(name);
            if (player == null) {
                await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed($"Could not find the user with name or ID `{name}`."));
                return;
            }

            Program.CurLeaderboard.Players.Remove(player);
            Program.CurLeaderboard.InvokeChange();

            await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed($"**{player.IGN}** has been removed from the leaderboard."));
        }

        public static Player FindPlayer(string name) {
            ulong id;
            var player = Program.CurLeaderboard.FuzzySearch(name);
            if (player == null) {
                player = Program.CurLeaderboard.Players.FirstOrDefault(p => p.UUId.Equals(name.Trim()));
            }
            if (player == null && UInt64.TryParse(name, out id)) {
                player = Program.CurLeaderboard.Players.FirstOrDefault(p => p.DiscordId == id);
            }
            return player;
        }

    }
}