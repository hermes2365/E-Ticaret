using _210921_ETicaret.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace _210921_ETicaret.Controllers
{
    public class BaseController : Controller
    {
        protected ETicaretEntities context { get; private set; }
        public BaseController()
        {
            context = new ETicaretEntities();
            ViewBag.MenuCategories = context.Categories.Where(x => x.Parent_Id == null).ToList();
        }

        protected DB.Members CurentUser()
        {
            if (Session["LogOnUser"] == null) return null;
            return (DB.Members)Session["LogOnUser"];
        }

        protected int CurrentUserId()
        {
            if (Session["LogOnUser"] == null) return 0;
            return ((DB.Members)Session["LogOnUser"]).Id;
        }

        protected bool IsLogOn()
        {
            if (Session["LogOnUser"] == null)
                return false;

            else
                return true;
        }

        /// <summary>
        /// Tüm alt kategorileri getirir
        /// </summary>
        /// <param name="cat">Hangi kategorilerin alt kategorisini getirsin</param>
        /// <returns></returns>
        protected List<Categories> GetChildCategories(Categories cat)
        {
            var result = new List<Categories>();
            result.AddRange(cat.Categories1);
            foreach (var item in cat.Categories1)
            {
                var list = GetChildCategories(item);
                result.AddRange(list);
            }
            return result;
        }
    }
}