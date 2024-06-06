using DatabaseAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ModelClasses.ViewModel;
using ModelClasses;
using Microsoft.Extensions.Hosting;

namespace Inazuma.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _HostEnvironment;
        public ProductController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context; // Corrected assignment
            _HostEnvironment = hostEnvironment;
        }

        public IActionResult Index()
        {
            var products = _context.Products.Include(u=> u.category).ToList();
            return View(products);
        }

        public IActionResult Create()
        {
            var productsVM = new ProductVM
            {
                Inventories = new Inventory(),
                PImages = new PImages(),
                Products = new Product(), // Inisialisasi Products
                CategoriesList = _context.categories.ToList().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                })
            };
            return View(productsVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductVM productVM)
        {
            string homeImageUrl = "";
            if (productVM.Images != null)
            {
                foreach (var image in productVM.Images)
                {
                    homeImageUrl = image.FileName;
                    if (homeImageUrl.Contains("Home"))
                    {
                        homeImageUrl = UploadFiles(image); // Metode UploadFiles yang sudah diperbarui
                        break;
                    }
                }
            }
            productVM.Products.HomeImgUrl = homeImageUrl; // Pastikan HomeImgUrl sama dengan nama file yang diunggah

            await _context.AddAsync(productVM.Products);
            await _context.SaveChangesAsync();
            var NewProduct = await _context.Products.Include(u => u.category).FirstOrDefaultAsync(u => u.Name == productVM.Products.Name);
            productVM.Inventories.Name = NewProduct.Name;
            productVM.Inventories.Category = NewProduct.category.Name;
            await _context.Inventories.AddAsync(productVM.Inventories);
            await _context.SaveChangesAsync();

            if (productVM.Images != null)
            {
                foreach (var image in productVM.Images)
                {
                    string tempFileName = image.FileName;
                    if (!tempFileName.Contains("Home"))
                    {
                        string stringFileName = UploadFiles(image); // Metode UploadFiles yang sudah diperbarui
                        var addressImage = new PImages
                        {
                            ImageUrl = stringFileName,
                            ProductId = NewProduct.Id,
                            ProductName = NewProduct.Name
                        };
                        await _context.PImages.AddAsync(addressImage);
                    }
                }
            }
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Product");
        }

        [HttpGet]
        public IActionResult Edit(int Id)
        {
            ProductVM productsVM = new ProductVM();
            {
                productsVM.Products = _context.Products.FirstOrDefault(p => p.Id == Id);
                productsVM.CategoriesList = _context.categories
                    .ToList()
                    .Select(u => new SelectListItem
                    {
                        Text = u.Name,
                        Value = u.Id.ToString()
                    });
            };
            productsVM.Products.ImgUrls = _context.PImages
                .Where(u => u.ProductId == Id)
                .ToList();
            return View(productsVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ProductVM productVM)
        {
            var ProductToEdit = _context.Products.FirstOrDefault(u => u.Id == productVM.Products.Id);
            if (ProductToEdit != null)
            {
                ProductToEdit.Name = productVM.Products.Name;
                ProductToEdit.Price = productVM.Products.Price;
                ProductToEdit.Description = productVM.Products.Description;
                ProductToEdit.CategoryId = productVM.Products.CategoryId;

                if (productVM.Images != null)
                {
                    foreach (var item in productVM.Images)
                    {
                        string tempFileName = item.FileName;
                        if (!tempFileName.Contains("Home"))
                        {
                            string stringFileName = UploadFiles(item); // Metode UploadFiles yang sudah diperbarui
                            var image = new PImages
                            {
                                ImageUrl = stringFileName,
                                ProductId = productVM.Products.Id,
                                ProductName = productVM.Products.Name
                            };
                            _context.PImages.Add(image);
                        }
                        else
                        {
                            if (ProductToEdit.HomeImgUrl == "")
                            {
                                string homeImageUrl = item.FileName;
                                if (homeImageUrl.Contains("Home"))
                                {
                                    homeImageUrl = UploadFiles(item); // Metode UploadFiles yang sudah diperbarui
                                    ProductToEdit.HomeImgUrl = homeImageUrl;
                                }
                            }
                        }
                    }
                }
                _context.Products.Update(ProductToEdit);
                _context.SaveChanges();

                return RedirectToAction("Index", "Product");
            }

            return NotFound();
        }

        [HttpDelete]
        public IActionResult Delete(int Id)
        {
            if (Id != 0)
            {
                var productToDelete = _context.Products.FirstOrDefault(x => x.Id == Id);
                var ImagesTodelete = _context.PImages.Where(u => u.ProductId == Id).Select(u => u.ImageUrl);
                foreach (var image in ImagesTodelete)
                {
                    string imageUrl = "Images\\" + image;
                    var toDelteImageFromFolder = Path.Combine(_HostEnvironment.WebRootPath, imageUrl.TrimStart('\\'));
                    DeleteAImage(toDelteImageFromFolder);
                }
                if (productToDelete.HomeImgUrl != "")
                {
                    string imageUrl = "Images\\" + productToDelete.HomeImgUrl;
                    var toDelteImageFromFolder = Path.Combine(_HostEnvironment.WebRootPath, imageUrl.TrimStart('\\'));
                    DeleteAImage(toDelteImageFromFolder);
                }
                _context.Products.Remove(productToDelete);
                _context.SaveChanges();
            }
            else
            {
                return Json(new {success = false, message = "Failed to delete the item"});
            }
            return Json(new { success = true, message = "Delete successfully" });
        }

        public IActionResult DeleteAImg(string Id)
        {
            int routeId = 0;

            if (Id != null)
            {
                if (!Id.Contains("Home"))
                {
                    var ImageToDeleteFromPImage = _context.PImages.FirstOrDefault(u => u.ImageUrl == Id);

                    if (ImageToDeleteFromPImage != null)
                    {
                        routeId = ImageToDeleteFromPImage.ProductId;
                        _context.PImages.Remove(ImageToDeleteFromPImage);
                    }
                }
                else
                {
                    var ImageToDeleteFromProduct = _context.Products.FirstOrDefault(u => u.HomeImgUrl == Id);

                    if (ImageToDeleteFromProduct != null)
                    {
                        ImageToDeleteFromProduct.HomeImgUrl = "";
                        routeId = ImageToDeleteFromProduct.Id;
                        _context.Products.Update(ImageToDeleteFromProduct);
                    }
                }
                string ImageUrl = "Images\\" + Id;
                var toDeleteImageFromFolder = Path.Combine(_HostEnvironment.WebRootPath, ImageUrl);
                DeleteAImage(toDeleteImageFromFolder);
                _context.SaveChanges();

                return Json(new { success = true, message = "Picture was deleted successfully", id = routeId });
            }

            return Json(new { success = false, message = "Failed to delete the item." });
        }


        private void DeleteAImage(string imagePath)
        {
            if (System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
            }
        }


        private string UploadFiles(IFormFile image)
        {
            string fileName = null;
            if (image != null)
            {
                string uploadDirLocation = Path.Combine(_HostEnvironment.WebRootPath, "Images");

                fileName = image.FileName; // Gunakan nama file asli tanpa tambahan GUID atau apapun
                string filePath = Path.Combine(uploadDirLocation, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    image.CopyTo(fileStream);
                }
            }
            return fileName;
        }
    }
}
