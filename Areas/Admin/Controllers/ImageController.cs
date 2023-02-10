using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Penguin.Cms.Errors;
using Penguin.Cms.Errors.Extensions;
using Penguin.Cms.Files;
using Penguin.Cms.Images;
using Penguin.Cms.Images.Repositories;
using Penguin.Cms.Modules.Dynamic.Areas.Admin.Controllers;
using Penguin.Cms.Modules.Images.Constants.Strings;
using Penguin.Cms.Modules.Images.Services;
using Penguin.Persistence.Abstractions.Interfaces;
using Penguin.Security.Abstractions.Interfaces;
using Penguin.Web.Data;
using Penguin.Web.Security.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Penguin.Cms.Modules.Images.Areas.Admin.Controllers
{
    [RequiresRole(RoleNames.IMAGE_MANAGER)]
    public partial class ImageController : ObjectManagementController<Image>
    {
        protected IRepository<DatabaseFile>? DatabaseFileRepository { get; set; }

        protected IRepository<AuditableError> ErrorRepository { get; set; }

        protected ImageRepository ImageRepository { get; set; }

        protected ImageService ImageService { get; set; }

        protected ISecurityProvider<Image> SecurityProvider { get; set; }

        public ImageController(IRepository<AuditableError> errorRepository, ImageRepository imageRepository, IServiceProvider serviceProvider, ImageService imageService, IUserSession userSession, ISecurityProvider<Image>? securityProvider = null, IRepository<DatabaseFile>? databaseFileRepository = null) : base(serviceProvider, userSession)
        {
            ImageService = imageService;
            SecurityProvider = securityProvider;
            ErrorRepository = errorRepository;
            ImageRepository = imageRepository;
            DatabaseFileRepository = databaseFileRepository;
        }

        public ActionResult ImportImages(string FilePath)
        {
            List<string> output = ScrapeImages(FilePath);
            return View("IndexOutput", output);
        }

        public List<string> ScrapeImages(string FilePath)
        {
            if (DatabaseFileRepository is null)
            {
                return new List<string>();
            }

            List<string> output = new();

            DirectoryInfo TargetPath = new(FilePath);

            using (IWriteContext context = DatabaseFileRepository.WriteContext())
            {
                output.Add($"Directory: {TargetPath}");

                if (!TargetPath.Exists)
                {
                    TargetPath.Create();
                }

                List<DatabaseFile> imageFiles = DatabaseFileRepository.Where(f => f.FilePath == FilePath && !f.IsDirectory && MimeMappings.GetType(f.FileName) == MimeMappings.FileType.Image).ToList();

                output.Add($"Total Images: {imageFiles.Count}");

                List<Image> databaseImages = ImageRepository.Get().ToList();

                output.Add($"Total Saved Images: {databaseImages.Count}");

                foreach (Image thisImage in databaseImages)
                {
                    DatabaseFile match = imageFiles.Where(f => string.Equals(f.FullName, thisImage.Uri, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                    if (match == null)
                    {
                        output.Add($"Deleting Missing Image: {thisImage.Uri}");

                        ImageRepository.Delete(thisImage);
                    }
                    else
                    {
                        if (thisImage.IsDeleted == true)
                        {
                            output.Add($"Re-adding Image: {match.FullName}");
                        }

                        ImageRepository.Delete(thisImage);
                        _ = imageFiles.Remove(match);
                    }
                }

                foreach (DatabaseFile thisFile in imageFiles)
                {
                    try
                    {
                        output.Add($"Adding New Image: {thisFile.FullName}");
                        ImageService.ImportImage(thisFile);
                    }
                    catch (Exception ex)
                    {
                        _ = ErrorRepository.TryAdd(ex);
                        output.Add($"Error adding image: {thisFile.FullName}");
                    }
                }
            }
            return output;
        }

        [HttpPost]
        public ActionResult Upload(IFormFile upload, bool Public = false)
        {
            if (upload != null)
            {
                Image thisImage;

                using (IWriteContext context = ImageRepository.WriteContext())
                {
                    byte[] array;
                    // save the image path path to the database or you can send image
                    // directly to database
                    // in-case if you want to store byte[] ie. for DB
                    using (MemoryStream ms = new())
                    {
                        upload.OpenReadStream().CopyTo(ms);
                        array = ms.GetBuffer();
                    }

                    thisImage = new Image(array, upload.FileName)
                    {
                        IsVisible = true
                    };

                    if (Public)
                    {
                        SecurityProvider?.SetPublic(thisImage);
                    }

                    thisImage.Refresh();

                    ImageRepository.AddOrUpdate(thisImage);
                }
                thisImage = ImageRepository.Find(thisImage.Guid);

                return Json(new
                {
                    uploaded = 1,
                    fileName = upload.FileName,
                    url = $"/Image/GetFullImage/{thisImage._Id}"
                });
            }
            // after successfully uploading redirect the user
            return Json(new
            {
                uploaded = 0
            });
        }
    }
}