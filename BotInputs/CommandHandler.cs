using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using OpenSkillBot.BotCommands;

// Mostly copied from the Discord.NET documentation.

namespace OpenSkillBot.BotInputs
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;

        public CommandHandler(DiscordSocketClient client, CommandService commands)
        {
            _commands = commands;
            _client = client;

            InstallCommandsAsync();
        }
        
        public async Task InstallCommandsAsync()
        {
            // Hook the MessageReceived event into our command handler
            _client.MessageReceived += HandleCommandAsync;

            // Here we discover all of the command modules in the entry 
            // assembly and load them. Starting from Discord.NET 2.0, a
            // service provider is required to be passed into the
            // module registration method to inject the 
            // required dependencies.
            //
            // If you do not use Dependency Injection, pass null.
            // See Dependency Injection guide for more information.
            await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), 
                                            services: null);
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            // Don't process the command if it was a system message
            var message = messageParam as SocketUserMessage;
            if (message == null) return;

            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;

            // Determine if the message is a command based on the prefix and make sure no bots trigger commands
            if (!(message.HasCharPrefix(Program.prefix, ref argPos) || 
                message.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
                message.Author.IsBot)
                return;

            // Create a WebSocket-based command context based on the message
            var context = new SocketCommandContext(_client, message);

            // Execute the command with the command context we just
            // created, along with the service provider for precondition checks.

            using (var typing = message.Channel.EnterTypingState()) {
                // Keep in mind that result does not indicate a return value
                // rather an object stating if the command executed successfully.
                var result = await _commands.ExecuteAsync(
                    context: context, 
                    argPos: argPos,
                    services: null);

                // Optionally, we may inform the user if the command fails
                // to be executed; however, this may not always be desired,
                // as it may clog up the request queue should a user spam a
                // command.
                if (!result.IsSuccess) {
                    if (result.Error.Equals(CommandError.BadArgCount) || result.Error.Equals(CommandError.ParseFailed)) {
                        var cmd = message.Content.Replace(Program.prefix.ToString(), "").Split(" ")[0];
                        await context.Channel.SendMessageAsync("", false, EmbedHelper.GenerateInfoEmbed(BasicCommands.GenerateHelpText(cmd)));
                    }
                    else {
                        await context.Channel.SendMessageAsync("", false, EmbedHelper.GenerateErrorEmbed(result.ErrorReason),
                            allowedMentions:new Discord.AllowedMentions(null), messageReference: new Discord.MessageReference(context.Message.Id));
                    }
                }

                try {
                    typing.Dispose();
                } catch (TaskCanceledException ex) {
                    if (ex.CancellationToken.IsCancellationRequested) throw ex;
                }

                
            }
        }
    }
}