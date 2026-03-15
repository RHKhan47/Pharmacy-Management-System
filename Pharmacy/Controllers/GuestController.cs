using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using static System.Collections.Specialized.BitVector32;
using System.Web.Razor.Parser.SyntaxTree;
using System.Web.UI.WebControls;
using Pharma.Models;

namespace Pharma.Controllers
{
    public class GuestController : Controller
    {
        private PharmacyContext db = new PharmacyContext();

        // GET: Guest
        public ActionResult Guest_Home()
        {
            return View();
        }
        public ActionResult Guest_About()
        {
            return View();
        }
        public ActionResult Guest_Shop()
        {
            var medicines = db.Medicines.ToList();                                         ///View Medicines
            return View(medicines);
        }
        public ActionResult Guest_Contact()
        {
            return View();
        }

        public ActionResult Guest_Offer()
        {
            return View();
        }


        public ActionResult ViewCart()
        {
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


        // POST: Guest/AddToCart
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

            return new HttpStatusCodeResult(System.Net.HttpStatusCode.OK);
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



        // GET: Guest/GuestMedicineDetails
        public ActionResult GuestMedicineDetails(int id)
        {

            var medicine = db.Medicines.Find(id); // Fetch medicine by ID
            if (medicine == null)
            {
                return HttpNotFound();
            }
            return View(medicine);
        }


    }
}