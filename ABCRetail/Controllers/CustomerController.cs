using ABCRetail.Models;
using ABCRetail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Controllers
{
    public class CustomerController : Controller
    {
        private readonly ITableStorageService _tableStorageService;
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(
            ITableStorageService tableStorageService,
            ILogger<CustomerController> logger)
        {
            _tableStorageService = tableStorageService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                List<CustomerProfileEntity> customers =
                    await _tableStorageService.GetCustomersAsync();

                return View(customers);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "An error occurred while retrieving customers.");

                TempData["ErrorMessage"] =
                    "Customers could not be loaded.";

                return View(new List<CustomerProfileEntity>());
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CustomerProfileEntity());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CustomerProfileEntity customer)
        {
            if (!ModelState.IsValid)
            {
                return View(customer);
            }

            try
            {
                customer.PartitionKey = "Customer";
                customer.RowKey = Guid.NewGuid().ToString();

                await _tableStorageService.AddCustomerAsync(customer);

                TempData["SuccessMessage"] =
                    "The customer profile was created successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "An error occurred while creating a customer.");

                ModelState.AddModelError(
                    string.Empty,
                    "The customer could not be saved.");

                return View(customer);
            }
        }
    }
}
