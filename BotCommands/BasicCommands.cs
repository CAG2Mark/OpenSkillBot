using System;
using Discord.Commands;
using Discord;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Text;
using Discord.Rest;
using Discord.WebSocket;
using System.Collections;

namespace OpenSkillBot.BotCommands
{
    [Name("Basic Commands")]
    [Summary("Commands providing basic interaction with the bot and set-up help.")]
    public class BasicCommands : ModuleBaseEx<SocketCommandContext>
    {
        public static string GetModuleQueryableName(ModuleInfo module) {
            return module.Name.Split(" ")[0].ToLower();
        }
        
        [Command("ping")]
        [Summary("Pings the bot.")]
        public async Task PingCommand() {

            var timeAuthorSent = Context.Message.Timestamp;
            var timeReceivedAuthor = DateTimeOffset.UtcNow;

            var msg = await Program.DiscordIO.SendMessage("", Context.Channel, EmbedHelper.GenerateInfoEmbed(":clock1: Ping received. Measuring latency..."));

            var timeSent = msg.Timestamp;

            var diff1 = Math.Max(0, (timeReceivedAuthor - timeAuthorSent).Milliseconds);
            var diff2 = Math.Max(0, (timeSent - timeReceivedAuthor).Milliseconds);

            await Task.WhenAll(
                ReplyAsync(EmbedHelper.GenerateInfoEmbed($"Gateway receive latency:  **{diff1}ms**" + Environment.NewLine +
                    $"Gateway send latency: **{diff2}ms**" + Environment.NewLine +
                    $"Total ping: **{diff1 + diff2}ms**", ":ping_pong: Pong!", null)),
                msg.DeleteAsync()
            );
        }

        // These two commands were implemented for fun and as an easter egg. They provide no functionality.

        [Command("fixcag")]
        [Summary("Fixes CAG. (Easter Egg)")]
        public Task FixCAGCommand() {
            var rand = new Random();
            var num = rand.Next();
            return ReplyAsync(EmbedHelper.GenerateSuccessEmbed($"Fixed a total of **{num}** CAGs."));
        }

        [Command("betterbot")]
        [Summary("Gives the superior Discord bot. (Easter Egg)")]
        public Task BetterBotCommand(string a) {
            return ReplyAsync(EmbedHelper.GenerateInfoEmbed($"OpenSkillBot is superior."));
        }

        [Command("decipher")]
        [Summary("Deciphers a message. (Easter Egg)")]
        public Task DecipherCommand([Summary("The string to decipher.")] [Remainder] string toDecipher) {
            return ReplyAsync(EmbedHelper.GenerateErrorEmbed($"Failed to decipher the message." +  
            Environment.NewLine + Environment.NewLine +
            "**Most Likely Cause:**" + Environment.NewLine  + "The message was composed by Cistic."));
        }

        [RequireUserPermission(GuildPermission.ManageGuild)]
        [Command("exit")]
        [Summary("Kills the current bot process.")]
        public async Task KillCommand() {
            await ReplyAsync("OpenSkillBot was slain by " + Context.Message.Author.Username);
            await Program.DiscordIO.Logout();
            Environment.Exit(0);
        }

        [Command("echo")]
        [Summary("Echos the given message.")]
        public Task EchoCommand([Remainder] [Summary("The text to echo.")] string message) {
            return ReplyAsync(message);
        }

        [RequireUserPermission(GuildPermission.Administrator)]
        [Command("deletechannels")]
        [Summary("Deletes all of the set up channels (DEBUG).")]
        public async Task DeleteChannelsCommand() {
            var msg = await Program.DiscordIO.SendMessage("", 
                Context.Channel, 
                EmbedHelper.GenerateInfoEmbed($":arrows_counterclockwise: Deleting channels - please wait..."));

            await ((SocketTextChannel)Program.Config.GetAchievementsChannel()).DeleteAsync();
            await ((SocketTextChannel)Program.Config.GetCommandChannel()).DeleteAsync();
            await ((SocketTextChannel)Program.Config.GetLogsChannel()).DeleteAsync();
            await ((SocketTextChannel)Program.Config.GetActiveMatchesChannel()).DeleteAsync();
            await ((SocketTextChannel)Program.Config.GetLeaderboardChannel()).DeleteAsync();
            await ((SocketTextChannel)Program.Config.GetLogsChannel()).DeleteAsync();
            await ((SocketTextChannel)Program.Config.GetTourneysChannel()).DeleteAsync();
            await ((SocketTextChannel)Program.Config.GetSignupLogsChannel()).DeleteAsync();
            await ((SocketTextChannel)Program.Config.GetSignupsChannel()).DeleteAsync();  
            Program.Config.LogsChannelId = 0;        
            Program.Config.AchievementsChannelId = 0;
            Program.Config.CommandChannelId = 0;
            Program.Config.LeaderboardChannelId = 0;
            Program.Config.HistoryChannelId = 0;
            Program.Config.ActiveMatchesChannelId = 0;
            Program.Config.TourneysChannelId = 0;
            Program.Config.SignupLogsChannelId = 0;
            Program.Config.SignupsChannelId = 0;

            await msg.DeleteAsync();
            await ReplyAsync(EmbedHelper.GenerateSuccessEmbed("Deleted the bot channels."));
        }

        [RequireUserPermission(GuildPermission.ManageGuild)]
        [Command("setupchannels")]
        [Summary("Sets up all the required channels.")]
        public async Task SetupChannelsCommand() {

            var msg = await Program.DiscordIO.SendMessage("", 
                Context.Channel, 
                EmbedHelper.GenerateInfoEmbed($":arrows_counterclockwise: Setting up channels - please wait..."));

            // Create Categories.
            var achievementsCat = (await Program.DiscordIO.CreateCategory("Achievements")).Id;
            var manageCat = (await Program.DiscordIO.CreateCategory("Skillbot Management")).Id;
            var skillCat = (await Program.DiscordIO.CreateCategory("Matches")).Id;
            var tourneyCat = (await Program.DiscordIO.CreateCategory("Tournaments")).Id;

            // Create channels.
            var achievementsId = (await Program.DiscordIO.CreateChannel("achievements", achievementsCat)).Id;

            var commandsId = (await Program.DiscordIO.CreateChannel("commands", manageCat)).Id;
            var logsId = (await Program.DiscordIO.CreateChannel("logs", manageCat)).Id;

            var leaderboardId = (await Program.DiscordIO.CreateChannel("leaderboard", skillCat)).Id;
            var historyId = (await Program.DiscordIO.CreateChannel("match-history", skillCat)).Id;
            var activeMatchesId = (await Program.DiscordIO.CreateChannel("active-matches", manageCat)).Id;

            var tourneysId = (await Program.DiscordIO.CreateChannel("tournaments", tourneyCat)).Id;
            var signupLogsId = (await Program.DiscordIO.CreateChannel("signup-log", tourneyCat)).Id;
            var signupsId = (await Program.DiscordIO.CreateChannel("signups", tourneyCat, true)).Id;

            // Save to config.
            Program.Config.AchievementsChannelId = achievementsId;
            Program.Config.CommandChannelId = commandsId;
            Program.Config.LogsChannelId = logsId;
            Program.Config.LeaderboardChannelId = leaderboardId;
            Program.Config.HistoryChannelId = historyId;
            Program.Config.ActiveMatchesChannelId = activeMatchesId;
            Program.Config.TourneysChannelId = tourneysId;
            Program.Config.SignupLogsChannelId = signupLogsId;
            Program.Config.SignupsChannelId = signupsId;

            await msg.DeleteAsync();

            Program.CurLeaderboard.InvokeChange(); 

            await ReplyAsync(EmbedHelper.GenerateSuccessEmbed("Succesfully created all the required channels. Basic permissions have been set up for the channels. Change the permissions as you please."));
        }

        [RequireUserPermission(GuildPermission.ManageGuild)]
        [Command("linkchannels")]
        [Summary("Links the commands channel, the leaderboard channel, and the match history channel, in that order.")]
        public Task LinkChannelsCommand(
            [Summary("The ID of the achievements channel.")] ulong achievementsId,
            [Summary("The ID of the commands channel.")] ulong commandsId,
            [Summary("The ID of the logs channel.")] ulong logsId,
            [Summary("The ID of the leaderboard channel.")] ulong leaderboardId,
            [Summary("The ID of the match history channel.")] ulong historyId,
            [Summary("The ID of the active matches channel.")] ulong activeMatchesId,
            [Summary("The ID of the tournaments channel.")] ulong tourneysId,
            [Summary("The ID of the tournament signups logs channel.")] ulong signupLogsId,
            [Summary("The ID of the tournament signups channel.")] ulong signupsId) {

            Program.Config.CommandChannelId = commandsId;
            Program.Config.LogsChannelId = logsId;
            Program.Config.LeaderboardChannelId = leaderboardId;
            Program.Config.HistoryChannelId = historyId;
            Program.Config.ActiveMatchesChannelId = activeMatchesId;
            Program.Config.TourneysChannelId = tourneysId;
            Program.Config.SignupLogsChannelId = signupLogsId;
            Program.Config.SignupsChannelId = signupsId;

            Program.CurLeaderboard.InvokeChange();  

            return ReplyAsync(EmbedHelper.GenerateSuccessEmbed("Succesfully linked."));
        }

        [RequireUserPermission(GuildPermission.ManageGuild)]
        [Command("addpermittedrole")]
        [Summary("Adds a role that is permitted to use the bot skill commands.")]
        public Task AddPermittedRoleCommand([Remainder][Summary("The name of the role to add.")] string roleName) {
            Program.Config.PermittedRoleNames.Add(roleName.Trim().ToLower());
            Program.SerializeConfig();
            return ReplyAsync(EmbedHelper.GenerateSuccessEmbed($"Permitted users with the role **{roleName}**."));
        }

        [RequireUserPermission(GuildPermission.ManageGuild)]
        [Command("viewpermittedroles")]
        [Summary("Shows a list of the roles permitted to use the bot skill commands.")]
        public Task ViewPermittedRoleCommand() {
            var sb = new StringBuilder();
            sb.Append("**The following roles are permitted:**" + Environment.NewLine + Environment.NewLine);
            foreach (var r in Program.Config.PermittedRoleNames) {
                sb.Append($"{r}{Environment.NewLine}");
            }
            return ReplyAsync(EmbedHelper.GenerateSuccessEmbed(sb.ToString()));
        }

        [RequireUserPermission(GuildPermission.ManageGuild)]
        [Command("removepermittedrole")]
        [Summary("Adds a role that is permitted to use the bot skill commands.")]
        public Task RemovePermittedRoleCommand([Remainder][Summary("The name of the role to remove.")] string roleName) {
            Program.Config.PermittedRoleNames.Remove(roleName);
            Program.SerializeConfig();
            return ReplyAsync(EmbedHelper.GenerateSuccessEmbed($"**{roleName}** is no longer a permitted role."));
        }

        public static string GenerateCommandUsage(CommandInfo cmd) {
            char prefix = Program.prefix;
            return $"{prefix}{cmd.Name} " +  
                    $"{string.Join(" ", cmd.Parameters.Select(p => p.IsOptional ? "[`" + p.Name + "` = " + p.DefaultValue + "]" : "<`" + p.Name + "`>"))}{Environment.NewLine}";
        }

        bool shouldDisplay(CommandInfo cmd) {
            PreconditionResult result = cmd.CheckPreconditionsAsync(Context).Result;
            return result.IsSuccess && (cmd.Summary == null | !cmd.Summary.Contains("(Easter Egg)"));
        }

        public Embed GenerateModuleEmbed(ModuleInfo module) {
            var f = new EmbedBuilder();
            f.Title = module.Name;
            f.Description = module.Summary;
            f.Color = Discord.Color.Blue;

            bool displayedAny = false;
            foreach (var cmd in module.Commands) {
                if (shouldDisplay(cmd)) {
                    displayedAny = true;
                    f.AddField(GenerateCommandUsage(cmd) + " " + GenerateCommandAliases(cmd), cmd.Summary??"No description");
                }
            }

            if (displayedAny)
                return f.Build();
            else
                return EmbedHelper.GenerateErrorEmbed("You do not have permission to view the commands under this query.");
        }

        [Command("help")]
        [Summary("Returns a list of all available commands.")]
        public Task HelpCommand() {

            char prefix = Program.prefix;

            var builder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
                Title = "Help Center"
            };

            builder.Description = $"The command prefix is `{prefix}`." + Environment.NewLine + Environment.NewLine +
                $"View specific help for a command or module using `{prefix}help <module or command>.`";
            
            foreach (var module in Program.DiscordIO.Commands.Modules)
            {
                var title = $"**{module.Name}** (`{prefix}help {GetModuleQueryableName(module)}`)";

                bool displayedAny = false;

                var sb = new StringBuilder();
                foreach (var cmd in module.Commands) {
                    if (shouldDisplay(cmd)) {
                        displayedAny = true;
                        sb.Append($"`{prefix}{cmd.Name}` ");
                    }
                }

                if (displayedAny) {
                    builder.AddField(title, (module.Summary != null ? module.Summary + Environment.NewLine : "") +
                        sb.ToString()
                    );
                }
            }

            return ReplyAsync(builder.Build());
        }

        [Command("help")]
        [Summary("Searches for a module or command that match the query and returns information about it.")]
        public async Task HelpCommand([Summary("The command to search for.")]string query, [Summary("Whehter or not to force search for a command.")] bool forceCmd = false)
        {
            query = query.ToLower();

            // first search modules
            var moduleResult = Program.DiscordIO.Commands.Modules.FirstOrDefault(m => query.Equals(GetModuleQueryableName(m)));
            if (moduleResult != null && !forceCmd) {
                await ReplyAsync(GenerateModuleEmbed(moduleResult));
                return;
            }

            // now search modules
            var cmdResult = Program.DiscordIO.Commands.Search(Context, query);

            if (!cmdResult.IsSuccess || cmdResult.Commands == null)
            {
                await ReplyAsync(EmbedHelper.GenerateErrorEmbed($"Could not find the command {(forceCmd ? "" : "or module ")}**{query}**."));
                return;
            }

            var result_ = cmdResult.Commands.Where(c => c.Command.Summary == null || !c.Command.Summary.Contains("(Easter Egg)")).ToList();

            string prefix = Program.prefix.ToString();
            var builder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
            };

            sbyte displayedCnt = 0;

            foreach (var match in result_)
            {
                var cmd = match.Command;

                if (shouldDisplay(cmd)) {
                    ++displayedCnt;
                    builder.AddField(x =>
                    {
                        x.Name = GenerateCommandTitle(cmd);
                        x.Value = GenerateHelpText(cmd);
                        x.IsInline = false;
                    });
                }
            }

            if (displayedCnt != 0) {
                await ReplyAsync(builder.Build());
            }
            else {
                builder.Description = $"Found **{result_.Count}** command{(result_.Count == 1 ? "" : "s")}:";
                await ReplyAsync(EmbedHelper.GenerateErrorEmbed("You do not have permission to view this command."));
            }
        }

        [Command("helpcmd")]
        [Summary("Searches for a command that match the query and returns information about it. (Is the same as `help <query> true`)")]
        public async Task HelpCommand([Summary("The command to search for.")]string query)
        {
            await HelpCommand(query, true);
        }

        public static string GenerateCommandAliases(CommandInfo cmd) {
            return (cmd.Aliases.Count == 1 ? "" : $"(Alias{(cmd.Aliases.Count == 2 ? "" : "es")}: !" + string.Join(", !", cmd.Aliases.Skip(1)) + ")");
        }

        public static string GenerateCommandTitle(CommandInfo cmd) {
            string prefix = Program.prefix.ToString();
            return "**" + prefix + cmd.Name + "** " + GenerateCommandAliases(cmd);
        }

        public static string GenerateHelpText(CommandInfo cmd) {
            string prefix = Program.prefix.ToString();
            var sb = new StringBuilder();

            // title
            sb.Append(Environment.NewLine);

            // parameters
            if (cmd.Parameters.Count != 0) {
                sb.Append($"Usage: { GenerateCommandUsage(cmd)}");

                foreach (var p in cmd.Parameters) {
                    sb.Append($"`{p.Name}`: *{p.Summary}*{Environment.NewLine}");
                }
                sb.Append(Environment.NewLine);
            }

            // summary
            sb.Append(cmd.Summary + Environment.NewLine + Environment.NewLine);
            return sb.ToString();
        }

        public static string GenerateHelpText(string command) {
            var result = Program.DiscordIO.Commands.Search(command);
            string prefix = Program.prefix.ToString();

            var sb = new StringBuilder();

            foreach (var match in result.Commands)
            {
                var cmd = match.Command;

                if (cmd.Summary == null || cmd.Summary.Contains("(Easter Egg)")) continue;

                sb.Append(GenerateCommandTitle(cmd));
                sb.Append(Environment.NewLine);
                sb.Append(GenerateHelpText(cmd));
            }

            return sb.ToString();
        }
    }
}