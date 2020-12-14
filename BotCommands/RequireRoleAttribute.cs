using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace OpenTrueskillBot.BotCommands
{

    // Inherit from PreconditionAttribute
    // NOTE: Taken directly and modified from Discord.NET documentation.
    public class RequirePermittedRole : PreconditionAttribute
    {

        // Override the CheckPermissions method
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            // Check if this user is a Guild User, which is the only context where roles exist
            if (context.User is SocketGuildUser gUser)
            {
                // If this command was executed by a user with the appropriate role, return a success
                if (gUser.Roles.Any(r => Program.Config.PermittedRoleNames.Contains(r.Name.Trim().ToLower())))
                    // Since no async work is done, the result has to be wrapped with `Task.FromResult` to avoid compiler errors
                    return Task.FromResult(PreconditionResult.FromSuccess());
                // Since it wasn't, fail
                else
                    return Task.FromResult(PreconditionResult.FromError($"You do not have permission to run this command."));
            }
            else
                return Task.FromResult(PreconditionResult.FromError("You must be in a guild to run this command."));
        
        }
    }
}