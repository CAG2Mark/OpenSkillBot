using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using OpenSkillBot.Skill;

namespace OpenSkillBot.BotCommands
{
    [Name("Info")]
    [Summary("Public information commands that anyone can use.")]
    public class PublicCommands : ModuleBaseEx<SocketCommandContext> {      

        [Command("playerinfo")]
        [Alias(new string[] {"pi"})]
        [Summary("Gives a summary of a player.**")]
        public async Task PlayerInfoCommand([Summary("The player's name, ID, or Discord ID. Leave blank to")] string name = null) {
            Player player = null;
            
            if (!string.IsNullOrWhiteSpace(name)) {
                player = PlayerManagementCommands.FindPlayer(name);
            }
            else {
                player = Program.CurLeaderboard.FindPlayer(Context.User.Id);
            }

            if (player == null) {
                await ReplyAsync(EmbedHelper.GenerateErrorEmbed($"Could not find the user with name or ID `{name}`."));
                return;
            }

            await ReplyAsync(player.GenerateEmbed());
        }  

        [Command("calculate")]
        [Alias(new string[] { "calc"})]
        [Summary("Returns the value of the hypothetical result of a full match between two teams.")]
        public async Task CalculateCommand([Summary("The first team.")] string team1, [Summary("The second team.")] string team2,
            [Summary("The result of a match. By default, the first team wins. Enter 0 for a draw. Enter -1 to cancel the match.")] int result = 1
        ) {
            try {
                Team t1;
                Team t2;
                try {
                    t1 = SkillCommands.StrToTeam(team1);
                    t2 = SkillCommands.StrToTeam(team2);
                }
                catch (Exception e) {
                    await ReplyAsync(EmbedHelper.GenerateErrorEmbed(e.Message));
                    return;
                }

                var oldData = SkillCommands.ToOldPlayerData(new Team[] {t1, t2});

                var t1_s = t1.ToString();
                var t2_s = t2.ToString();

                var team1Win = SkillWrapper.GetMatchResult(t1.Players, t2.Players);
                var team2Win = SkillWrapper.GetMatchResult(t2.Players, t1.Players);
                var draw = SkillWrapper.GetMatchResult(t1.Players, t2.Players, true);

                var embed = new EmbedBuilder()
                    .WithColor(Discord.Color.Blue)
                    .WithTimestamp(DateTime.UtcNow)
                    .WithTitle($":crossed_swords: **{t1_s}** vs **{t2_s}**:");

                embed.AddField($"If **{t1_s}** wins:", 
                    MessageGenerator.MatchDeltaGenerator(oldData, SkillCommands.ToOldPlayerData(team1Win)).SkillChanges);

                embed.AddField($"If **{t2_s}** wins:", 
                    MessageGenerator.MatchDeltaGenerator(oldData, SkillCommands.ToOldPlayerData(team2Win)).SkillChanges);

                embed.AddField($"If **{t1_s}** and **{t2_s}** draw:", 
                    MessageGenerator.MatchDeltaGenerator(oldData, SkillCommands.ToOldPlayerData(draw)).SkillChanges);

                await ReplyAsync(embed.Build());
            }
            catch (Exception e) {
                await ReplyAsync(EmbedHelper.GenerateErrorEmbed(e.Message));
            }
        }
    }
}