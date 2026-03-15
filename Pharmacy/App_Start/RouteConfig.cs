using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;


namespace Pharma
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Guest", action = "Guest_Home", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Seller",
                url: "Seller/{action}/{id}",
                defaults: new { controller = "Seller", action = "SellerLogin", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Admin",
                url: "Admin/{action}/{id}",
                defaults: new { controller = "Admin", action = "AdminLogin", id = UrlParameter.Optional }
            );
        }
    }
}
