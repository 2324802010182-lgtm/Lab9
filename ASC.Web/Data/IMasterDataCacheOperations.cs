namespace ASC.Web.Data
{
    public interface IMasterDataCacheOperations
    {
        Task CreateMasterDataCacheAsync();
        Task<MasterDataCache?> GetMasterDataCacheAsync();
    }
}
