using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Pharma.Models
{
    public class OrderItem
    {
        public int MedicineID { get; set; }
        public string MedicineName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total => Quantity * Price;
    }

    public class SellerDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalMedicines { get; set; }
        public int TotalOrders { get; set; }
        public List<Medicine> LatestMedicines { get; set; }
        public int PendingOrdersCount { get; set; }
    }
}