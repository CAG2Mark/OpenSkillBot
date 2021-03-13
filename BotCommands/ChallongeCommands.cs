using Discord.Commands;
using Discord;
using System;
using System.Threading.Tasks;
using OpenSkillBot.Skill;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenSkillBot.BotCommands
{
    
    /// <summary>
    /// Challonge commands for debug purposes.
    /// </summary>
    [RequirePermittedRole]
    [Name("Challonge Commands")]
    public class ChallongeCommands : ModuleBase
    {
        [Command("listchallongetournaments")]
        [Alias(new string[] { "lct" })]
        [Summary("Lists the Challonge tournaments.")]
        public async Task ListChallongeTournamentsCommand() {
            var tourneys = await Program.Challonge.GetTournaments();

            var sb = new StringBuilder();
            foreach (var t in tourneys) {
                sb.Append($"**{t.Name}** - {t.FullChallongeUrl}{Environment.NewLine}");
            }

            await ReplyAsync("", false, EmbedHelper.GenerateInfoEmbed(sb.ToString(), $"Found {tourneys.Count} tournament(s):", null));
        }   
    }
}