namespace Sample.DiscordBot.Permissions
{
    using Volo.Abp.Authorization.Permissions;

    public class DiscordBotPermissionDefinitionProvider : PermissionDefinitionProvider
    {
        public override void Define(IPermissionDefinitionContext context)
        {
            if (context.GetGroupOrNull(DiscordBotPermissions.GroupName) != null)
            {
                return;
            }

            var discordPermisssionGroup = context.AddGroup(DiscordBotPermissions.GroupName);
            discordPermisssionGroup.AddPermission(DiscordBotPermissions.Commands.Echo);
        }
    }
}