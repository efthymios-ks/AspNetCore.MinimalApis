using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Samples.MinimalApis.Views.ProductList;

public sealed class ProductListModel : PageModel
{
    public string[] Products { get; private set; } = [];

    public void OnGet()
        => Products =
        [
            "Smartphone",
            "Laptop",
            "Wireless Headphones",
            "Mechanical Keyboard",
            "USB-C Hub"
        ];
}
