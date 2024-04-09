
namespace INTEXII.Models.ViewModels
{
    public class ProductViewModel
    {
        public Product Product { get; set; }
        public string CategoryDescription { get; set; }
    }

    public class ProductsListViewModel
    {
        public IEnumerable<ProductViewModel> Products { get; set; }
        public PaginationInfo PaginationInfo { get; set; }
    }

}