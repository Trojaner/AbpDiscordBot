using Discord.Commands;

namespace Sample.DiscordBot.Commands
{
    public interface IDiscordCommandContextAccessor
    {
        ICommandContext CommandContext { get; set; }
        int ArgsPos { get; set; }
    }
}