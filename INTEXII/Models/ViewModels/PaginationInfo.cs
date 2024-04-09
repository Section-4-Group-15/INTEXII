namespace INTEXII.Models.ViewModels
{
    public class PaginationInfo
    {
        public int TotalItmes { get; set; }
        public int ItemsPerPage { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages => (int)(Math.Ceiling((decimal)TotalItmes / ItemsPerPage));
    }
}





