using ABCRetail.Models;
using ABCRetail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Controllers
{
    public class ProductController : Controller
    {
        private readonly ITableStorageService _tableStorageService;
        private readonly IBlobStorageService _blobStorageService;
        private readonly IQueueStorageService _queueStorageService;
        private readonly ILogger _logger;

        public ProductController(
            ITableStorageService tableStorageService,
            IBlobStorageService blobStorageService,
            IQueueStorageService queueStorageService,
            ILogger<ProductController> logger)
        {
            _tableStorageService = tableStorageService;
            _blobStorageService = blobStorageService;
            _queueStorageService = queueStorageService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                List<ProductEntity> products =
                    await _tableStorageService.GetProductsAsync();

                return View(products);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "An error occurred while retrieving products.");

                TempData["ErrorMessage"] =
                    "Products could not be loaded.";

                return View(new List<ProductEntity>());
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new ProductViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            ProductViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string? uploadedImageUrl = null;

            try
            {
                if (model.ImageFile == null)
                {
                    ModelState.AddModelError(
                        nameof(model.ImageFile),
                        "Please select a product image.");

                    return View(model);
                }

                uploadedImageUrl =
                    await _blobStorageService.UploadImageAsync(
                        model.ImageFile);

                var product = new ProductEntity
                {
                    PartitionKey = "Product",
                    RowKey = Guid.NewGuid().ToString(),
                    ProductName = model.ProductName,
                    Description = model.Description,
                    Category = model.Category,
                    Price = model.Price,
                    QuantityInStock = model.QuantityInStock,
                    ImageUrl = uploadedImageUrl
                };

                await _tableStorageService.AddProductAsync(product);

                await _queueStorageService.SendMessageAsync(
                    $"Inventory update - Product: {product.ProductName}, " +
                         $"Quantity in stock: {product.QuantityInStock}");

                TempData["SuccessMessage"] =
                    "The product and image were saved successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException exception)
            {
                ModelState.AddModelError(
                    nameof(model.ImageFile),
                    exception.Message);

                return View(model);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "An error occurred while creating a product.");

                // Remove the uploaded image if saving the table
                // entity failed.
                if (!string.IsNullOrWhiteSpace(uploadedImageUrl))
                {
                    await _blobStorageService.DeleteImageAsync(
                        uploadedImageUrl);
                }

                ModelState.AddModelError(
                    string.Empty,
                    "The product could not be saved.");

                return View(model);
            }
        }
    }
}
