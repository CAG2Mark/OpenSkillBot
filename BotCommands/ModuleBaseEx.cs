using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace OpenSkillBot.BotCommands
{
    public class ModuleBaseEx<T>: ModuleBase<T> where T : class, ICommandContext
    {
        /// <summary>
        /// Replies to the message asynchronously
        /// </summary>
        /// <param name="embed"></param>
        /// <returns></returns>
        public async Task<IMessage> ReplyAsync(Embed embed) {
            var msg = await ReplyAsync("", false, embed,
                allowedMentions: new AllowedMentions(null),
                messageReference: new MessageReference(Context.Message.Id));

            return msg;
        }

        public async Task<IUserMessage> SendProgressAsync(Embed embed) {
            var chnl = Context.Channel;

            var msg = await chnl.SendMessageAsync("", false, embed, 
                allowedMentions: new AllowedMentions(null),
                messageReference: new MessageReference(Context.Message.Id));

            // currently this method is the same as SendAsync, but will change this later to add some extra features

            return msg;
        }

        public async Task<IUserMessage> SendAsync(Embed embed) {
            var chnl = Context.Channel;

            var msg = await chnl.SendMessageAsync("", false, embed, 
                allowedMentions: new AllowedMentions(null),
                messageReference: new MessageReference(Context.Message.Id));

            return msg;
        }
    }
}