using ABCRetail.Models;
using ABCRetail.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ABCRetail.Controllers
{
    public class ProductController : Controller
    {
        private readonly ITableStorageService _tableStorageService;
        private readonly IQueueStorageService _queueStorageService;
        private readonly ILogger<ProductController> _logger;
        private readonly HttpClient _httpClient;
        private readonly EventHubService _eventHubService;

        public ProductController(
            ITableStorageService tableStorageService,
            IQueueStorageService queueStorageService,
            ILogger<ProductController> logger,
            EventHubService eventHubService,
            IHttpClientFactory httpClientFactory)
        {
            _tableStorageService = tableStorageService;
            _queueStorageService = queueStorageService;
            _logger = logger;
            _eventHubService = eventHubService;

            _httpClient = httpClientFactory.CreateClient();
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
                if (model.ImageFile == null ||
                    model.ImageFile.Length == 0)
                {
                    ModelState.AddModelError(
                        nameof(model.ImageFile),
                        "Please select a product image.");

                    return View(model);
                }

                // -----------------------------------------
                // Call UploadProductImage Azure Function
                // -----------------------------------------

                using var multipartContent =
                    new MultipartFormDataContent();

                using var fileStream =
                    model.ImageFile.OpenReadStream();

                using var fileContent =
                    new StreamContent(fileStream);

                if (!string.IsNullOrWhiteSpace(
                    model.ImageFile.ContentType))
                {
                    fileContent.Headers.ContentType =
                        new MediaTypeHeaderValue(
                            model.ImageFile.ContentType);
                }

                multipartContent.Add(
                    fileContent,
                    "file",
                    model.ImageFile.FileName);

                HttpResponseMessage response =
                    await _httpClient.PostAsync(
                        "http://localhost:7058/api/UploadProductImage",
                        multipartContent);

                string responseBody =
                    await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "UploadProductImageFunction returned {StatusCode}. " +
                        "Response: {Response}",
                        response.StatusCode,
                        responseBody);

                    ModelState.AddModelError(
                        nameof(model.ImageFile),
                        "The product image could not be uploaded.");

                    return View(model);
                }

                BlobUploadFunctionResponse? functionResponse =
                    JsonSerializer.Deserialize<BlobUploadFunctionResponse>(
                        responseBody,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (functionResponse == null ||
                    !functionResponse.Success ||
                    string.IsNullOrWhiteSpace(
                        functionResponse.BlobUrl))
                {
                    ModelState.AddModelError(
                        nameof(model.ImageFile),
                        "The product image could not be uploaded.");

                    return View(model);
                }

                uploadedImageUrl =
                    functionResponse.BlobUrl;

                // -----------------------------------------
                // Save product information to Table Storage
                // -----------------------------------------

                var product = new ProductEntity
                {
                    PartitionKey = "Product",
                    RowKey = Guid.NewGuid().ToString(),
                    ProductName = model.ProductName,
                    Description = model.Description,
                    Category = model.Category,
                    Price = model.Price,
                    QuantityInStock =
                        model.QuantityInStock,
                    ImageUrl = uploadedImageUrl
                };

                await _tableStorageService
                    .AddProductAsync(product);

                await _eventHubService.SendEventAsync(
    "ProductCreated",
    new
    {
        ProductId = product.RowKey,
        ProductName = product.ProductName,
        Description = product.Description,
        Category = product.Category,
        Price = product.Price,
        QuantityInStock = product.QuantityInStock,
        ImageUrl = product.ImageUrl
    });

                // Existing Project 1 queue behaviour
                await _queueStorageService.SendMessageAsync(
                    $"Inventory update - Product: " +
                    $"{product.ProductName}, " +
                    $"Quantity in stock: " +
                    $"{product.QuantityInStock}");

                TempData["SuccessMessage"] =
    "The product was saved successfully and the activity was sent to Azure Event Hubs.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "An error occurred while creating a product.");

                ModelState.AddModelError(
                    string.Empty,
                    "The product could not be saved.");

                return View(model);
            }
        }
    }
}