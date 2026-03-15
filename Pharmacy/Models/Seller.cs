using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Pharma.Models
{
    public class Seller
    {
        public int SellerID { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
    }
}