using System.Linq;
using System.Collections.Generic;
using System.Web.Mvc;
using Pharma.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using System;

using System.Data.Entity;
using System.Drawing.Printing;
using System.Drawing;
using System.Xml.Linq;
using System.Net;

namespace Pharma.Controllers
{
    public class SellerController : Controller
    {
        private PharmacyContext db = new PharmacyContext();

        // GET: Seller/SellerLogin
        public ActionResult SellerLogin()
        {
            return View();
        }

        // POST: Seller/SellerLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SellerLogin(string username, string password)
        {
            if (ModelState.IsValid)
            {
                var seller = db.Sellers.SingleOrDefault(s => s.Username == username && s.Password == password);
                if (seller != null)
                {
                    Session["SellerID"] = seller.SellerID;
                    Session["Username"] = seller.Username;
                    return RedirectToAction("Dashboard");
                }
                else
                {
                    ModelState.AddModelError("", "Invalid username or password.");
                }
            }
            return View();
        }

        // GET: Seller/Dashboard
        public ActionResult Dashboard()
        {
            // Ensure SellerID is present in the session
            if (Session["SellerID"] == null)
            {
                TempData["Error"] = "Seller ID is not set. Please log in again.";
                return RedirectToAction("SellerLogin");
            }

            var sellerId = (int)Session["SellerID"];

            var viewModel = new SellerDashboardViewModel
            {
                TotalUsers = db.Customers.AsNoTracking().Count(),
                TotalMedicines = db.Medicines.AsNoTracking().Count(),
                TotalOrders = db.Receipts.AsNoTracking().Count(r => r.SellerID == sellerId),
                LatestMedicines = db.Medicines.AsNoTracking().OrderByDescending(m => m.MedicineID).ToList(),
                PendingOrdersCount = db.Orders.AsNoTracking().Count(o => o.Status == 0)
            };

            return View(viewModel);
        }

        // GET: Seller/ViewMedicines
        public ActionResult ViewMedicines()
        {
            var medicines = db.Medicines.ToList();
            return View(medicines);
        }

        // POST: Seller/AddToList
        [HttpPost]
        public ActionResult AddToList(int medicineId, int quantity)
        {
            var medicine = db.Medicines.Find(medicineId);
            if (medicine != null && medicine.Quantity >= quantity)
            {
                var item = new OrderItem
                {
                    MedicineID = medicine.MedicineID,
                    MedicineName = medicine.MedicineName,
                    Quantity = quantity,
                    Price = medicine.Price,
                    /*Total = quantity * medicine.Price*/ // Ensure Total is calculated
                };

                if (Session["Cart"] == null)
                {
                    Session["Cart"] = new List<OrderItem>();
                }
                var cart = (List<OrderItem>)Session["Cart"];
                cart.Add(item);

                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, message = "The requested quantity is not available." });
            }
        }

        // GET: Seller/ViewCart
        public ActionResult ViewCart()
        {
            var cart = Session["Cart"] as List<OrderItem> ?? new List<OrderItem>();
            return View(cart);
        }

        /*public ActionResult ViewReceipts()
        {
            var receipts = db.Orders.Include(o => o.OrderDetails.Select(od => od.Medicine)).ToList();
            return View(receipts);
        }*/

        public ActionResult ViewReceipts(int page = 1, int pageSize = 10)
        {
            // Ensure SellerID is present in the session
            if (Session["SellerID"] == null)
            {
                TempData["Error"] = "Seller ID is not set. Please log in again.";
                return RedirectToAction("SellerLogin");
            }

            // Retrieve the SellerID from the session
            int sellerId = (int)Session["SellerID"];

            // Fetch receipts that belong to the current seller
            var receiptsQuery = db.Receipts
                                  .Include(r => r.ReceiptItems)
                                  .Where(r => r.SellerID == sellerId);

            // Calculate the total count of receipts for the seller
            int totalReceipts = receiptsQuery.Count();

            // Fetch the appropriate subset of receipts for the current page
            var receipts = receiptsQuery
                           .OrderByDescending(r => r.DateCreated)
                           .Skip((page - 1) * pageSize)
                           .Take(pageSize)
                           .ToList();

            // Set ViewBag properties for pagination
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalReceipts / pageSize);
            ViewBag.CurrentPage = page;

            return View(receipts);
        }


        // POST: Seller/UpdateQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateQuantity(int medicineId, int quantity)
        {
            var cart = Session["Cart"] as List<OrderItem>;
            if (cart != null)
            {
                var item = cart.FirstOrDefault(i => i.MedicineID == medicineId);
                if (item != null)
                {
                    var medicine = db.Medicines.Find(medicineId);
                    if (medicine != null && medicine.Quantity >= quantity)
                    {
                        item.Quantity = quantity;
                        /*item.Total = item.Quantity * item.Price;*/ // Ensure Total is updated
                        return RedirectToAction("ViewCart");
                    }
                    else
                    {
                        TempData["Error"] = "The requested quantity is not available.";
                    }
                }
            }
            return RedirectToAction("ViewCart");
        }

        // POST: Seller/DeleteItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteItem(int medicineId)
        {
            var cart = Session["Cart"] as List<OrderItem>;
            if (cart != null)
            {
                var item = cart.FirstOrDefault(i => i.MedicineID == medicineId);
                if (item != null)
                {
                    cart.Remove(item);
                }
            }
            return RedirectToAction("ViewCart");
        }

        // POST: Seller/PrintReceipt
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PrintReceipt(string buyerName, string buyerPhone)
        {
            if (Session["SellerID"] == null)
            {
                TempData["Error"] = "Seller ID is not set. Please log in again.";
                return RedirectToAction("ViewCart");
            }

            var sellerId = (int)Session["SellerID"];
            var cart = Session["Cart"] as List<OrderItem>;

            if (cart != null && cart.Count > 0)
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var receipt = new Receipt
                        {
                            DateCreated = DateTime.Now,
                            TotalAmount = cart.Sum(item => item.Total),
                            BuyerName = buyerName,
                            BuyerPhone = buyerPhone,
                            SellerID = sellerId
                        };

                        db.Receipts.Add(receipt);
                        db.SaveChanges();

                        foreach (var item in cart)
                        {
                            var receiptItem = new ReceiptItem
                            {
                                ReceiptID = receipt.ReceiptID,
                                MedicineID = item.MedicineID,
                                MedicineName = item.MedicineName,
                                Quantity = item.Quantity,
                                Price = item.Price,
                                Total = item.Total
                            };
                            db.ReceiptItems.Add(receiptItem);

                            var medicine = db.Medicines.Find(item.MedicineID);
                            if (medicine != null)
                            {
                                medicine.Quantity -= item.Quantity;
                            }
                        }
                        db.SaveChanges();

                        transaction.Commit();

                        using (MemoryStream stream = new MemoryStream())
                        {
                            Document pdfDoc = new Document(PageSize.A4, 25f, 25f, 30f, 30f);
                            PdfWriter.GetInstance(pdfDoc, stream).CloseStream = false;
                            pdfDoc.Open();

                            var titleFont = iTextSharp.text.FontFactory.GetFont("Arial", 18, iTextSharp.text.Font.BOLD);
                            var regularFont = iTextSharp.text.FontFactory.GetFont("Arial", 12, iTextSharp.text.Font.NORMAL);
                            var boldFont = iTextSharp.text.FontFactory.GetFont("Arial", 12, iTextSharp.text.Font.BOLD);

                            pdfDoc.Add(new Paragraph("Pharma Store", titleFont) { Alignment = Element.ALIGN_CENTER });
                            pdfDoc.Add(new Paragraph("123 Pharmacy St., Health City", regularFont) { Alignment = Element.ALIGN_CENTER });
                            pdfDoc.Add(new Paragraph("Phone: (123) 456-7890", regularFont) { Alignment = Element.ALIGN_CENTER });
                            pdfDoc.Add(new Paragraph(" ", regularFont));

                            pdfDoc.Add(new Paragraph("Receipt", titleFont) { Alignment = Element.ALIGN_CENTER });
                            pdfDoc.Add(new Paragraph(" ", regularFont));
                            pdfDoc.Add(new Paragraph("Date: " + DateTime.Now.ToString("MM/dd/yyyy"), regularFont) { Alignment = Element.ALIGN_RIGHT });
                            pdfDoc.Add(new Paragraph("Time: " + DateTime.Now.ToString("HH:mm:ss"), regularFont) { Alignment = Element.ALIGN_RIGHT });
                            pdfDoc.Add(new Paragraph(" "));

                            pdfDoc.Add(new Paragraph($"Buyer: {buyerName}", boldFont));
                            pdfDoc.Add(new Paragraph($"Phone: {buyerPhone}", boldFont));
                            pdfDoc.Add(new Paragraph($"Seller ID: {sellerId}", boldFont));
                            pdfDoc.Add(new Paragraph(" "));

                            PdfPTable table = new PdfPTable(4);
                            table.WidthPercentage = 100;
                            table.SetWidths(new float[] { 3f, 1f, 2f, 2f });

                            PdfPCell cell = new PdfPCell(new Phrase("Medicine Name", boldFont));
                            cell.HorizontalAlignment = Element.ALIGN_CENTER;
                            cell.Padding = 5;
                            table.AddCell(cell);

                            cell = new PdfPCell(new Phrase("Quantity", boldFont));
                            cell.HorizontalAlignment = Element.ALIGN_CENTER;
                            cell.Padding = 5;
                            table.AddCell(cell);

                            cell = new PdfPCell(new Phrase("Price", boldFont));
                            cell.HorizontalAlignment = Element.ALIGN_CENTER;
                            cell.Padding = 5;
                            table.AddCell(cell);

                            cell = new PdfPCell(new Phrase("Total", boldFont));
                            cell.HorizontalAlignment = Element.ALIGN_CENTER;
                            cell.Padding = 5;
                            table.AddCell(cell);

                            foreach (var item in cart)
                            {
                                table.AddCell(new PdfPCell(new Phrase(item.MedicineName, regularFont)) { Padding = 5 });
                                table.AddCell(new PdfPCell(new Phrase(item.Quantity.ToString(), regularFont)) { Padding = 5, HorizontalAlignment = Element.ALIGN_RIGHT });
                                table.AddCell(new PdfPCell(new Phrase(item.Price.ToString("C"), regularFont)) { Padding = 5, HorizontalAlignment = Element.ALIGN_RIGHT });
                                table.AddCell(new PdfPCell(new Phrase(item.Total.ToString("C"), regularFont)) { Padding = 5, HorizontalAlignment = Element.ALIGN_RIGHT });
                            }

                            PdfPCell emptyCell = new PdfPCell(new Phrase(""));
                            emptyCell.Border = PdfPCell.NO_BORDER;
                            table.AddCell(emptyCell);
                            table.AddCell(emptyCell);
                            cell = new PdfPCell(new Phrase("Grand Total", boldFont));
                            cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                            cell.Padding = 5;
                            table.AddCell(cell);

                            decimal grandTotal = cart.Sum(i => i.Total);
                            cell = new PdfPCell(new Phrase(grandTotal.ToString("C"), boldFont));
                            cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                            cell.Padding = 5;
                            table.AddCell(cell);

                            pdfDoc.Add(table);
                            pdfDoc.Close();

                            byte[] bytes = stream.ToArray();
                            stream.Close();

                            Session["Cart"] = null;

                            return File(bytes, "application/pdf", "Receipt.pdf");
                        }
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        var errorMessage = $"An error occurred while generating the receipt: {ex.Message}";
                        if (ex.InnerException != null)
                        {
                            errorMessage += $" Inner Exception: {ex.InnerException.Message}";
                            if (ex.InnerException.InnerException != null)
                            {
                                errorMessage += $" Inner Inner Exception: {ex.InnerException.InnerException.Message}";
                            }
                        }
                        TempData["Error"] = errorMessage;
                        return RedirectToAction("ViewCart");
                    }
                }
            }
            else
            {
                TempData["Error"] = "No items in the cart to print.";
                return RedirectToAction("ViewCart");
            }
        }




        // GET: Seller/Orders
        public ActionResult Orders(int page = 1, int pageSize = 10)
        {
            // Filtering the orders to only those that are pending
            var pendingOrders = db.Orders
                                  .Include("Customer")
                                  .Include("OrderDetails.Medicine")
                                  .Where(o => o.Status == OrderStatus.Pending);

            // Calculate total count of pending orders
            int totalOrders = pendingOrders.Count();

            // Fetch the appropriate subset of orders for the current page
            var orders = pendingOrders
                         .OrderBy(o => o.OrderID)
                         .Skip((page - 1) * pageSize)
                         .Take(pageSize)
                         .ToList();

            // Set ViewBag properties for pagination
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalOrders / pageSize);
            ViewBag.CurrentPage = page;

            return View(orders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateOrderStatus(int orderId)
        {
            var order = db.Orders.Find(orderId);
            if (order != null)
            {
                order.Status = OrderStatus.OutForDelivery;
                order.SellerID = (int)Session["SellerID"]; // Assuming SellerID is stored in Session
                db.SaveChanges();
                TempData["SuccessMessage"] = "Order marked as Out For Delivery.";
            }
            else
            {
                TempData["ErrorMessage"] = "Order not found.";
            }
            return RedirectToAction("Orders");
        }


        // GET: Seller/Display_Customer_Seller
        public ActionResult Display_Customer_Seller()
        {
            // Ensure SellerID is present in the session
            if (Session["SellerID"] == null)
            {
                TempData["Error"] = "Seller ID is not set. Please log in again.";
                return RedirectToAction("SellerLogin");
            }

            // Retrieve the list of customers
            var customers = db.Customers.ToList(); // Adjust this as needed based on your customer fetching logic

            return View(customers); // Pass the customer list to the view
        }

        /*// GET: Create Customer Account Seller
        public ActionResult Create_Customer_Account_Seller()
        {
            if (Session["SellerID"] == null)
            {
                TempData["Error"] = "Seller ID is not set. Please log in again.";
                return RedirectToAction("SellerLogin");
            }

            ViewBag.Customers = db.Customers.ToList();
            return View();
        }
*/
        // GET: Seller/Logout
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("SellerLogin");
        }




        // GET: Create Customer Account
        public ActionResult Create_Customer_Account_Seller()
        {
            if (Session["SellerID"] == null)
            {
                TempData["Error"] = "Seller ID is not set. Please log in again.";
                return RedirectToAction("SellerLogin");
            }

            ViewBag.Customers = db.Customers.ToList();
            return View();
        }

        // POST: Create Customer Account
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create_Customer_Account_Seller(Customer customer)
        {
            if (Session["SellerID"] == null)
            {
                TempData["Error"] = "Seller ID is not set. Please log in again.";
                return RedirectToAction("SellerLogin");
            }

            if (ModelState.IsValid)
            {
                // Check if the username, email, or phone number already exists
                var existingCustomer = db.Customers
                    .FirstOrDefault(c => c.Username == customer.Username || c.Email == customer.Email || c.Phone == customer.Phone);

                if (existingCustomer != null)
                {
                    if (existingCustomer.Username == customer.Username)
                    {
                        ModelState.AddModelError("Username", "A customer with this username already exists.");
                    }
                    if (existingCustomer.Email == customer.Email)
                    {
                        ModelState.AddModelError("Email", "A customer with this email already exists.");
                    }
                    if (existingCustomer.Phone == customer.Phone)
                    {
                        ModelState.AddModelError("Phone", "A customer with this phone number already exists.");
                    }
                }
                else
                {
                    db.Customers.Add(customer);
                    db.SaveChanges();
                    return RedirectToAction("Create_Customer_Account_Seller");
                }
            }

            ViewBag.Customers = db.Customers.ToList();
            return View(customer);
        }












        // Action to get customer details
        public ActionResult GetCustomerDetails(int id)
        {
            if (Session["SellerID"] == null)
            {
                return RedirectToAction("SellerLogin");
            }

            var customer = db.Customers.Find(id);
            if (customer == null)
            {
                return HttpNotFound();
            }
            return Json(customer, JsonRequestBehavior.AllowGet);
        }

        // Action to update customer details
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateCustomer(Customer customer)
        {
            if (Session["SellerID"] == null)
            {
                return RedirectToAction("SellerLogin");
            }

            if (ModelState.IsValid)
            {
                var dbCustomer = db.Customers.Find(customer.CustomerID);
                if (dbCustomer == null)
                {
                    return HttpNotFound();
                }

                dbCustomer.Username = customer.Username;
                dbCustomer.Email = customer.Email;
                dbCustomer.FullName = customer.FullName;
                dbCustomer.Address = customer.Address;
                dbCustomer.Phone = customer.Phone;

                db.Entry(dbCustomer).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("Display_Customer_Seller");
            }
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        }

        // Action to delete customer
        [HttpPost]
        public ActionResult DeleteCustomer(int id)
        {
            if (Session["SellerID"] == null)
            {
                return RedirectToAction("SellerLogin");
            }

            var customer = db.Customers.Find(id);
            if (customer == null)
            {
                return HttpNotFound();
            }

            db.Customers.Remove(customer);
            db.SaveChanges();

            return Json(new { success = true });
        }

        // Action to change customer password
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(int customerId, string newPassword)
        {
            if (Session["SellerID"] == null)
            {
                return RedirectToAction("SellerLogin");
            }

            var customer = db.Customers.Find(customerId);
            if (customer == null)
            {
                return HttpNotFound();
            }

            // Update customer password (replace with your hashing logic)
            customer.Password = newPassword;

            db.Entry(customer).State = EntityState.Modified;
            db.SaveChanges();

            return RedirectToAction("Display_Customer");
        }























        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }


    }
}