using ASC.Web.Models;
namespace ASC.Web.Data
{
    public interface INavigationCacheOperations
    {
        Task<NavigationRoot> GetNavigationCacheAsync();
        Task CreateNavigationCacheAsync();
    }
}