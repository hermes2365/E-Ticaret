using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace _210921_ETicaret.Models.Message
{
    public class MsgModels
    {
        public List<SelectListItem> Users { get; set; }
        public List<DB.Messages> Messages { get; set; }
    }
}