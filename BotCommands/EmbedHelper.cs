using Discord;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenTrueskillBot.BotCommands {
    public static class EmbedHelper {

        public static Embed GenerateSuccessEmbed(string text, string footer = null) {
            var builder = new EmbedBuilder() {
                Color = new Color(28, 189, 71),
            };
            builder.Description = $":white_check_mark: {text}";
            builder.WithCurrentTimestamp();
            if (footer != null) {
                builder.WithFooter(footer);
            }
            return builder.Build();
        }

        public static Embed GenerateInfoEmbed(string text, string footer = null) {
            var builder = new EmbedBuilder() {
                Color = Discord.Color.Blue
            };
            builder.Description = text;
            if (footer != null) {
                builder.WithFooter(footer);
            }
            return builder.Build();
        }

        public static Embed GenerateErrorEmbed(string text) {
            var builder = new EmbedBuilder() {
                Color = Discord.Color.Red
            };
            builder.Description = $":x: {text}";
            return builder.Build();
        }

    }
}
