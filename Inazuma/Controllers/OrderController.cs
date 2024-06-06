using DatabaseAccess;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ModelClasses.ViewModel;
using ModelClasses;
using Inazuma.Utility;
using Stripe.Checkout;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using Stripe;
using Microsoft.Extensions.Options;

namespace Inazuma.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        [BindProperty]
        public OrderDetailsVM OrderDetailsVM { get; set; }

        public OrderController(ApplicationDbContext db, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            _db = db;
            _userManager = userManager;
            _signInManager = signInManager;
        }
        public IActionResult orderDetailPreview()
        {
            var claim = _signInManager.IsSignedIn(User);

            if (claim)
            {
                var userId = _userManager.GetUserId(User);
                var currentUser = _db.applicationUser.FirstOrDefault(x => x.Id == userId);

                SummeryVM summeryVM = new SummeryVM()
                {
                    userCartList = _db.userCarts.Include(u => u.product).Where(u => u.userId.Contains(userId)).ToList(),
                    orderSummery = new UserOrderHeader(),
                    cartUserId = userId
                };

                if (currentUser != null)
                {
                    // Assigning the user's info from the database as default address
                    summeryVM.orderSummery.DeliveryStreetAddress = currentUser.Address;
                    summeryVM.orderSummery.City = currentUser.City;
                    summeryVM.orderSummery.State = currentUser.City; // Corrected from currentUser.City to currentUser.State
                    summeryVM.orderSummery.PostalCode = currentUser.PostalCode;
                    summeryVM.orderSummery.Name = currentUser.FirstName + " " + currentUser.LastName;
                }

                var count = _db.userCarts.Where(u => u.userId.Contains(_userManager.GetUserId(User))).ToList().Count;
                HttpContext.Session.SetInt32(cartCount.sessionCount, count);

                return View(summeryVM);
            }

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Summery(SummeryVM summeryVMFromView)
        {
            var claim = _signInManager.IsSignedIn(User);
            if (claim)
            {
                var userId = _userManager.GetUserId(User);
                var currentUser = _db.applicationUser.FirstOrDefault(x => x.Id == userId);
                SummeryVM summeryVM = new SummeryVM()
                {
                    userCartList = _db.userCarts.Include(u => u.product).Where(u => u.userId.Contains(userId)).ToList(),
                    orderSummery = new UserOrderHeader(),
                };

                if (currentUser != null)
                {
                    summeryVM.orderSummery.Name = summeryVMFromView.orderSummery.Name;
                    summeryVM.orderSummery.DeliveryStreetAddress = summeryVMFromView.orderSummery.DeliveryStreetAddress;
                    summeryVM.orderSummery.City = summeryVMFromView.orderSummery.City;
                    summeryVM.orderSummery.State = summeryVMFromView.orderSummery.State;
                    summeryVM.orderSummery.PostalCode = summeryVMFromView.orderSummery.PostalCode;
                    summeryVM.orderSummery.PhoneNumber = summeryVMFromView.orderSummery.PhoneNumber;
                    summeryVM.orderSummery.DateOfOrder = DateTime.Now;
                    summeryVM.orderSummery.OrderStatus = "Pending";
                    summeryVM.orderSummery.PaymentStatus = "Not Paid";
                    summeryVM.orderSummery.UserId = summeryVMFromView.cartUserId;

                    double totalOrderPrice = 0.0;
                    foreach (var item in summeryVM.userCartList)
                    {
                        double setDecimal = item.Quantity * item.product.Price;
                        totalOrderPrice += setDecimal;
                    }
                    summeryVM.orderSummery.TotalOrderAmount = totalOrderPrice;

                    await _db.AddAsync(summeryVM.orderSummery);
                    await _db.SaveChangesAsync();
                    int orderId = summeryVM.orderSummery.Id;

                    foreach (var item in summeryVM.userCartList)
                    {
                        OrderDetails orderDetail = new OrderDetails()
                        {
                            OrderHeaderId = orderId,
                            ProductId = item.product.Id,
                            Count = item.Quantity,
                            Price = item.product.Price
                        };
                        await _db.orderDetails.AddAsync(orderDetail);
                    }

                    await _db.SaveChangesAsync();

                    var userCartToRemove = _db.userCarts.Where(u => u.userId == userId).ToList();
                    _db.userCarts.RemoveRange(userCartToRemove);
                    await _db.SaveChangesAsync();

                    var count = _db.userCarts.Where(u => u.userId.Contains(userId)).ToList().Count;
                    HttpContext.Session.SetInt32("cartCount", count);

                    return RedirectToAction("Index", "Home");
                }
            }

            return RedirectToAction("Index", "Home");
        }





        public IActionResult OrderCancele(int id)
        {
            var orderProcesseCanceled = _db.orderHeaders.FirstOrDefault(u => u.Id == id);
            _db.orderHeaders.Remove(orderProcesseCanceled);
            _db.SaveChanges();
            return RedirectToAction("CartIndex", "Cart");
        }

        public IActionResult OrderSuccess(int id)
        {
            var userId = _userManager.GetUserId(User);
            var userCartToRemove = _db.userCarts.Where(u => u.userId == userId).ToList();
            var orderProcessed = _db.orderHeaders.FirstOrDefault(h => h.Id == id);
            if (orderProcessed != null)
            {
                if (orderProcessed.PaymentStatus == updateOrderStatus.PaymentStatusNotPaid)
                {
                    var service = new SessionService();
                    Session session = service.Get(orderProcessed.StripeSessionId);
                    if (session.PaymentStatus.ToLower() == updateOrderStatus.PaymentStatusPaid)
                    {
                        orderProcessed.PaymentStatus = updateOrderStatus.PaymentStatusPaid;
                        orderProcessed.PaymentProccessDate = DateTime.Now;
                        orderProcessed.StripePaymentIntendId = session.PaymentIntentId;
                    }
                }
            }
            foreach (var item in userCartToRemove)
            {
                OrderDetails orderReceived = new OrderDetails()
                {
                    OrderHeaderId = orderProcessed.Id,
                    ProductId = (int)item.ProductId,
                    Count = item.Quantity
                };
                _db.orderDetails.Add(orderReceived);
            }
            ViewBag.OrderId = id;
            _db.userCarts.RemoveRange(userCartToRemove);
            _db.SaveChanges();

           HttpContext.Session.Clear();

            return View();
        }

        public IActionResult OrderHistory(string? status)
        {
            var userId = _userManager.GetUserId(User);
            List<UserOrderHeader> orderLists = new List<UserOrderHeader>();

            if (status != null && status.ToUpper() != "ALL")
            {
                if (User.IsInRole("Admin"))
                {
                    orderLists = _db.orderHeaders.Where(u => u.OrderStatus == status).ToList();
                }
                else
                {
                    orderLists = _db.orderHeaders.Where(u => u.OrderStatus == status && u.UserId == userId).ToList();
                }
            }
            else
            {
                if (User.IsInRole("Admin"))
                {
                    orderLists = _db.orderHeaders.ToList();
                }
                else
                {
                    orderLists = _db.orderHeaders.Where(u => u.UserId == userId).ToList();
                }
            }

            return View(orderLists);
        }

        public IActionResult OrderDetails(int orderId)
        {
            var orderDetailsVM = new OrderDetailsVM();
            orderDetailsVM.orderHeader = _db.orderHeaders.FirstOrDefault(u => u.Id == orderId);
            orderDetailsVM.userProductList = _db.orderDetails.Include(u => u.Product).Where(u => u.OrderHeaderId == orderId).ToList();
            return View(orderDetailsVM); 
        }



        public IActionResult InProcess()
        {
            var orderToUpdate = _db.orderHeaders.FirstOrDefault(u => u.Id == OrderDetailsVM.orderHeader.Id);
            if (orderToUpdate != null)
            {
                orderToUpdate.OrderStatus = updateOrderStatus.OrderStatusInProcess;
                _db.orderHeaders.Update(orderToUpdate);
                _db.SaveChanges();
            }
            return RedirectToAction("OrderDetails", new { orderId = OrderDetailsVM.orderHeader.Id });
        }

        public IActionResult Shipped()
        {
            var orderToUpdate = _db.orderHeaders.FirstOrDefault(u => u.Id == OrderDetailsVM.orderHeader.Id);
            if (orderToUpdate != null)
            {
                orderToUpdate.OrderStatus = "Shipped";
                orderToUpdate.Carrier = OrderDetailsVM.orderHeader.Carrier;
                orderToUpdate.TrackingNumber = OrderDetailsVM.orderHeader.TrackingNumber;
                orderToUpdate.DateOfShipped = DateTime.Now;
                _db.orderHeaders.Update(orderToUpdate);
                _db.SaveChanges();
                return RedirectToAction("OrderDetails", new { orderId = OrderDetailsVM.orderHeader.Id });
            }
            else
            {
                return NotFound();
            }
        }

        public IActionResult Delivered()
        {
            var orderToUpdate = _db.orderHeaders.FirstOrDefault(u => u.Id == OrderDetailsVM.orderHeader.Id);
            if (orderToUpdate != null)
            {
                orderToUpdate.OrderStatus = "Completed";
                _db.orderHeaders.Update(orderToUpdate);
                _db.SaveChanges();
                return RedirectToAction("OrderDetails", new { orderId = OrderDetailsVM.orderHeader.Id });
            }
            else
            {
                return NotFound();
            }
        }

        public async Task<IActionResult> Canceled(int Id)
        {
            var orderToUpdate = _db.orderHeaders.FirstOrDefault(u => u.Id == Id);
            if (orderToUpdate != null)
            {
                orderToUpdate.OrderStatus = "Canceled"; // Ubah status pesanan menjadi "Dibatalkan"

                _db.orderHeaders.Update(orderToUpdate);
                await _db.SaveChangesAsync();

                return Json(new { success = true, message = "Order canceled successfully" });
            }
            else
            {
                return Json(new { success = false, message = "Order not found" });
            }
        }

    }
}
