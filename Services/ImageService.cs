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
using System.Linq;

namespace Penguin.Cms.Modules.Images.Services
{
    public class ImageService : IMessageHandler<Creating<DatabaseFile>>
    {
        public IProvideConfigurations ConfigurationProvider { get; set; }
        public IRepository<DatabaseFile>? DatabaseFileRepository { get; set; }
        public ISecurityProvider<Entity> SecurityProvider { get; set; }
        public ImageRepository ImageRepository { get; set; }

        public ImageService(ImageRepository imageRepository, IProvideConfigurations configurationProvider, ISecurityProvider<Entity> securityProvider, IRepository<DatabaseFile>? databaseFileRepository = null)
        {
            ImageRepository = imageRepository;
            ConfigurationProvider = configurationProvider;
            DatabaseFileRepository = databaseFileRepository;
            SecurityProvider = securityProvider;
        }

        public void AcceptMessage(Creating<DatabaseFile> message)
        {
            if (ConfigurationProvider.GetBool(ConfigurationNames.IMPORT_IMAGES_AUTOMATICALLY))
            {
                ImportImage(message.Target);
            }
        }

        public void ImportImage(DatabaseFile target)
        {
            if (DatabaseFileRepository is null)
            {
                return;
            }

            Image newImage = this.ImageRepository.GetByUri(target.FullName).FirstOrDefault() ?? new Image(target.FullName);
            newImage.Refresh();

            SecurityProvider.ClonePermissions(target, newImage);

            this.ImageRepository.AddOrUpdate(newImage);
        }
    }
}