using System.ComponentModel.DataAnnotations;

namespace ASC.Web.Areas.Configuration.Models
{
    public class MasterDataKeyViewModel
    {
        // RowKey/PartitionKey are system-generated; don't require them on Create.
        public string? RowKey { get; set; }
        public string? PartitionKey { get; set; }
        public bool IsActive { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;
    }
}