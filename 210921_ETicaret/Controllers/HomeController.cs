using _210921_ETicaret.DB;
using _210921_ETicaret.Filter;
using _210921_ETicaret.Models;
using _210921_ETicaret.Models.index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace _210921_ETicaret.Controllers
{
    public class HomeController : BaseController
    {
        // GET: Home
        [HttpGet]
        public ActionResult Index(int id = 0)
        {
            IQueryable<DB.Products> products = context.Products.OrderByDescending(x => x.AddedDate).Where(x => x.IsDeleted == false || x.IsDeleted == null);
            DB.Categories category = null;

            if (id > 0)
            {
                category = context.Categories.FirstOrDefault(x => x.Id == id);
                var allCategories = GetChildCategories(category);
                allCategories.Add(category);

                var catIntList = allCategories.Select(x => x.Id).ToList();

                products = products.Where(x => catIntList.Contains(x.Category_Id));

            }
            var viewModel = new Models.index.IndexModel
            {
                Products = products.ToList(),
                Category = category
            };
            return View(viewModel);
        }

        [HttpGet]
        public ActionResult Product(int id = 0)
        {
            var pro = context.Products.FirstOrDefault(x => x.Id == id);

            if (pro == null) return RedirectToAction("index", "Home");

            ProductModels model = new ProductModels()
            {
                Product = pro,
                Comments = pro.Comments.ToList()
            };
            return View(model);
        }

        [HttpPost]
        [MyAuthorization]
        public ActionResult Product(DB.Comments comment)
        {
            try
            {
                comment.Member_Id = base.CurrentUserId();
                comment.AddedDate = DateTime.Now;
                context.Comments.Add(comment);
                context.SaveChanges();
            }
            catch (Exception ex)
            {
                ViewBag.MyError = ex.Message;
            }

            return RedirectToAction("Product", "Home");
        }

        [HttpGet]
        public ActionResult AddBasket(int id, bool remove = false)
        {
            List<Models.index.BasketModels> basket = null;

            if (Session["Basket"] == null)
            {
                basket = new List<Models.index.BasketModels>();
            }
            else
            {
                basket = (List<Models.index.BasketModels>)Session["Basket"];
            }

            if (basket.Any(x => x.Product.Id == id))
            {
                var pro = basket.FirstOrDefault(x => x.Product.Id == id);

                if (remove && pro.Count > 0)
                {
                    pro.Count -= 1;

                }

                else
                {
                    if (pro.Product.UnitsInStock > pro.Count)
                    {
                        pro.Count += 1;
                    }
                    else
                    {
                        TempData["MyError"] = "Yeterli Stok Bulunmamaktadır";
                    }
                }

            }

            else
            {
                var pro = context.Products.FirstOrDefault(x => x.Id == id);

                if (pro != null && pro.IsContinued && pro.UnitsInStock > 0)
                {
                    basket.Add(new Models.index.BasketModels()
                    {
                        Count = 1,
                        Product = pro
                    });
                }

                else if (pro != null && pro.IsContinued == false)
                {
                    TempData["MyError"] = "Bu Ürünün Satışı Durduruldu";
                }
            }
            basket.RemoveAll(x => x.Count < 1);
            Session["Basket"] = basket;
            return RedirectToAction("Basket", "Home");
        }

        [HttpGet]
        public ActionResult Basket()
        {

            List<Models.index.BasketModels> model = (List<Models.index.BasketModels>)Session["Basket"];

            if (model == null)
            {
                model = new List<Models.index.BasketModels>();
            }

            if (base.IsLogOn())
            {
                int currentId = CurrentUserId();
                ViewBag.CurrentAddresses = context.Addresses
                                           .Where(x => x.Member_Id == currentId)
                                           .Select(x => new SelectListItem()
                                           {
                                               Text = x.Name,
                                               Value = x.Id.ToString()

                                           }).ToList();
            }

            ViewBag.TotalPrice = model.Select(x => x.Product.Price * x.Count).Sum();
            return View(model);
        }

        [HttpGet]
        public ActionResult RemoveBasket(int id)
        {
            List<Models.index.BasketModels> basket = (List<Models.index.BasketModels>)Session["Basket"];
            if (basket != null)
            {
                if (id > 0)
                {
                    basket.RemoveAll(x => x.Product.Id == id);
                }
                else if (id == 0)
                {
                    basket.Clear();
                }
                Session["Basket"] = basket;
            }

            return RedirectToAction("Basket", "Home");
        }

        [HttpPost]
        public ActionResult Buy(string Address)
        {
            if (IsLogOn())
            {
                try
                {
                    var basket = (List<Models.index.BasketModels>)Session["Basket"];
                    var guid = new Guid(Address);
                    var _address = context.Addresses.FirstOrDefault(x => x.Id == guid);
                    //Sipariş Verildi : SV
                    //Ödeme Bildirimi : OB
                    //Ödeme Onaylandı : OO
                    var order = new DB.Orders()
                    {
                        AddedDate = DateTime.Now,
                        Address = _address.AdresDescription,
                        Member_Id = CurrentUserId(),
                        Status = "SV",
                        Id = Guid.NewGuid()
                    };

                    foreach (Models.index.BasketModels item in basket)
                    {
                        var oDetail = new DB.OrderDetails();
                        oDetail.AddedDate = DateTime.Now;
                        oDetail.Price = item.Product.Price * item.Count;
                        oDetail.Product_Id = item.Product.Id;
                        oDetail.Quantity = item.Count;
                        oDetail.Id = Guid.NewGuid();

                        order.OrderDetails.Add(oDetail);
                        var _product = context.Products.FirstOrDefault(x => x.Id == item.Product.Id);

                        if (_product != null && _product.UnitsInStock >= item.Count)
                        {
                            _product.UnitsInStock = _product.UnitsInStock - item.Count;
                        }

                        else
                        {
                            throw new Exception(string.Format("{0} ürünü için yeterli stok yoktur veya silinmiş biri ürünü almaya çalışıyorsunuz.", item.Product.Name));
                        }
                    }

                    context.Orders.Add(order);
                    context.SaveChanges();
                    Session["Basket"] = null;
                }
                catch (Exception ex)
                {

                    TempData["MyError"] = ex.Message;
                }

                return RedirectToAction("Buy", "Home");
            }
            else return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        [MyAuthorization]
        public ActionResult Buy()
        {
            if (IsLogOn())
            {
                var currentId = CurrentUserId();
                IQueryable<DB.Orders> orders;
                if (((int)CurentUser().MemberType) > 8)
                {
                    orders = context.Orders.Where(x => x.Status == "OB");
                }
                else
                {
                    orders = context.Orders.Where(x => x.Member_Id == currentId);
                }

                List<Models.index.BuyModels> model = new List<BuyModels>();

                foreach (var item in orders)
                {
                    var byModel = new BuyModels();
                    byModel.TotalPrice = item.OrderDetails.Sum(y => y.Price);
                    byModel.OrderName = string.Join(",", item.OrderDetails.Select(y => y.Products.Name + "(" + y.Quantity + ")"));
                    byModel.OrderStatus = item.Status;
                    byModel.OrderId = item.Id.ToString();
                    byModel.Member = item.Members;
                    model.Add(byModel);
                }

                return View(model);
            }
            else return RedirectToAction("Login", "Account");

        }

        [HttpPost]
        [MyAuthorization]
        public JsonResult OrderNotification(OrderNotificationModel model)
        {
            if (string.IsNullOrEmpty(model.OrderId) == false)
            {
                var guid = new Guid(model.OrderId);
                var order = context.Orders.FirstOrDefault(x => x.Id == guid);

                if (order != null)
                {
                    order.Description = model.OrderDescription;
                    order.Status = "OB";
                    context.SaveChanges();
                }

            }
            return Json("");
        }

        [HttpGet]
        public JsonResult GetProductDes(int id)
        {
            var pro = context.Products.FirstOrDefault(x => x.Id == id);
            return Json(pro.Description, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetOrderDes(string id)
        {
            var guid = new Guid(id);
            var order = context.Orders.FirstOrDefault(x => x.Id == guid);
            return Json(order.Description, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [MyAuthorization]
        public JsonResult OrderComplete(string id, string text)
        {
            var guid = new Guid(id);
            var order = context.Orders.FirstOrDefault(x => x.Id == guid);
            order.Description = text;
            order.Status = "OO";
            context.SaveChanges();
            return Json(true, JsonRequestBehavior.AllowGet);
        }
    }
}