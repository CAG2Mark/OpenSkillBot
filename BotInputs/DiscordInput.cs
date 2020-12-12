using Discord.WebSocket;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Discord.Rest;
using System.Collections.Generic;

// Most of the code here is copied from the Discord.NET documentation.

namespace OpenTrueskillBot.BotInputs
{
    public class DiscordInput {

        private DiscordSocketClient client;
        private CommandHandler commandHandler;

        public CommandService Commands;
        private IServiceProvider provider;

        public SocketGuild CurGuild => client.Guilds.First();
        
        public DiscordInput(string token) {
            
            var cfg = new DiscordSocketConfig();
            cfg.AlwaysDownloadUsers = true;

            client = new DiscordSocketClient(cfg);

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

            client.UserJoined += (user) => {
                return client.DownloadUsersAsync(client.Guilds);
            };
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

            await client.DownloadUsersAsync(client.Guilds);

            Console.WriteLine("Users downloaded");

        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        // Task when message is received on Discord
        private async Task MessageReceived(SocketMessage arg) {

        }

        public async Task<RestUserMessage> SendMessage(string message, ISocketMessageChannel channel, Embed embed = null) {
            // sends the message and returns it
            return await channel.SendMessageAsync(message, false, embed);
        }
        
        public ISocketMessageChannel GetChannel(ulong id) {
            // the bot is designed to only be in one server
            return CurGuild.GetTextChannel(id);
        }

        public SocketGuildUser GetUser(ulong userId) {
            return CurGuild.GetUser(userId);
        }

        public async Task AddRole(ulong userId, ulong roleId) {
            await AddRole(GetUser(userId), roleId);
        }

        public async Task AddRole(SocketGuildUser user, ulong roleId) {
            await user.AddRoleAsync(CurGuild.GetRole(roleId));
        }

        public async Task RemoveRole(ulong userId, ulong roleId) {
            await RemoveRole(GetUser(userId), roleId);
        }

        public async Task RemoveRole(SocketGuildUser user, ulong roleId) {
            await user.RemoveRoleAsync(CurGuild.GetRole(roleId));
        }

        public async Task RemoveRoles(ulong userId, IEnumerable<ulong> roleIds) {
            await RemoveRoles(GetUser(userId), roleIds);
        }

        public async Task RemoveRoles(SocketGuildUser user, IEnumerable<ulong> roleIds) {
            var roles = new List<SocketRole>();
            foreach (var id in roleIds) {
                roles.Add(CurGuild.GetRole(id));
            }
            await user.RemoveRolesAsync(roles);
        }

        public async Task<IMessage> GetMessage(ulong messageId, ulong channelId) {
            var channel = GetChannel(channelId);
            return await channel.GetMessageAsync(messageId);
        }

        public async Task EditMessage(ulong messageId, ulong channelId, string newText, Embed embed = null) {
            try {
                var msg = (RestUserMessage)await GetMessage(messageId, channelId);
                await EditMessage(msg, newText, embed);
            }
            // error if the bot is not the author of the message
            catch (InvalidCastException) {
                throw new Exception($"Cannot edit message {messageId} in channel {channelId}. The bot is not the author of the message.");
            }          
        }

        public async Task EditMessage(RestUserMessage msg, string newText, Embed embed = null) {
            await msg.ModifyAsync(m => {
                m.Content = newText;
                m.Embed = embed;
            });
        }

        public async Task PopulateChannel(ulong channelId, string[] text) {
            try {
                var messages = await GetChannel(channelId).GetMessagesAsync(text.Length).FlattenAsync();
                int count = messages.Count();

                List<IMessage> messagesList = messages.OrderBy(m => ((RestUserMessage)m).Timestamp).ToList();

                int i;
                for (i = 0; i < count; ++i) {
                    var msgRest = (RestUserMessage)messagesList[i];
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

        public async Task Logout() {
            await client.LogoutAsync();
        }
    }
}