namespace Sample.DiscordBot.Permissions
{
    public class DiscordBotPermissions
    {
        public const string GroupName = "SampleDiscordBot";

        public static class Commands
        {
            public const string Default = GroupName + ".Commands";
            public const string Echo = Default + ".Echo";
        }
    }
}