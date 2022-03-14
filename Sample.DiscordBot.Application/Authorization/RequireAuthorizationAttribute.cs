using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Users;

namespace Sample.DiscordBot.Authorization
{
    public class RequireAuthorizationAttribute : PreconditionAttribute
    {
        public string Permission { get; }

        public RequireAuthorizationAttribute(string permission = null)
        {
            Permission = permission;
        }

        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var currentUser = services.GetRequiredService<ICurrentUser>();
            if (currentUser?.Id == null)
            {
                // Failed to resolve user
                return PreconditionResult.FromError(
                    "You can not use this command because your discord account is not linked yet.\n");
            }

            if (string.IsNullOrEmpty(Permission))
            {
                return PreconditionResult.FromSuccess();
            }

            var permissionChecker = services.GetRequiredService<IPermissionChecker>();
            var isGranted = await permissionChecker.IsGrantedAsync(Permission);

            if (!isGranted)
            {
                return PreconditionResult.FromError(
                    $"You do not have access to this command (missing permission: {Permission}).");
            }

            return PreconditionResult.FromSuccess();
        }
    }
}