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
            this.ImageRepository = imageRepository;
            this.ConfigurationProvider = configurationProvider;
            this.DatabaseFileRepository = databaseFileRepository;
            this.SecurityProvider = securityProvider;
        }

        public void AcceptMessage(Creating<DatabaseFile> message)
        {
            if (this.ConfigurationProvider.GetBool(ConfigurationNames.IMPORT_IMAGES_AUTOMATICALLY))
            {
                this.ImportImage(message.Target);
            }
        }

        public void ImportImage(DatabaseFile target)
        {
            if (this.DatabaseFileRepository is null)
            {
                return;
            }

            if (MimeMappings.GetMimeType(System.IO.Path.GetExtension(target.FullName)).StartsWith("image/", System.StringComparison.OrdinalIgnoreCase))
            {
                Image newImage = this.ImageRepository.GetByUri(target.FullName).FirstOrDefault() ?? new Image(target.FullName);
                newImage.Refresh();

                this.SecurityProvider.ClonePermissions(target, newImage);

                this.ImageRepository.AddOrUpdate(newImage);
            }
        }
    }
}