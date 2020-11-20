using System;
using System.Threading.Tasks;

namespace OpenTrueskillBot.BotInputs
{
    /// <summary>
    /// Middleman between the process and the standard C# IO.
    /// </summary>
    public class ConsoleBotInput {

        public ConsoleBotInput() {
            ReadBotInput();
        }

        public async void ReadBotInput() {
            while (true) {
                var input = await GetInputAsync();
            }
        }

        private async Task<string> GetInputAsync() {
            return await Task.Run(() => Console.ReadLine());
        }


        /// <summary>
        /// Fires when new data or a command is inputted into the system.
        /// </summary>
        public event EventHandler<BotInputEventArgs> OnBotInput;

        /// <summary>
        /// Outputs text.
        /// </summary>
        /// <param name="output">The text to output.</param>
        public void Output(string output) {

        }
    }
}
