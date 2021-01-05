using System;
using Discord.Commands;
using Discord;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Text;

namespace OpenTrueskillBot.BotCommands
{
    [Name("Basic Commands")]
    public class BasicCommands : ModuleBase<SocketCommandContext>
    {
        
        [Command("ping")]
        [Summary("Pings the bot.")]
        public Task PingCommand() {

            var timeSent = Context.Message.Timestamp;
            var timeNow = DateTimeOffset.UtcNow;            

            var diff = Math.Abs(timeNow.Ticks - timeSent.Ticks) / TimeSpan.TicksPerMillisecond;

            return ReplyAsync("Ping received. Latency was " + diff.ToString() + "ms");
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

        [RequireUserPermission(GuildPermission.ManageGuild)]
        [Command("linkchannels")]
        [Summary("Links the commands channel, the leaderboard channel, and the match history channel, in that order.")]
        public Task LinkChannelsCommand(
            [Summary("The ID of the commands channel.")] ulong commandsId,
            [Summary("The ID of the logs channel.")] ulong logsId,
            [Summary("The ID of the leaderboard channel.")] ulong leaderboardId,
            [Summary("The ID of the match history channel.")] ulong historyId) {

            Program.Config.CommandChannelId = commandsId;
            Program.Config.LogsChannelId = logsId;
            Program.Config.LeaderboardChannelId = leaderboardId;
            Program.Config.HistoryChannelId = historyId;


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
                string description = null;
                foreach (var cmd in module.Commands)
                {
                    PreconditionResult result = cmd.CheckPreconditionsAsync(Context).Result;
                    if (result.IsSuccess && !cmd.Summary.Contains("(Easter Egg)"))
                        description += $"**{prefix}{cmd.Name}**: _{cmd.Summary}_\n";
                }
                
                if (!string.IsNullOrWhiteSpace(description))
                {
                    builder.AddField(x =>
                    {
                        x.Name = module.Name;
                        x.Value = description;
                        x.IsInline = false;
                    });
                }
            }

            return ReplyAsync("", false, builder.Build());
        }

        public static string GenerateHelpText(string command) {
            var result = Program.DiscordIO.Commands.Search(command);
            string prefix = Program.prefix.ToString();

            var sb = new StringBuilder();

            foreach (var match in result.Commands)
            {
                var cmd = match.Command;

                if (cmd.Summary.Contains("(Easter Egg)")) continue;

                sb.Append("**" + prefix + cmd.Name + "** (Aliases: !" + string.Join(", !", cmd.Aliases) + ")" + Environment.NewLine);
                if (cmd.Parameters.Count != 0) {
                    sb.Append($"*Usage: {prefix}{cmd.Name} " +  
                        $"{string.Join(" ", cmd.Parameters.Select(p => p.IsOptional ? "[" + p.Name + " = " + p.DefaultValue + "]" : "<" + p.Name + ">"))}*");
                    sb.Append(Environment.NewLine);
                }
                sb.Append(cmd.Summary + Environment.NewLine + Environment.NewLine);
            }

            return sb.ToString();
        }

        [Name("help")]
        [Command("help")]
        [Summary("Searches for commands that match the query and returns their usages.")]
        public Task HelpCommand([Summary("The command to search for.")]string query)
        {
            var result = Program.DiscordIO.Commands.Search(Context, query);
            var result_ = result.Commands.Where(c => !c.Command.Summary.Contains("(Easter Egg)")).ToList();

            if (!result.IsSuccess)
            {
                return ReplyAsync($"Could not find the command **{query}**.");
            }

            string prefix = Program.prefix.ToString();
            var builder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
                Description = $"Found **{result_.Count}** command(s):"
            };

            foreach (var match in result_)
            {
                var cmd = match.Command;

                builder.AddField(x =>
                {
                    x.Name = prefix + cmd.Name + " (Aliases: !" + string.Join(", !", cmd.Aliases) + ")";
                    if (cmd.Parameters.Count != 0) {
                        x.Value +=  $"Usage: {prefix}{cmd.Name} " +  
                            $"{string.Join(" ", cmd.Parameters.Select(p => p.IsOptional ? "[" + p.Name + " = " + p.DefaultValue + "]" : "<" + p.Name + ">"))}\n";
                    }
                    x.Value += $"{cmd.Summary}";
                    x.IsInline = false;
                });
            }

            return ReplyAsync("", false, builder.Build());
        }

        

        
        
    }
}