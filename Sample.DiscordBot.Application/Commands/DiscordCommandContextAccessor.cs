using Discord.Commands;
using Volo.Abp.DependencyInjection;

namespace Sample.DiscordBot.Commands
{
    public class DiscordCommandContextAccessor : IDiscordCommandContextAccessor, IScopedDependency
    {
        public ICommandContext CommandContext { get; set; }
        public int ArgsPos { get; set; }
    }
}