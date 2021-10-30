using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace _210921_ETicaret.Models.index
{
    public class BuyModels
    {
        public string OrderId { get; set; }
        public string OrderName { get; set; }
        public decimal TotalPrice { get; set; }
        public string OrderStatus { get; set; }
        public DB.Members Member { get; set; }

    }
}