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
        public IViewComponentResult Invoke()
        {
            var categoryTypes = _brickwellContext.Categories
                .Select(x => x.CategoryDescription)
                .Distinct()
                .OrderBy(x => x);

            return View(categoryTypes);
        }
    }
}
