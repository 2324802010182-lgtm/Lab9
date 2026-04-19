namespace ASC.Web.Areas.Configuration.Models
{
    public class MasterKeysViewModel
    {
        // Not posted back from the form (we reload it from session); don't validate as required.
        public List<MasterDataKeyViewModel>? MasterKeys { get; set; }
        public MasterDataKeyViewModel MasterKeyInContext { get; set; } = new MasterDataKeyViewModel();
        public bool IsEdit { get; set; }
    }
}