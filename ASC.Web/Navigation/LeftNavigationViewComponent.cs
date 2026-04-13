using ASC.Web.Data;
using ASC.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace ASC.Web.Navigation
{
    [ViewComponent(Name = "ASC.Web.Navigation.LeftNavigation")]
    public class LeftNavigationViewComponent : ViewComponent
    {
        private readonly INavigationCacheOperations _navigationCacheOperations;

        public LeftNavigationViewComponent(INavigationCacheOperations navigationCacheOperations)
        {
            _navigationCacheOperations = navigationCacheOperations;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var menu = await _navigationCacheOperations.GetNavigationCacheAsync();

            if (menu == null)
            {
                menu = new NavigationRoot
                {
                    MenuItems = new List<NavigationMenuItem>()
                };
            }

            menu.MenuItems = menu.MenuItems?
                .OrderBy(p => p.Sequence)
                .ToList() ?? new List<NavigationMenuItem>();

            return View(menu);
        }
    }
}