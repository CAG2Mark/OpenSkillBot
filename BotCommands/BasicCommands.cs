using System;
using Discord.Commands;
using Discord;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Linq;

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

        [Command("echo")]
        [Summary("Echos the given message.")]
        public Task EchoCommand([Remainder] [Summary("The text to echo.")] string message) {
            return ReplyAsync(message);
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
                    if (result.IsSuccess)
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

        [Name("help <query>")]
        [Command("help")]
        [Summary("Searches for commands that match the query and returns their usages.")]
        public Task HelpAsync([Summary("The command to search for.")]string query)
        {
            var result = Program.DiscordIO.Commands.Search(Context, query);

            if (!result.IsSuccess)
            {
                return ReplyAsync($"Could not find the command **{query}**.");
            }

            string prefix = Program.prefix.ToString();
            var builder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
                Description = $"Found **{result.Commands.Count}** command(s):"
            };

            foreach (var match in result.Commands)
            {
                var cmd = match.Command;

                builder.AddField(x =>
                {
                    x.Name = prefix + cmd.Name + " (Aliases: !" + string.Join(", !", cmd.Aliases) + ")";
                    if (cmd.Parameters.Count != 0) {
                        x.Value += $"_Parameters: {string.Join(", ", cmd.Parameters.Select(p => p.Name))}_\n";
                    }
                    x.Value += $"{cmd.Summary}";
                    x.IsInline = false;
                });
            }

            return ReplyAsync("", false, builder.Build());
        }
    }
}