using DatabaseAccess;
using Inazuma.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ModelClasses;
using ModelClasses.ViewModel;

namespace Inazuma.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public CartController(ApplicationDbContext db, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            _db = db;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public IActionResult CartIndex()
        {
            var claim = _signInManager.IsSignedIn(User);
            if(claim)
            {
                var userId = _userManager.GetUserId(User);
                CartIndexVM cartIndexVM = new CartIndexVM()
                {
                    productList = _db.userCarts.Include(u => u.product).Where(u => u.userId.Contains(userId)).ToList(),
                };
                var count = _db.userCarts.Where(u => u.userId.Contains(userId)).Count();
                HttpContext.Session.SetInt32(cartCount.sessionCount, count);

                return View(cartIndexVM);
            }
            return View();
        }

        [Authorize]
        public async Task<IActionResult> AddToCart(int productId, string? returnUrl)
        {
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Home"); // Misalnya, mengarahkan ke halaman utama
            }

            var productAddToCart = await _db.Products.FirstOrDefaultAsync(u => u.Id == productId);
            var CheckIfUserSignedInOrNot = _signInManager.IsSignedIn(User);

            if (CheckIfUserSignedInOrNot)
            {
                var user = await _userManager.GetUserAsync(User);

                if (user != null)
                {
                    var getTheCartIfAnyExistForTheUser = await _db.userCarts.Where(u => u.userId == user.Id).ToListAsync();

                    if (getTheCartIfAnyExistForTheUser.Count() > 0)
                    {
                        var getTheQuantity = getTheCartIfAnyExistForTheUser.FirstOrDefault(p => p.ProductId == productId);

                        if (getTheQuantity != null)
                        {
                            getTheQuantity.Quantity += 1;
                        }
                        else
                        {
                            var newItemToCart = new UserCart
                            {
                                ProductId = productId,
                                userId = user.Id,
                                Quantity = 1
                            };
                            await _db.userCarts.AddAsync(newItemToCart);
                        }
                    }
                    else
                    {
                        var newItemToCart = new UserCart
                        {
                            ProductId = productId,
                            userId = user.Id,
                            Quantity = 1
                        };
                        await _db.userCarts.AddAsync(newItemToCart);
                    }

                    await _db.SaveChangesAsync();
                }
            }
            if (returnUrl != null)
            {
                return RedirectToAction("CartIndex", "Cart");
            }
            return RedirectToAction("Index", "Home");
        }


        public IActionResult MinusAnItem(int productId)
        {
            // Get the item which we need to decrement the quantity
            var itemToMinus = _db.userCarts.FirstOrDefault(u => u.ProductId == productId);

            if (itemToMinus != null)
            {
                if (itemToMinus.Quantity - 1 == 0)
                {
                    _db.userCarts.Remove(itemToMinus);
                }
                else
                {
                    itemToMinus.Quantity -= 1;
                    _db.userCarts.Update(itemToMinus);
                }

                _db.SaveChanges();
            }

            return RedirectToAction(nameof(CartIndex));
        }

        public IActionResult DeleteAnItem(int productId)
        {
            var itemToRemove = _db.userCarts.FirstOrDefault(u => u.ProductId == productId);

            if (itemToRemove != null)
            {
                _db.userCarts.Remove(itemToRemove);
                _db.SaveChanges();
            }

            return RedirectToAction(nameof(CartIndex));
        }
    }

}
