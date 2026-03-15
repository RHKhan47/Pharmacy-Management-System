using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web.Mvc;
using Pharma.Models;

namespace Pharma.Controllers
{
    public class UserController : Controller
    {
        private PharmacyContext db = new PharmacyContext();

        // GET: User/Login & Signup
        public ActionResult UserLogin()
        {
            return View();
        }

        public ActionResult UserSignup()
        {
            return View();
        }

        public ActionResult User_Offer()
        {
            if (Session["UserID"] == null)
            {
                TempData["Error_User"] = "You need to login first.";
                return RedirectToAction("UserLogin");
            }

            return View();
        }

        public ActionResult UserAbout()
        {
            if (Session["UserID"] == null)
            {
                TempData["Error_User"] = "You need to login first.";
                return RedirectToAction("UserLogin");
            }

            return View();
        }

        // POST: User/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UserLogin(string username, string password)
        {
            if (ModelState.IsValid)
            {
                var user = db.Customers.SingleOrDefault(u => u.Username == username && u.Password == password);
                if (user != null)
                {
                    Session["UserID"] = user.CustomerID;
                    Session["Username"] = user.Username;
                    // Assuming 'user' is your user object
                    Session["UserEmail"] = user.Email;
                    return RedirectToAction("Dashboard");
                }
                else
                {
                    ModelState.AddModelError("", "Invalid username or password.");
                }
            }
            return View();
        }

        // POST: User/Signup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UserRegister(Customer userReg)
        {
            if (ModelState.IsValid)
            {
                // Check if username is already taken
                var existingUser = db.Customers.FirstOrDefault(u => u.Username == userReg.Username);
                if (existingUser != null)
                {
                    ModelState.AddModelError("", "Username already exists. Please choose a different one.");
                    return View();
                }

                // Map UserReg to Customer model
                var customer = new Customer
                {
                    FullName = userReg.FullName,
                    Address = userReg.Address,
                    Phone = userReg.Phone,
                    Username = userReg.Username,
                    Password = userReg.Password,
                    Email = userReg.Email
                };

                // Add the new customer to the database
                db.Customers.Add(customer);
                db.SaveChanges();
                return RedirectToAction("UserLogin");
            }
            return View();
        }

        // GET: User/Logout
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("UserLogin", "User");
        }

        // GET: User/Dashboard
        public ActionResult Dashboard()
        {
            if (Session["UserID"] == null)
            {
                TempData["Error_User"] = "You need to login first.";
                return RedirectToAction("UserLogin");
            }

            return View();
        }

        // GET: User/ViewMedicines
        public ActionResult ViewMedicines()
        {
            if (Session["UserID"] == null)
            {
                TempData["Error_User"] = "You need to login first.";
                return RedirectToAction("UserLogin");
            }

            var medicines = db.Medicines.ToList();
            return View(medicines);
        }

        // GET: User/MedicineDetails
        public ActionResult MedicineDetails(int id)
        {

            var medicine = db.Medicines.Find(id); // Fetch medicine by ID
            if (medicine == null)
            {
                return HttpNotFound();
            }
            return View(medicine);
        }

        // POST: User/PlaceOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PlaceOrder(List<int> medicineIds, List<int> quantities)
        {
            if (medicineIds == null || !medicineIds.Any())
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No items in the cart.");
            }

            var cartMedicines = db.Medicines.Where(m => medicineIds.Contains(m.MedicineID)).ToList();

            if (!cartMedicines.Any())
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid medicines in the cart.");
            }

            int customerId = (int)Session["UserID"];

            // Create a new order
            var order = new Order
            {
                CustomerID = customerId,
                OrderDate = DateTime.Now,
                Status = OrderStatus.Pending,
                TotalPrice = 0,
                OrderDetails = new List<OrderDetail>()
            };

            // Add order details for each medicine in the cart
            for (int i = 0; i < cartMedicines.Count && i < quantities.Count; i++)
            {
                var medicine = cartMedicines[i];
                var quantity = quantities[i];

                if (medicine.Quantity < quantity)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"Not enough stock for {medicine.MedicineName}.");
                }

                // Reduce the quantity of the medicine
                medicine.Quantity -= quantity;

                // Create an order detail
                var orderDetail = new OrderDetail
                {
                    MedicineID = medicine.MedicineID,
                    Quantity = quantity,
                    Price = medicine.Price
                };
                order.OrderDetails.Add(orderDetail);
            }

            // Calculate the total price of the order
            order.TotalPrice = order.OrderDetails.Sum(od => od.Price * od.Quantity);

            // Save the order and update the medicine quantities in the database
            db.Orders.Add(order);
            db.SaveChanges();

            // Clear the cart after placing the order
            Session["Cart"] = null;

            TempData["OrderMessage"] = "Order placed successfully.";
            return RedirectToAction("ViewMedicines");


        }

        // GET: User/RequestMedicine
        public ActionResult RequestMedicine()
        {
            return View();
        }

        // POST: User/RequestMedicine
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RequestMedicine(string medicineName)
        {
            var request = new MedicineRequest
            {
                CustomerID = (int)Session["UserID"],
                MedicineName = medicineName,
                RequestDate = DateTime.Now
            };

            db.MedicineRequests.Add(request);
            db.SaveChanges();

            TempData["Success"] = "Medicine request submitted successfully.";
            return RedirectToAction("Dashboard");
        }

        // POST: User/AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddToCart(int medicineId)
        {
            var medicine = db.Medicines.Find(medicineId);
            if (medicine == null)
            {
                return HttpNotFound();
            }

            List<int> cart;
            if (Session["Cart"] == null)
            {
                cart = new List<int>();
            }
            else
            {
                cart = (List<int>)Session["Cart"];
            }

            cart.Add(medicine.MedicineID);
            Session["Cart"] = cart;

            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        // GET: User/ViewCart
        public ActionResult ViewCart()
        {
            if (Session["UserID"] == null)
            {
                TempData["Error_User"] = "You need to login first.";
                return RedirectToAction("UserLogin");
            }

            List<Medicine> cartMedicines = new List<Medicine>();
            decimal totalPrice = 0;

            if (Session["Cart"] != null)
            {
                List<int> cart = (List<int>)Session["Cart"];
                cartMedicines = db.Medicines.Where(m => cart.Contains(m.MedicineID)).ToList();
                totalPrice = cartMedicines.Sum(m => m.Price);
            }
            ViewBag.TotalPrice = totalPrice;
            return View(cartMedicines);
        }

        [HttpPost]
        public ActionResult RemoveFromCart(int id)
        {
            if (Session["Cart"] != null)
            {
                List<int> cart = (List<int>)Session["Cart"];
                cart.Remove(id);
                if (cart.Count == 0)
                {
                    Session["Cart"] = null;
                }
                else
                {
                    Session["Cart"] = cart;
                }
            }

            return Json(new { success = true });
        }

        // GET: User/Profile
        public new ActionResult Profile()
        {
            if (Session["UserID"] == null)
            {
                TempData["Error_User"] = "You need to login first.";
                return RedirectToAction("UserLogin");
            }

            int customerId = (int)Session["UserID"];
            var customerDetails = db.Customers.FirstOrDefault(c => c.CustomerID == customerId);
            return View(customerDetails);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProfileChange(Customer profcng)
        {
            if (ModelState.IsValid)
            {
                int customerId = (int)Session["UserID"];
                var customer = db.Customers.SingleOrDefault(c => c.CustomerID == customerId);

                if (customer != null)
                {
                    customer.FullName = !string.IsNullOrEmpty(profcng.FullName) ? profcng.FullName : customer.FullName;
                    customer.Address = !string.IsNullOrEmpty(profcng.Address) ? profcng.Address : customer.Address;
                    customer.Phone = !string.IsNullOrEmpty(profcng.Phone) ? profcng.Phone : customer.Phone;

                    db.SaveChanges();
                }
            }

            return RedirectToAction("Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PassChange(string CurrentPassword, string NewPassword)
        {
            if (ModelState.IsValid)
            {
                int customerId = (int)Session["UserID"];
                var customer = db.Customers.SingleOrDefault(c => c.CustomerID == customerId);

                if (customer != null)
                {
                    // Validate if the entered current password matches the one stored in the database
                    if (customer.Password == CurrentPassword)
                    {
                        // If the current password is correct, update to the new password
                        customer.Password = NewPassword;
                        db.SaveChanges();
                        TempData["Success_Password"] = "Password updated successfully.";
                    }
                    else
                    {
                        // If the current password is incorrect, return an error
                        TempData["Error_Password"] = "The current password you entered is incorrect.";
                        return RedirectToAction("Profile");
                    }
                }
                else
                {
                    TempData["noUser"] = "User not found.";
                    return RedirectToAction("Profile");
                }
            }

            return RedirectToAction("Profile");
        }




        // GET: User/OrderHistory
        [HttpGet]
        public ActionResult OrderHistory()
        {
            if (Session["UserID"] == null)
            {
                TempData["Error_User"] = "You need to login first.";
                return RedirectToAction("UserLogin");
            }

            int customerId = (int)Session["UserID"];
            var orderMedicines = db.Orders.Where(c => c.CustomerID == customerId).Include(o => o.OrderDetails).ToList();
            return View(orderMedicines);
        }

        [HttpPost]
        public ActionResult OrderHistory(string search)
        {
            int customerId = (int)Session["UserID"];
            OrderStatus status;
            bool isValidStatus = Enum.TryParse(search, true, out status);
            DateTime searchDate;

            if (!string.IsNullOrEmpty(search))
            {
                var orderMedicines = db.Orders.Where(c => c.CustomerID == customerId).Include(o => o.OrderDetails).ToList();
                if (isValidStatus)
                {
                    var orders = orderMedicines.Where(o => o.Status == status).ToList();
                    return View(orders);
                }
                else if (DateTime.TryParse(search, out searchDate))
                {
                    var orders = orderMedicines.Where(o => DbFunctions.TruncateTime(o.OrderDate) == searchDate.Date).ToList();
                    return View(orders);
                }
                else
                {
                    ModelState.AddModelError("", "Invalid search input. Please enter a valid order status or date.");
                    return View(orderMedicines);
                }
            }
            else
            {
                var orderMedicines = db.Orders.Where(c => c.CustomerID == customerId).Include(o => o.OrderDetails).ToList();
                return View(orderMedicines);
            }
        }


        public ActionResult GetOrderDetails(int orderId)
        {
            var order = db.Orders
                          .Include(o => o.OrderDetails.Select(od => od.Medicine))
                          .Include(o => o.Customer)
                          .FirstOrDefault(o => o.OrderID == orderId);

            if (order == null)
            {
                return HttpNotFound();
            }

            // Return JSON with necessary data
            return Json(new
            {
                order.OrderID,
                OrderDate = order.OrderDate.ToString("yyyy-MM-dd"),
                order.TotalPrice,
                Customer = new
                {
                    order.Customer.FullName,
                    order.Customer.Email,
                    order.Customer.Phone,
                    order.Customer.Address
                },
                OrderDetails = order.OrderDetails.Select(od => new
                {
                    od.Medicine.MedicineName,
                    od.Medicine.GenericName,
                    od.Medicine.Manufacturer,
                    od.Medicine.DosageForm,
                    od.Medicine.Strength,
                    od.Quantity,
                    od.Price,
                    od.Medicine.Category
                })
            }, JsonRequestBehavior.AllowGet);
        }


        public ActionResult SendMessage(string subject, string message)
        {
            try
            {
                var userName = Session["UserName"] as string;
                var userEmail = Session["UserEmail"] as string;

                // Ensure the session variables for the user's name and email are set
                if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userEmail))
                {
                    TempData["MessageSent"] = "Error: User information not found in session.";
                    return RedirectToAction("UserContact");
                }

                // Create a new mail message
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("noreply@yourdomain.com"); // Can be a placeholder for Mailtrap
                mail.To.Add("teamflancer47@gmail.com"); // The recipient address
                mail.Subject = subject;
                mail.Body = $"User: {userName}\nEmail: {userEmail}\n\nMessage:\n{message}";
                mail.IsBodyHtml = false;

                // SMTP settings for Mailtrap
                SmtpClient smtpClient = new SmtpClient("smtp.mailtrap.io", 2525)
                {
                    Credentials = new NetworkCredential("a950acb93d1b57", "de5f27609363e0"),
                    EnableSsl = true
                };

                // Send the email
                smtpClient.Send(mail);

                // Success message after sending email
                TempData["MessageSent"] = "Message sent successfully!";
            }
            catch (Exception ex)
            {
                TempData["MessageSent"] = $"Error sending message: {ex.Message}";
            }

            // Stay on the same page
            return RedirectToAction("UserContact");
        }

        public ActionResult UserContact()
        {
            if (Session["UserID"] == null)
            {
                TempData["Error_User"] = "You need to login first.";
                return RedirectToAction("UserLogin");
            }

            return View();
        }
    }
}
