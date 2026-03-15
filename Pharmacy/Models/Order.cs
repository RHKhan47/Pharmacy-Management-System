using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


namespace Pharma.Models
{
    public class Order
    {
        public int OrderID { get; set; }
        public int CustomerID { get; set; }
        public int? SellerID { get; set; } // Nullable SellerID
        public OrderStatus Status { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalPrice { get; set; }
        public List<OrderDetail> OrderDetails { get; set; }

        // Navigation properties
        public virtual Customer Customer { get; set; }
        public virtual Seller Seller { get; set; }
    }


}