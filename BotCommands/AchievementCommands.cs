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
    [RequirePermittedRole]
    [Name("Achievements")]
    [Summary("Create and manage achievements and grant them to players.")]
    public class AchievementCommands : ModuleBase<SocketCommandContext>
    {
        [Command("createachievement")]
        [Alias(new string[] {"ca"})]
        [Summary("Creates an achievement.")]
        public async Task CreateAchievementCommand(
            [Summary("The achievement's name.")] string name,
            [Summary("The description of the achievement.")] string description
        ) {
            var result = await Program.Controller.Achievements.AddAchievement(name, description);
            if (result) await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed($"Successfully added the achievement **{name}**."));
            else await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed($"Could not create the achievement **{name}**. Please try again."));
        }

        [Command("editachievement")]
        [Summary("Edits an achievement.")]
        public async Task EditAchievementCommand(
            [Summary("The name, UUID or Discord message ID of the achievement.")] string achievement,
            [Summary("The achievement's name.")] string name,
            [Summary("The description of the achievement.")] string description
        ) {
            var achv = Program.Controller.Achievements.FindAchievement(achievement);
            if (achv == null) {
                await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed($"Could not find the achievement from the query **{achievement}**."));
                return;
            }

            await achv.Edit(name, description);
            
            await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed($"Successfully edited the achievement **{name}**."));
        }


        [Command("giveachievement")]
        [Alias(new string[] {"ga"})]
        [Summary("Gives an achievement to a player.")]
        public async Task GiveAchievementCommand(
            [Summary("The name, UUID or Discord message ID of the achievement.")] string achievement,
            [Remainder][Summary("The players to give the achievement to.")] string players
        ) {
            var achv = Program.Controller.Achievements.FindAchievement(achievement);
            if (achv == null) {
                await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed($"Could not find the achievement from the query **{achievement}**."));
                return;
            }

            var teams = TournamentCommands.StrListToTeams(players);

            var output = "";
            foreach (var t in teams) {
                foreach (var p in t.Players) {
                    if (await achv.AddPlayer(p)) {
                        output += p.IGN + Environment.NewLine;
                    }
                    else {
                        await ReplyAsync("", false, EmbedHelper.GenerateWarnEmbed($"Could not add the player **{p.IGN}** to the achievement **{achv.Name}**."));
                    }
                }
            }

            await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed(
                $"Successfully added the following players to the achievement **{achv.Name}**:" + Environment.NewLine + Environment.NewLine + output));
        }

        [Command("revokeachievement")]
        [Alias(new string[] {"ra"})]
        [Summary("Revokes an achievement from a player.")]
        public async Task RemoveAchievementCommand(
            [Summary("The name, UUID or Discord message ID of the achievement.")] string achievement,
            [Remainder][Summary("The player to revoke the achievement from.")] string players
        ) {
            var achv = Program.Controller.Achievements.FindAchievement(achievement);
            if (achv == null) {
                await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed($"Could not find the achievement from the query **{achievement}**."));
                return;
            }

            var teams = TournamentCommands.StrListToTeams(players);

            var output = "";
            foreach (var t in teams) {
                foreach (var p in t.Players) {
                    if (await achv.AddPlayer(p)) {
                        output += p.IGN + Environment.NewLine;
                    }
                    else {
                        await ReplyAsync("", false, EmbedHelper.GenerateWarnEmbed($"Could not revoke the player **{p.IGN}** of the achievement **{achv.Name}**."));
                    }
                }
            }

            await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed(
                $"Successfully revoked the following players of the achievement **{achv.Name}**:" + Environment.NewLine + Environment.NewLine + output));
        }

        [Command("deleteachievement")]
        [Summary("Deletes an achievement.")]
        public async Task DeleteAchievementCommand(
            [Summary("The name, UUID or Discord message ID of the achievement to delete.")] string achievement
        ) {
            var achv = Program.Controller.Achievements.FindAchievement(achievement);
            if (achv != null) {
                if (await Program.Controller.Achievements.DeleteAchievement(achv)) {
                    await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed($"Deleted the achievement **{achv.Name}**."));
                } else {
                    await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed($"Could not delete the achievement **{achv.Name}**."));
                }
            } else {
                await ReplyAsync("", false, EmbedHelper.GenerateErrorEmbed($"Could not find the achievement from the query **{achievement}**."));
            }
        }

        [Command("resendachvs")]
        [Summary("Deletes and resends all the achievements in the achievements channel.")]
        public async Task ResentAchvsCommand() {
            var msg = await Program.DiscordIO.SendMessage(
                "", 
                Context.Channel, 
                EmbedHelper.GenerateInfoEmbed($":arrows_counterclockwise: Resending all achievements..."));
            foreach (var a in Program.Controller.Achievements.AchievementsList) {
                await a.DeleteMessage();
                await a.SendMessage();
            }

            await ReplyAsync("", false, EmbedHelper.GenerateSuccessEmbed($"Resent {Program.Controller.Achievements.AchievementsList.Count()} achievement(s)."));
        }
    }
}