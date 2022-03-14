using System.Security.Claims;
using Discord;

namespace Sample.DiscordBot.Authentication
{
    public interface IDiscordUserResolver
    {
        // Get's the ABP identity of a discord user
        // Return null if you fail to resolve a user (e.g. user is not registered or linked yet)
        Task<ClaimsPrincipal> ResolveAsync(IUser user);
    }
}