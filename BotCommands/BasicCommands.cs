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
    public class BasicCommands : ModuleBase<SocketCommandContext>
    {
        
        [Command("ping")]
        [Summary("Pings the bot.")]
        public Task PingCommand() {

            var timeSent = Context.Message.Timestamp;
            var timeNow = DateTimeOffset.UtcNow;            

            var diff = Math.Abs((timeNow - timeSent).Milliseconds);

            return ReplyAsync("Ping received. Latency was " + diff + "ms");
        }

        // These two commands were implemented for fun and as an easter egg. They provide no functionality.

        [Command("fixcag")]
        [Summary("Fixes CAG. (Easter Egg)")]
        public Task FixCAGCommand() {
            var rand = new Random();
            var num = rand.Next();
            return ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed($"Fixed a total of **{num}** CAGs."));
        }

        [Command("betterbot")]
        [Summary("Gives the superior Discord bot. (Easter Egg)")]
        public Task BetterBotCommand(string a) {
            return ReplyAsync("", false, EmbedHelper.GenerateInfoEmbed($"Open TrueSkill Bot is superior."));
        }

        [Command("decipher")]
        [Summary("Deciphers a message. (Easter Egg)")]
        public Task DecipherCommand([Summary("The string to decipher.")] [Remainder] string toDecipher) {
            return ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed($"Failed to decipher the message." +  
            Environment.NewLine + Environment.NewLine +
            "**Most Likely Cause:**" + Environment.NewLine  + "The message was composed by Cistic."));
        }

        [RequireUserPermission(GuildPermission.ManageGuild)]
        [Command("exit")]
        [Summary("Kills the current bot process.")]
        public async Task KillCommand() {
            await ReplyAsync("OpenTrueskillBot was slain by " + Context.Message.Author.Username);
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
            await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed("Deleted the bot channels."));
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

            await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed("Succesfully created all the required channels. Basic permissions have been set up for the channels. Change the permissions as you please."));
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

            return ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed("Succesfully linked."));
        }

        [RequireUserPermission(GuildPermission.ManageGuild)]
        [Command("addpermittedrole")]
        [Summary("Adds a role that is permitted to use the bot skill commands.")]
        public Task AddPermittedRoleCommand([Remainder][Summary("The name of the role to add.")] string roleName) {
            Program.Config.PermittedRoleNames.Add(roleName.Trim().ToLower());
            Program.SerializeConfig();
            return ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed($"Permitted users with the role **{roleName}**."));
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
            return ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed(sb.ToString()));
        }

        [RequireUserPermission(GuildPermission.ManageGuild)]
        [Command("removepermittedrole")]
        [Summary("Adds a role that is permitted to use the bot skill commands.")]
        public Task RemovePermittedRoleCommand([Remainder][Summary("The name of the role to remove.")] string roleName) {
            Program.Config.PermittedRoleNames.Remove(roleName);
            Program.SerializeConfig();
            return ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed($"**{roleName}** is no longer a permitted role."));
        }

        [Command("help")]
        [Summary("Returns a list of all available commands.")]
        public Task HelpCommand() {

            char prefix = Program.prefix;

            var builder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
            };
            
            foreach (var module in Program.DiscordIO.Commands.Modules)
            {
                bool hasSentFirst = false;

                string name = "---" + module.Name.ToUpper() + "---";

                string description = "";
                foreach (var cmd in module.Commands)
                {

                    PreconditionResult result = cmd.CheckPreconditionsAsync(Context).Result;
                    if (result.IsSuccess && (cmd.Summary == null | !cmd.Summary.Contains("(Easter Egg)"))) {
                        var append = $"**{prefix}{cmd.Name}**: _{cmd.Summary}_\n";
                        if (append.Length + description.Length > 1024) {
                            builder.AddField(x => {
                                x.Name = name;
                                x.Value = description;
                                x.IsInline = false;
                            });
                            description = "";
                            hasSentFirst = true;
                        }
                        description += append;
                    }
                }
                
                if (!string.IsNullOrWhiteSpace(description))
                {
                    builder.AddField(x => {
                        x.Name = hasSentFirst ? "..." : name;
                        x.Value = description;
                        x.IsInline = false;
                    });
                }
            }

            return ReplyAsync("", false, builder.Build());
        }

        public static string GenerateCommandTitle(CommandInfo cmd) {
            string prefix = Program.prefix.ToString();
            return "**" + prefix + cmd.Name + "** " + 
            (cmd.Aliases.Count == 1 ? "" : $"(Alias{(cmd.Aliases.Count == 2 ? "" : "es")}: !" + string.Join(", !", cmd.Aliases.Skip(1)) + ")");
        }

        public static string GenerateHelpText(CommandInfo cmd) {
            string prefix = Program.prefix.ToString();
            var sb = new StringBuilder();

            // title
            sb.Append(Environment.NewLine);

            // parameters
            if (cmd.Parameters.Count != 0) {
                sb.Append($"Usage: {prefix}{cmd.Name} " +  
                    $"{string.Join(" ", cmd.Parameters.Select(p => p.IsOptional ? "[`" + p.Name + "` = " + p.DefaultValue + "]" : "<`" + p.Name + "`>"))}{Environment.NewLine}"
                    );

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

        [Name("help")]
        [Command("help")]
        [Summary("Searches for commands that match the query and returns their usages.")]
        public async Task HelpCommand([Summary("The command to search for.")]string query)
        {
            var result = Program.DiscordIO.Commands.Search(Context, query);

            if (!result.IsSuccess || result.Commands == null)
            {
                await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed($"Could not find the command **{query}**."));
                return;
            }

            var result_ = result.Commands.Where(c => c.Command.Summary == null || !c.Command.Summary.Contains("(Easter Egg)")).ToList();

            string prefix = Program.prefix.ToString();
            var builder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
                Description = $"Found **{result_.Count}** command{(result_.Count == 1 ? "" : "s")}:"
            };

            foreach (var match in result_)
            {
                var cmd = match.Command;

                builder.AddField(x =>
                {
                    x.Name = GenerateCommandTitle(cmd);
                    x.Value = GenerateHelpText(cmd);
                    x.IsInline = false;
                });
            }

            await ReplyAsync("", false, builder.Build());
        }
    }
}