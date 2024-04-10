
namespace INTEXII.Models.ViewModels
{
    public class ProjectsListViewModel
    {
        public List<Product> Products { get; set; }
        public PaginationInfo PaginationInfo { get; set; } = new PaginationInfo();
        public List<ProductRec> ProductRecs { get; set; }

    }
}