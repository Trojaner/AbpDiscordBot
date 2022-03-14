using System.Security.Claims;
using Discord;
using Volo.Abp.DependencyInjection;

namespace Sample.DiscordBot.Authentication
{
    public class DiscordUserResolver : IDiscordUserResolver, ITransientDependency
    {
        public Task<ClaimsIdentity> ResolveAsync(IUser user)
        {
            throw new NotImplementedException();
        }
    }
}