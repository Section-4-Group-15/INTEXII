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
            var categoryTypes = _brickwellContext.Products
                .Select(x => x.Category1)
                .Distinct()
                .OrderBy(x => x);

            return View(categoryTypes);
        }
    }
}
