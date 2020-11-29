using Discord;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenTrueskillBot.BotCommands {
    public static class EmbedHelper {

        public static Embed GenerateSuccessEmbed(string text) {
            var builder = new EmbedBuilder() {
                Color = new Color(28, 189, 71),
            };
            builder.AddField(x => {
                x.Name = $":white_check_mark: {text}.";
            });
            return builder.Build();
        }

    }
}
