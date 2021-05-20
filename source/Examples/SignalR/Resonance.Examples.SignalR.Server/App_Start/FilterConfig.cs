using System.Web;
using System.Web.Mvc;

namespace Resonance.Examples.SignalR.Server
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
