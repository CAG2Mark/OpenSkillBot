using Discord.Commands;
using Discord;
using System;
using System.Threading.Tasks;

namespace OpenTrueskillBot.BotCommands
{
    public class SkillCommands : ModuleBase<SocketCommandContext>
    {
        [Command("fullmatch")]
        [Alias(new string[] {"fm"})]
        [Summary("Calculates a full match between two teams.")]
        public Task FullMatchCommand() {

            return ReplyAsync("");

        }
    }
}