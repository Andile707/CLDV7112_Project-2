using ABCRetail.Models;
using ABCRetail.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace ABCRetail.Controllers
{
    public class OrderController : Controller
    {
        private readonly IQueueStorageService _queueService;
        private readonly ServiceBusService _serviceBusService;
        private readonly HttpClient _httpClient;

        public OrderController(
            IQueueStorageService queueService,
            ServiceBusService serviceBusService,
            IHttpClientFactory httpClientFactory)
        {
            _queueService = queueService;
            _serviceBusService = serviceBusService;
            _httpClient = httpClientFactory.CreateClient();
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

            try
            {
                // -----------------------------------------
                // Function 3:
                // Send order/inventory data to Azure Queue
                // -----------------------------------------

                var queueRequest = new
                {
                    orderNumber = model.OrderNumber,
                    customerName = model.CustomerName,
                    productName = model.ProductName,
                    quantity = model.Quantity
                };

                HttpResponseMessage queueResponse =
                    await _httpClient.PostAsJsonAsync(
                        "http://localhost:7058/api/QueueTransaction",
                        queueRequest);

                if (!queueResponse.IsSuccessStatusCode)
                {
                    string queueError =
                        await queueResponse.Content.ReadAsStringAsync();

                    ModelState.AddModelError(
                        string.Empty,
                        "The order could not be sent to Azure Queue.");

                    return View(model);
                }

                // -----------------------------------------
                // Function 4:
                // Write order log to Azure Files
                // -----------------------------------------

                var logRequest = new
                {
                    message =
                        $"Order {model.OrderNumber} was placed."
                };

                HttpResponseMessage logResponse =
                    await _httpClient.PostAsJsonAsync(
                        "http://localhost:7058/api/WriteLog",
                        logRequest);

                if (!logResponse.IsSuccessStatusCode)
                {
                    string logError =
                        await logResponse.Content.ReadAsStringAsync();

                    ModelState.AddModelError(
                        string.Empty,
                        "The order was queued successfully, " +
                        "but the log file could not be created.");

                    return View(model);
                }

                // -----------------------------------------
                // ICE Task 4:
                // Send order notification to Service Bus
                // -----------------------------------------

                await _serviceBusService.SendMessageAsync(
                    new
                    {
                        MessageType = "OrderConfirmation",
                        OrderNumber = model.OrderNumber,
                        CustomerName = model.CustomerName,
                        ProductName = model.ProductName,
                        Quantity = model.Quantity,
                        Status = "Order received",
                        CreatedAt = DateTime.UtcNow
                    });

                TempData["SuccessMessage"] =
                    "The order was processed successfully and an " +
                    "order notification was sent to Azure Service Bus.";

                return RedirectToAction(nameof(Index));
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The Azure Functions service could not be reached.");

                return View(model);
            }
            catch (Exception)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The order could not be processed.");

                return View(model);
            }
        }
    }
}