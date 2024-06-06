using DatabaseAccess;
using Inazuma.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using ModelClasses.ViewModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Inazuma.Utility;

namespace Inazuma.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db, SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _db = db;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        public IActionResult Index(string? searchByName, string? searchByCategory)
        {
            var claim = _signInManager.IsSignedIn(User);
            if (claim) {
                var userId = _userManager.GetUserId(User);
                var count = _db.userCarts.Where(u => u.userId.Contains(userId)).Count();
                HttpContext.Session.SetInt32(cartCount.sessionCount, count);
            }

            HomePageVM vm = new HomePageVM();

            if (searchByName != null)
            {
                vm.ProductList = _db.Products
                    .Where(product => EF.Functions.Like(product.Name, $"%{searchByName}%"))
                    .ToList();
                vm.Categories = _db.categories.ToList();
            }
            else if (searchByCategory != null)
            {
                var searchByCategoryName = _db.categories.FirstOrDefault(category => category.Name == searchByCategory);

                if (searchByCategoryName != null)
                {
                    vm.ProductList = _db.Products
                        .Where(product => product.CategoryId == searchByCategoryName.Id)
                        .ToList();
                    vm.Categories = _db.categories.Where(category => category.Name.Contains(searchByCategory)).ToList();
                }
            }
            else
            {
                vm.ProductList = _db.Products.ToList();
                vm.Categories = _db.categories.ToList();
            }

            return View(vm);
        }



        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
