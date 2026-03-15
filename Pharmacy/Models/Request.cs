using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Pharma.Models
{
    public class Request
    {
        public int RequestID { get; set; }
        public int CustomerID { get; set; }
        public string MedicineName { get; set; }
        public DateTime RequestDate { get; set; }
        public string Status { get; set; }
    }
}