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
using Discord.Net;
using Discord.Net.Rest;
using OpenSkillBot.BotCommands;

// Most of the code here is copied from the Discord.NET documentation.

namespace OpenSkillBot.BotInputs
{
    public class DiscordInput {

        private DiscordSocketClient client;

        public CommandService Commands;
        private IServiceProvider provider;

        // currently only supports single guilds
        public SocketGuild CurGuild => client.Guilds.First();

        public bool IsReady { get; private set; }

        private bool testMode;
        
        public DiscordInput(string token, bool testMode = false) {
            
            if (testMode) {
                this.testMode = testMode;
                return;
            }

            var cfg = new DiscordSocketConfig();
            cfg.AlwaysDownloadUsers = true;
            cfg.RestClientProvider = DefaultRestClientProvider.Create(useProxy:true);

            client = new DiscordSocketClient(cfg);

            // client.MessageReceived += MessageReceived;

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

            // do not call any further commands from the client here

            client.Ready += async () => {           
                this.IsReady = true;
                await Log(new LogMessage(LogSeverity.Info, "Program",
                    $"Bot started. Initialisation time was {Program.InitTime}ms"));

                while (LogQueue.Count > 0) {
                    await SendLog();
                }
            };

            client.UserJoined += async (user) => {
                Program.CurLeaderboard.LatestJoinedPlayer = (user.Username, user.Id);
                Program.Controller.SerializeLeaderboard();

                // send message to help managers auto add players
                var commandsChnl = Program.Config.GetCommandChannel();
                if (commandsChnl != null) {
                    var embed = new EmbedBuilder()
                        .WithThumbnailUrl(user.GetAvatarUrl())
                        .WithColor(Discord.Color.Blue)
                        .WithTitle("New user: " + user.Username + "#" + user.DiscriminatorValue)
                        .WithFooter("ID: " + user.Id);

                    var nl = Environment.NewLine;
                    embed.Description = $"Auto-add {user.Mention} to the leaderboard using: {nl}{nl} `!autocp [name]`";

                    await SendMessage("", commandsChnl, embed.Build());
                }

                await client.DownloadUsersAsync(client.Guilds);
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
            if (testMode) return;


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

        }

        Queue<Tuple<LogMessage, DateTime>> LogQueue = new Queue<Tuple<LogMessage, DateTime>>();

        public async Task SendLog() {
            if (LogQueue.Count == 0) return;

            var val = LogQueue.Peek();
            var msg = val.Item1;
            var text = msg.Message;
            if (string.IsNullOrWhiteSpace(text)) {
                text = msg.Exception.Message;
            }

            if (client.ConnectionState == ConnectionState.Connected && Program.Config.LogsChannelId != 0) {
                try {
                    var channel = Program.Config.GetLogsChannel();
                    if (channel != null) {
                        Color color;
                        switch (msg.Severity) {
                            case LogSeverity.Critical:
                                color = Discord.Color.DarkRed;
                                break;
                            case LogSeverity.Error:
                                color = Discord.Color.Red;
                                break;
                            case LogSeverity.Warning:
                                color = Discord.Color.Gold;
                                break;
                            case LogSeverity.Verbose:
                                color = Discord.Color.Blue;
                                break;
                            case LogSeverity.Info:
                                color = Discord.Color.Blue;
                                break;
                            default:
                                color = new Color(255, 255, 255);
                                break;
                        }
                        var embed = new EmbedBuilder()
                            .WithColor(color)
                            .WithFooter(msg.Severity.ToString())
                            .WithTimestamp(val.Item2);
                        embed.Description = "**" + msg.Source + "**: " + text;

                        try {
                            await SendMessage("", channel, embed.Build());
                        }
                        catch (HttpException e) {
                            Console.WriteLine("Failed to send log message:" + Environment.NewLine + Environment.NewLine + e.ToString());
                        }
                    }
                }
                catch (Exception) {
                    Console.WriteLine("Warning: Could not write to Discord logs channel! Queuing.");
                    return;
                }
                if (LogQueue.Count != 0)
                    LogQueue.Dequeue();
            }
        }

        public async Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            LogQueue.Enqueue(new Tuple<LogMessage, DateTime>(msg, DateTime.UtcNow));

            await SendLog();
        }


        public async Task<RestUserMessage> SendMessage(string message, ISocketMessageChannel channel, Embed embed = null) {
            if (testMode) return null;
            
            // sends the message and returns it
            return await channel.SendMessageAsync(message, false, embed);
        }
        
        public ISocketMessageChannel GetChannel(ulong id) {
            if (testMode) return null;

            // the bot is designed to only be in one server
            return CurGuild.GetTextChannel(id);
        }

        public SocketGuildUser GetUser(ulong userId) {
            if (testMode) return null;

            return CurGuild.GetUser(userId);
        }

        public async Task AddRole(ulong userId, ulong roleId) {
            if (testMode) return;

            await AddRole(GetUser(userId), roleId);
        }

        public async Task AddRole(SocketGuildUser user, ulong roleId) {
            if (testMode) return;

            await user.AddRoleAsync(CurGuild.GetRole(roleId));
        }

        public async Task RemoveRole(ulong userId, ulong roleId) {
            if (testMode) return;

            await RemoveRole(GetUser(userId), roleId);
        }

        public async Task RemoveRole(SocketGuildUser user, ulong roleId) {
            if (testMode) return;

            await user.RemoveRoleAsync(CurGuild.GetRole(roleId));
        }

        public async Task RemoveRoles(ulong userId, IEnumerable<ulong> roleIds) {
            if (testMode) return;

            await RemoveRoles(GetUser(userId), roleIds);
        }

        public async Task RemoveRoles(SocketGuildUser user, IEnumerable<ulong> roleIds) {
            if (testMode) return;

            var roles = new List<SocketRole>();
            foreach (var id in roleIds) {
                roles.Add(CurGuild.GetRole(id));
            }
            await user.RemoveRolesAsync(roles);
        }

        public async Task<IMessage> GetMessage(ulong messageId, ulong channelId) {
            if (testMode) return null;

            var channel = GetChannel(channelId);
            return await channel.GetMessageAsync(messageId);
        }

        public async Task<IMessage> GetMessage(ulong messageId, ISocketMessageChannel channel) {
            if (testMode) return null;

            return await channel.GetMessageAsync(messageId);
        }


        public async Task EditMessage(ulong messageId, ulong channelId, string newText, Embed embed = null) {
            if (testMode) return;

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
            if (testMode) return;

            await msg.ModifyAsync(m => {
                m.Content = newText;
                m.Embed = embed;
            });
        }

        List<SocketGuildUser> deafened = new List<SocketGuildUser>();

        // Only undeafens users when they were deafened by the bot, and not someone else.
        public async Task SafeUndeafen(SocketGuildUser user) {
            if (testMode) return;
            
            if (deafened.Contains(user)) { 
                await UndeafenUser(user);
                deafened.Remove(user);
            }
        }

        public async Task UndeafenUser(SocketGuildUser user) {
            if (testMode) return;
            
            if (user.VoiceChannel != null)
                await user.ModifyAsync(p => {
                    p.Deaf = false;
                    p.Mute = false;
                });
        }

        public async Task DeafenUser(SocketGuildUser user) {
            if (testMode) return;

            if (user.VoiceChannel != null)
                await user.ModifyAsync(p => {
                    p.Deaf = true;
                    p.Mute = true;
                });
        }

        // permissions integers
        const ulong sendInt = 2048;
        const ulong botInt = 224336;
        public async Task<RestCategoryChannel> CreateCategory(string name) {
            if (testMode) return null;

            var cat = await CurGuild.CreateCategoryChannelAsync(name);
            await cat.AddPermissionOverwriteAsync(CurGuild.EveryoneRole, new OverwritePermissions(0, sendInt));
            await cat.AddPermissionOverwriteAsync(client.CurrentUser, new OverwritePermissions(botInt, 0));
            return cat;
        }

        public async Task<RestTextChannel> CreateChannel(string name, ulong categoryId = 0, bool everyoneCanSend = false) {
            if (testMode) return null;

            var chnl = await CurGuild.CreateTextChannelAsync(name);
            // deny everyone send message perms
            if (!everyoneCanSend)
                await chnl.AddPermissionOverwriteAsync(CurGuild.EveryoneRole, new OverwritePermissions(0, sendInt));
            // give bot perms to send and manage
            await chnl.AddPermissionOverwriteAsync(client.CurrentUser, new OverwritePermissions(botInt, 0));

            if (categoryId != 0) await chnl.ModifyAsync(p => p.CategoryId = categoryId);

            return chnl;
        }

        public async Task DeleteChannel(RestTextChannel chnl) {
            if (testMode) return;

            await chnl.DeleteAsync();
        }

        public async Task PopulateChannel(ulong channelId, string[] text) {
            if (testMode) return;

            try {
                var chnl = GetChannel(channelId);
                if (chnl == null) return;
                
                var messages = await chnl.GetMessagesAsync(text.Length).FlattenAsync();
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