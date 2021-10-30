using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace _210921_ETicaret.Filter
{
    public class MyAuthorizationAttribute : ActionFilterAttribute, IAuthorizationFilter
    {
        public int ActionMemberType { get; private set; }
        public MyAuthorizationAttribute()
        {

        }
        /// <summary>
        /// Varilen numara ve üzerinde kontol yapar
        /// </summary>
        /// <param name="_memberType">Yetki numarası</param>
        public MyAuthorizationAttribute(int _memberType)
        {
            this.ActionMemberType = _memberType;
        }
        public void OnAuthorization(AuthorizationContext filterContext)
        {
            var member = (DB.Members)HttpContext.Current.Session["LogOnUser"];

            if (member == null)
            {
                filterContext.Result = new RedirectResult("Home/Index");
            }

            else
            {
                var memberType = (int)member.MemberType;

                if (memberType < ActionMemberType)
                {
                    filterContext.Result = new RedirectResult("Home/Index");
                }

              
            }
        }
    }
}