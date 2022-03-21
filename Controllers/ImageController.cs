using Microsoft.AspNetCore.Mvc;
using Penguin.Cms.Images;
using Penguin.Cms.Images.Repositories;
using Penguin.Cms.Modules.Dynamic.Attributes;
using Penguin.Images.Extensions;
using Penguin.Persistence.Abstractions.Attributes.Control;
using Penguin.Reflection.Serialization.Abstractions.Interfaces;
using System;
using System.IO;
using System.Reflection;
using Drawing = System.Drawing;

namespace Penguin.Cms.Modules.Images.Controllers
{
    public partial class ImageController : Controller
    {
        protected ImageRepository ImageRepository { get; set; }

        public ImageController(ImageRepository imageRepository)
        {
            this.ImageRepository = imageRepository;
        }

        public ActionResult DisplayThumb(Image i)
        {
            if (i != null)
            {
                return this.View("DisplayThumb", i);
            }
            else
            {
                return this.Content("No thumb found");
            }
        }

        [DynamicPropertyHandler(DisplayContexts.Edit, typeof(Image), nameof(Image.Thumb))]
        public ActionResult DynamicDisplayThumb(IMetaObject Model) => this.View(Model);

        public ActionResult GetFullImage(int id)
        {
            Image thisImage = this.ImageRepository.Find(id) ?? throw new NullReferenceException($"Can not find image with Id {id}");

            ImageData thisImageData = thisImage.Full ?? throw new Exception($"No full found for image {id}");

            //if (thisImage.IsNSFW && !this.UserSession.AllowNSFW)
            //{
            //    return this.ReturnNSFW(thisImageData);
            //}

            return this.ReturnImage(thisImageData);
        }

        public ActionResult GetImage(int id)
        {
            Image thisImage = this.ImageRepository.Find(id) ?? throw new NullReferenceException($"Can not find image with Id {id}");

            ImageData thisImageData = thisImage.Full ?? throw new Exception($"No Content found for image {id}");

            //if (thisImage.IsNSFW && !this.UserSession.AllowNSFW)
            //{
            //    return this.ReturnNSFW(thisImageData);
            //}

            return this.ReturnImage(thisImageData);
        }

        public ActionResult GetThumb(int id)
        {
            Image thisImage = this.ImageRepository.Find(id) ?? throw new NullReferenceException($"Can not find image with Id {id}");

            ImageData thisImageData = thisImage.Full ?? throw new Exception($"No Thumb found for image {id}");

            //if (thisImage.IsNSFW && !this.UserSession.AllowNSFW)
            //{
            //    return this.ReturnNSFW(thisImageData);
            //}

            return this.ReturnImage(thisImageData);
        }

        public ActionResult ReturnImage(ImageData thisImage)
        {
            if (thisImage != null && thisImage.Data.Length >= 10)
            {
                byte[] imageData = thisImage.Data;
                return this.File(imageData, thisImage.Mime);
            }
            else
            {
                return this.Redirect("/Content/Image/static.jpg");
            }
        }

        public ActionResult ReturnNSFW(ImageData thisImage)
        {
            if (thisImage != null && thisImage.Data.Length >= 10)
            {
                Assembly _assembly = Assembly.GetExecutingAssembly();
                Stream? _imageStream = _assembly.GetManifestResourceStream("APLoffredo.Content.Images.placeholder.jpg");

                if (_imageStream is null)
                {
                    throw new Exception("Placeholder image stream not found");
                }

                using Drawing.Bitmap original = new Drawing.Bitmap(_imageStream);
                using Drawing.Bitmap resized = new Drawing.Bitmap(original, new Drawing.Size(thisImage.Width, thisImage.Height));
                return this.File(resized.ToByteArray(), "image/jpeg");
            }
            else
            {
                return this.Redirect("/Content/Image/placeholder.jpg");
            }
        }
    }
}