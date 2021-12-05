using System;
using System.Threading.Tasks;
using Discord.Commands;
using System.Linq;
using OpenSkillBot.Skill;
using Discord;

namespace OpenSkillBot.BotCommands
{
    [RequirePermittedRole]
    [Name("Player Management")]
    [Summary("Add, edit and delete players.")]
    public class PlayerManagementCommands : ModuleBaseEx<SocketCommandContext>
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

            await ReplyAsync(EmbedHelper.GenerateSuccessEmbed(
                $"Added player **{name}** with skill {np.DisplayedSkill} RD {np.Sigma}.", "Player ID: " + np.UUId)
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
                await ReplyAsync(EmbedHelper.GenerateErrorEmbed($"The ID `{id}` is invalid."));
                return;
            }

            np.LinkDiscord(id);
            await np.UpdateRank(true);
            
            Program.Controller.CurLeaderboard.AddPlayer(np);

            await ReplyAsync(EmbedHelper.GenerateSuccessEmbed(
                $"Added player **{name}** with skill {np.DisplayedSkill} RD {np.Sigma}," 
                + $" and linked them to the Discord User {user.Mention}.", "Player ID: " + np.UUId)
                );
        }

        [Command("autocreateplayer")]
        [Alias(new string[] {"autocp"})]
        [Summary("Creates a new player with the default settings and links it to the last joined member in the server.")]
        public async Task AutoCreatePlayer(
            [Remainder][Summary("The player's name. Leave empty to set to their Discord username.")] string name = null
        ) {

            if (Program.CurLeaderboard.LatestJoinedPlayer == null) {
                await ReplyAsync(EmbedHelper.GenerateErrorEmbed("Could not identify the latest player who has joined."));
                return;
            }

            (string Name, ulong DiscordID) p = ((string Name, ulong DiscordID))Program.CurLeaderboard.LatestJoinedPlayer;

            await CreateLinkedPlayerCommand(string.IsNullOrWhiteSpace(name) ? p.Name : name, p.DiscordID);
        }

        private Player createPlayer(string name, double skill = double.NaN, double rd = double.NaN) {
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
                await ReplyAsync(EmbedHelper.GenerateErrorEmbed($"Could not find the player **{name}**."));
                return;
            }

            var user = Program.DiscordIO.GetUser(id);
            if (user == null) {
                await ReplyAsync(EmbedHelper.GenerateErrorEmbed($"The ID `{id}` is invalid."));
                return;
            }

            player.LinkDiscord(id);
            await player.UpdateRank(true);

            await ReplyAsync(EmbedHelper.GenerateSuccessEmbed($"**{player.IGN}** is now linked to **{user.Username}#{user.DiscriminatorValue}**"));
        }

        [Command("unlink")]
        [Summary("Unlinks a player from Discord.")]
        public async Task UnlinkPlayerCommand(
            [Summary("The player's name, ID, or Discord ID.")] string name
        ) {
            var player = FindPlayer(name);

            if (player == null) {
                await ReplyAsync(EmbedHelper.GenerateErrorEmbed($"Could not find the user with name or ID `{name}`."));
                return;
            }

            player.UnlinkDiscord();

            await ReplyAsync(EmbedHelper.GenerateSuccessEmbed($"**{player.IGN}** has been unlinked."));
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
                await ReplyAsync(EmbedHelper.GenerateErrorEmbed($"Could not find the user with name or ID `{name}`."));
                return;
            }

            if (!newName.Equals("~")) {
                player.IGN = newName;
            }

            double d_newSigma;
            if (!newRd.Equals("~") && Double.TryParse(newRd, out d_newSigma)) {
                double diff = d_newSigma - player.Sigma;
                player.Sigma = d_newSigma;
                player.Mu += diff * Program.Config.TrueSkillDeviations;
            }

            double d_newTs;
            if (!newTs.Equals("~") && Double.TryParse(newTs, out d_newTs)) {
                player.Mu = d_newTs + player.Sigma * Program.Config.TrueSkillDeviations;
            }

            if (!newAlias.Equals("~")) {
                player.Alias = newAlias;
            }

            Program.CurLeaderboard.InvokeChange();

            await ReplyAsync(EmbedHelper.GenerateSuccessEmbed($"**{player.IGN}**'s data has been updated to the following: {Environment.NewLine}{Environment.NewLine}"
                + player.GenerateSummary()));
        }

        [Command("findplayer")]
        [Summary("Finds a player and displays information about them.")]
        public async Task FindPlayerCommand([Summary("The player's name, ID, or Discord ID.")] string name) {
            var player = FindPlayer(name);
            if (player == null) {
                await ReplyAsync(EmbedHelper.GenerateErrorEmbed($"Could not find the user with name or ID `{name}`."));
                return;
            }

            var embed = new EmbedBuilder().WithTitle(player.IGN).WithColor(Discord.Color.Green);
            embed.AddField("Info", player.GenerateSummary());
            if (player.DiscordUser != null) {
                embed.ThumbnailUrl = player.DiscordUser.GetAvatarUrl();
            }

            await ReplyAsync(embed.Build());
        }


        [Command("deleteplayer")]
        [Summary("Deletes a player from the leaderboard. **Note that this player will only be truly deleted after a leaderboard reset," + 
            "as their stats are still needed to re-calculate past matches! Until then, they are marked for deletion and hidden from the leaderboard.**")]
        public async Task DeletePlayer([Summary("The player's name, ID, or Discord ID.")] string name) {
            var player = FindPlayer(name);
            if (player == null) {
                await ReplyAsync(EmbedHelper.GenerateErrorEmbed($"Could not find the user with name or ID `{name}`."));
                return;
            }

            player.MarkedForDeletion = true;

            Program.CurLeaderboard.InvokeChange();

            await ReplyAsync(EmbedHelper.GenerateSuccessEmbed($"**{player.IGN}** has been removed from the leaderboard."));
        }

        public static Player FindPlayer(string name) {
            return Program.CurLeaderboard.FuzzySearch(name);
        }
    }
}