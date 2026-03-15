using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Pharma.Models;

namespace Pharma.Models
{


    public class OrderDetail
    {
        public int OrderDetailID { get; set; }
        public int OrderID { get; set; }
        public int MedicineID { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }

        // Navigation properties
        public virtual Order Order { get; set; }
        public virtual Medicine Medicine { get; set; }
    }

    public enum OrderStatus
    {
        Pending,
        InProcess,
        OutForDelivery,
        Delivered
    }

}


