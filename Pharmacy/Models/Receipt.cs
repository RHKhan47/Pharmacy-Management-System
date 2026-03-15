using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Pharma.Models
{
    public class Receipt
    {
        public int ReceiptID { get; set; }
        public DateTime DateCreated { get; set; }
        public decimal TotalAmount { get; set; }
        public string BuyerName { get; set; } // New property
        public string BuyerPhone { get; set; } // New property
        public int SellerID { get; set; } // New property

        public virtual ICollection<ReceiptItem> ReceiptItems { get; set; }
    }

    public class ReceiptItem
    {
        public int ReceiptItemID { get; set; }
        public int ReceiptID { get; set; }
        public int MedicineID { get; set; }
        public string MedicineName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total { get; set; }

        public virtual Receipt Receipt { get; set; }
    }

}