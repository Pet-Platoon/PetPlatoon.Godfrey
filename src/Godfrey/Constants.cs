using System;
using DSharpPlus.Entities;

namespace Godfrey
{
    public static class Constants
    {
        public static class Quotes
        {
            public const string Loopbacks = "quote.loopbacks";
            public const string Foreigns = "quote.foreigns";
            public const string HideNotSafeForWork = "quote.hidensfw";

            public static class Times
            {
                public const double Downtime = 90;
            }

            public static class Permissions
            {
                public const string Users = "quote.permission.users";
                public const string Roles = "quote.permission.roles";
            }
        }

        public static class Casino
        {
            public const string Channel = "casino.channel";
        }

        public static class Embeds
        {
            public static class Colors
            {
                public static DiscordColor Success => DiscordColor.Green;

                public static DiscordColor Error => DiscordColor.Red;

                public static DiscordColor Output => DiscordColor.Orange;
            }

            public static class Presets
            {
                private static DiscordEmbedBuilder Build(string title, string description = "")
                {
                    return new DiscordEmbedBuilder()
                           .WithTitle(title)
                           .WithDescription(description)
                           .WithTimestamp(DateTime.UtcNow);
                }

                public static DiscordEmbedBuilder Success(string title = "Erfolgreich ausgeführt",
                                                          string description = "")
                {
                    return Build(title, description)
                            .WithColor(Colors.Success);
                }

                public static DiscordEmbedBuilder Error(string title = "Es ist ein Fehler aufgetreten",
                                                        string description = "")
                {
                    return Build(title, description)
                            .WithColor(Colors.Error);
                }

                public static DiscordEmbedBuilder Output(string title = "Information",
                                                         string description = "")
                {
                    return Build(title, description)
                            .WithColor(Colors.Output);
                }
            }
        }
    }
}
