using Penguin.Cms.Modules.Core.Security;
using Penguin.Cms.Modules.Images.Constants.Strings;

namespace Penguin.Cms.Modules.Images.Constants
{
    public static class Roles
    {
        /// <summary>
        /// This role allows a user or group to access the administration panel
        /// </summary>
        public static Role ImageManager { get; } = new Role()
        {
            ExternalId = RoleNames.ImageManager,
            Description = RoleStrings.ImageManager.Description
        };
    }
}