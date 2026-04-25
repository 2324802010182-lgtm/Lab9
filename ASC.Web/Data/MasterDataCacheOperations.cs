using ASC.Model.BaseTypes;
using ASC.Model.Models;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
namespace ASC.Web.Data
{
    public class MasterDataCacheOperations : IMasterDataCacheOperations
    {
        private readonly IDistributedCache _cache;
        private const string CacheKey = "MasterDataCache";

        public MasterDataCacheOperations(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task CreateMasterDataCacheAsync()
        {
            var data = new MasterDataCache
            {
                Values = new List<MasterDataValue>
        {
            new MasterDataValue
            {
                PartitionKey = MasterKeys.VehicleName.ToString(),
                RowKey = "Car",
                Name = "Car"
            },
            new MasterDataValue
            {
                PartitionKey = MasterKeys.VehicleName.ToString(),
                RowKey = "Bike",
                Name = "Bike"
            },
            new MasterDataValue
            {
                PartitionKey = MasterKeys.VehicleType.ToString(),
                RowKey = "SUV",
                Name = "SUV"
            },
            new MasterDataValue
            {
                PartitionKey = MasterKeys.VehicleType.ToString(),
                RowKey = "Sedan",
                Name = "Sedan"
            }
        }
            };

            var json = JsonSerializer.Serialize(data);

            await _cache.SetStringAsync(CacheKey, json);
        }

        public async Task<MasterDataCache?> GetMasterDataCacheAsync()
        {
            var json = await _cache.GetStringAsync(CacheKey);

            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<MasterDataCache>(json);
        }
    }
}
