using System.Security.Claims;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Security.Claims;

namespace Sample.DiscordBot.Authentication
{
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(ICurrentPrincipalAccessor))]
    public class DiscordCurrentPrincipalAccessor : ICurrentPrincipalAccessor, IScopedDependency
    {
        public IDisposable Change(ClaimsPrincipal principal)
        {
            var previous = Principal;
            Principal = principal;
            return new DisposeAction(() => { Principal = previous; });
        }

        public ClaimsPrincipal Principal { get; set; }
    }
}