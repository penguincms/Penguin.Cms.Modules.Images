using Penguin.Security.Abstractions;

namespace Penguin.Cms.Modules.Images.Constants.Strings
{
    public static class RoleStrings
    {
        /// <summary>
        /// This role allows a user or group to access the administration panel
        /// </summary>
        public static NameDescriptionPair ImageManager { get; } = new NameDescriptionPair()
        {
            Name = RoleNames.ImageManager,
            Description = "This role is grants access to Image Management portions of the web site"
        };
    }
}