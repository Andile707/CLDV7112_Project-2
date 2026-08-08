using ABCRetail.Models;
using ABCRetail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Controllers
{
    public class OrderController : Controller
    {
        private readonly IQueueStorageService _queueService;
        private readonly IFileStorageService _fileService;

        public OrderController(
            IQueueStorageService queueService,
            IFileStorageService fileService)
        {
            _queueService = queueService;
            _fileService = fileService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            List<string> messages =
                await _queueService.GetMessagesAsync();

            return View(messages);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            OrderViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string message =
                $"Processing Order {model.OrderNumber} - " +
                $"Customer: {model.CustomerName}, " +
                $"Product: {model.ProductName}, " +
                $"Quantity: {model.Quantity}";

            await _queueService.SendMessageAsync(message);

            await _fileService.WriteLogAsync(
                $"Order {model.OrderNumber} was placed.");

            TempData["SuccessMessage"] =
                "Order sent to Azure Queue successfully.";

            await _queueService.SendMessageAsync(message);

            string inventoryMessage =
                $"Inventory update - Product: {model.ProductName}, " +
                $"Quantity ordered: {model.Quantity}";

            await _queueService.SendMessageAsync(inventoryMessage);

            return RedirectToAction(nameof(Index));
        }
    }
}
