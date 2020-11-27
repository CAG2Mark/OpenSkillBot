using Discord.WebSocket;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        public async Task SendMessage(string message, ISocketMessageChannel channel) {
            await channel.SendMessageAsync(message);
        }       
    }
}