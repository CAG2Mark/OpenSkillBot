using System;

namespace OpenSkillBot.BotInputs
{
    /// <summary>
    /// Allows for multiple forms of IO for the bot, including ones that output to third-party services such as Discord.
    /// </summary>
    public interface IBotInput {

        /// <summary>
        /// Fires when new data or a command is inputted into the system.
        /// </summary>
        event EventHandler<BotInputEventArgs> OnBotInput;

        /// <summary>
        /// Outputs text into the console interface.
        /// </summary>
        /// <param name="output">The text to output.</param>
        void OutputPrivate(string output);
        
        /// <summary>
        /// Outputs text into the public, visible section.
        /// </summary>
        /// <param name="output"></param>
        void OutputPublic(string output);
    
    }


    /// <summary>
    /// Extension of EventArgs that includes the parameter for bot inputs.
    /// </summary>
    public class BotInputEventArgs : EventArgs {
        /// <summary>
        /// The inputted text.
        /// </summary>
        public string InputText;
        
    }
}