using INTEXII.Models;
using Microsoft.AspNetCore.Mvc;

namespace INTEXII.Components
{
    public class ProductCategoryViewComponent : ViewComponent
    {
        private BrickwellContext _brickwellContext;
        public ProductCategoryViewComponent(BrickwellContext temp)
        {
            _brickwellContext = temp;
        }

        public IViewComponentResult Invoke(List<string> selectedCategories)
        {
            var categoryTypes = _brickwellContext.Products
                .Select(x => x.Category_1)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            ViewData["SelectedCategories"] = selectedCategories;

            return View(categoryTypes);
        }
    }
}
