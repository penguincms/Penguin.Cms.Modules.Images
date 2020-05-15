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
using System.Diagnostics.CodeAnalysis;
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

        public ImageController(IRepository<AuditableError> errorRepository, ImageRepository imageRepository, IServiceProvider serviceProvider, ImageService imageService, ISecurityProvider<Image> securityProvider = null, IRepository<DatabaseFile>? databaseFileRepository = null) : base(serviceProvider)
        {
            this.ImageService = imageService;
            this.SecurityProvider = securityProvider;
            this.ErrorRepository = errorRepository;
            this.ImageRepository = imageRepository;
            this.DatabaseFileRepository = databaseFileRepository;
        }

        public ActionResult ImportImages(string FilePath)
        {
            List<string> output = this.ScrapeImages(FilePath);
            return this.View("IndexOutput", output);
        }

        [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
        public List<string> ScrapeImages(string FilePath)
        {
            if (this.DatabaseFileRepository is null)
            {
                return new List<string>();
            }

            List<string> output = new List<string>();

            DirectoryInfo TargetPath = new DirectoryInfo(FilePath);

            using (IWriteContext context = this.DatabaseFileRepository.WriteContext())
            {
                output.Add($"Directory: {TargetPath}");

                if (!TargetPath.Exists)
                {
                    TargetPath.Create();
                }

                List<DatabaseFile> imageFiles = this.DatabaseFileRepository.Where(f => f.FilePath == FilePath && !f.IsDirectory && MimeMappings.GetType(f.FileName) == MimeMappings.FileType.Image).ToList();

                output.Add($"Total Images: {imageFiles.Count}");

                List<Image> databaseImages = this.ImageRepository.Get().ToList();

                output.Add($"Total Saved Images: {databaseImages.Count}");

                foreach (Image thisImage in databaseImages)
                {
                    DatabaseFile match = imageFiles.Where(f => string.Equals(f.FullName, thisImage.Uri, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                    if (match == null)
                    {
                        output.Add($"Deleting Missing Image: {thisImage.Uri}");

                        this.ImageRepository.Delete(thisImage);
                    }
                    else
                    {
                        if (thisImage.IsDeleted == true)
                        {
                            output.Add($"Re-adding Image: {match.FullName}");
                        }

                        this.ImageRepository.Delete(thisImage);
                        imageFiles.Remove(match);
                    }
                }

                foreach (DatabaseFile thisFile in imageFiles)
                {
                    try
                    {
                        output.Add($"Adding New Image: {thisFile.FullName}");
                        this.ImageService.ImportImage(thisFile);
                    }
                    catch (Exception ex)
                    {
                        this.ErrorRepository.TryAdd(ex);
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

                using (IWriteContext context = this.ImageRepository.WriteContext())
                {
                    byte[] array;
                    // save the image path path to the database or you can send image
                    // directly to database
                    // in-case if you want to store byte[] ie. for DB
                    using (MemoryStream ms = new MemoryStream())
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
                        this.SecurityProvider?.SetPublic(thisImage);
                    }

                    thisImage.Refresh();

                    this.ImageRepository.AddOrUpdate(thisImage);
                }
                thisImage = this.ImageRepository.Find(thisImage.Guid);

                return this.Json(new
                {
                    uploaded = 1,
                    fileName = upload.FileName,
                    url = $"/Image/GetFullImage/{thisImage._Id}"
                });
            }
            // after successfully uploading redirect the user
            return this.Json(new
            {
                uploaded = 0
            });
        }
    }
}