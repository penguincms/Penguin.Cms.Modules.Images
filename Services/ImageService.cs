using Penguin.Cms.Entities;
using Penguin.Cms.Files;
using Penguin.Cms.Images;
using Penguin.Cms.Images.Repositories;
using Penguin.Cms.Modules.Images.Constants.Strings;
using Penguin.Configuration.Abstractions.Extensions;
using Penguin.Configuration.Abstractions.Interfaces;
using Penguin.Messaging.Abstractions.Interfaces;
using Penguin.Messaging.Persistence.Messages;
using Penguin.Persistence.Abstractions.Interfaces;
using Penguin.Security.Abstractions.Interfaces;
using Penguin.Web.Data;
using System.Linq;

namespace Penguin.Cms.Modules.Images.Services
{
    public class ImageService : IMessageHandler<Creating<DatabaseFile>>
    {
        public IProvideConfigurations ConfigurationProvider { get; set; }
        public IRepository<DatabaseFile>? DatabaseFileRepository { get; set; }
        public ImageRepository ImageRepository { get; set; }
        public ISecurityProvider<Entity> SecurityProvider { get; set; }

        public ImageService(ImageRepository imageRepository, IProvideConfigurations configurationProvider, ISecurityProvider<Entity> securityProvider, IRepository<DatabaseFile>? databaseFileRepository = null)
        {
            ImageRepository = imageRepository;
            ConfigurationProvider = configurationProvider;
            DatabaseFileRepository = databaseFileRepository;
            SecurityProvider = securityProvider;
        }

        public void AcceptMessage(Creating<DatabaseFile> message)
        {
            if (message is null)
            {
                throw new System.ArgumentNullException(nameof(message));
            }

            if (ConfigurationProvider.GetBool(ConfigurationNames.IMPORT_IMAGES_AUTOMATICALLY))
            {
                ImportImage(message.Target);
            }
        }

        public void ImportImage(DatabaseFile target)
        {
            if (target is null)
            {
                throw new System.ArgumentNullException(nameof(target));
            }

            if (DatabaseFileRepository is null)
            {
                return;
            }

            if (MimeMappings.GetMimeType(System.IO.Path.GetExtension(target.FullName)).StartsWith("image/", System.StringComparison.OrdinalIgnoreCase))
            {
                Image newImage = ImageRepository.GetByUri(target.FullName).FirstOrDefault() ?? new Image(target.FullName);
                newImage.Refresh();

                SecurityProvider.ClonePermissions(target, newImage);

                ImageRepository.AddOrUpdate(newImage);
            }
        }
    }
}