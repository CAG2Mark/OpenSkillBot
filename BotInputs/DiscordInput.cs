using Discord.WebSocket;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Discord.Rest;

// Most of the code here is copied from the Discord.NET documentation.

namespace OpenTrueskillBot.BotInputs
{
    public class DiscordInput {

        private DiscordSocketClient client;
        private CommandHandler commandHandler;

        public CommandService Commands;
        private IServiceProvider provider;
        
        public DiscordInput(string token) {
            
            client = new DiscordSocketClient();

            client.MessageReceived += MessageReceived;

            Commands = new CommandService();
            // commandHandler = new CommandHandler(client, commands);

            // Subscribe the logging handler to both the client and the CommandService.
            client.Log += Log;
            Commands.Log += Log;
        
            // Source: https://github.com/Aux/Discord.Net-Example

            // Create a new instance of a service collection
            var services = new ServiceCollection();             
            ConfigureServices(services, client, Commands);

            // Build the service provider
            provider = services.BuildServiceProvider();
            // Start the command handler service     
            provider.GetRequiredService<CommandHandler>(); 	

            LoginToDiscord(token);	
        }

        private void ConfigureServices(IServiceCollection services, DiscordSocketClient client, CommandService commands)
        {
            services       
                // Repeat this for all the service classes
                // and other dependencies that your commands might need.
                .AddSingleton(client)
                .AddSingleton(commands)
                .AddSingleton<CommandHandler>();
            
        }

        async Task LoginToDiscord(string token) {

            // Remember to keep token private or to read it from an 
            // external source! In this case, we are reading the token 
            // from an environment variable. If you do not know how to set-up
            // environment variables, you may find more information on the 
            // Internet or by using other methods such as reading from 
            // a configuration.
            await client.LoginAsync(TokenType.Bot, 
                token);
            await client.StartAsync();

        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        // Task when message is received on Discord
        private async Task MessageReceived(SocketMessage arg) {

        }

        public async Task<IMessage> SendMessage(string message, ISocketMessageChannel channel) {
            // sends the message and returns it
            return await channel.SendMessageAsync(message);
        }
        
        public ISocketMessageChannel GetChannel(ulong id) {
            // the bot is designed to only be in one server
            return client.Guilds.First().GetTextChannel(id);
        }

        public async Task<IMessage> GetMessage(ulong messageId, ulong channelId) {
            var channel = GetChannel(channelId);
            return await channel.GetMessageAsync(messageId);
        }

        public async Task EditMessage(ulong messageId, ulong channelId, string newText) {
            try {
                var msg = (RestUserMessage)await GetMessage(messageId, channelId);
                await EditMessage(msg, newText);
            }
            // error if the bot is not the author of the message
            catch (InvalidCastException) {
                throw new Exception($"Cannot edit message {messageId} in channel {channelId}. The bot is not the author of the message.");
            }          
        }

        public async Task EditMessage(RestUserMessage msg, string newText) {
            await msg.ModifyAsync(m => {
                m.Content = newText;
            });
        }

        public async Task PopulateChannel(ulong channelId, string[] text) {
            try {
                var messages = GetChannel(channelId).GetMessagesAsync(text.Length);
                int count = await messages.CountAsync();

                messages.OrderBy(m => ((RestUserMessage)m).Timestamp);

                int i;
                for (i = 0; i < count; ++i) {
                    var msgRest = (RestUserMessage) await messages.ElementAtAsync(i);
                    await EditMessage(msgRest, text[i]);
                }

                // if a new message has to be sent
                if (count < text.Length) {
                    for (; i < text.Length; ++i) {
                        await SendMessage(text[i], GetChannel(channelId));
                    }
                }

                // todo: delete redundant messages automatically
                
            }
            catch (InvalidCastException) {
                throw new Exception($"Cannot edit message. The bot is not the author of the message.");
            }    
        }
    }
}