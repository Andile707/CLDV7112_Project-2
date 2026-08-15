using ABCRetail.Models;
using ABCRetail.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace ABCRetail.Controllers
{
    public class CustomerController : Controller
    {
        private readonly ITableStorageService _tableStorageService;
        private readonly ILogger<CustomerController> _logger;
        private readonly HttpClient _httpClient;

        public CustomerController(
            ITableStorageService tableStorageService,
            ILogger<CustomerController> logger,
            IHttpClientFactory httpClientFactory)
        {
            _tableStorageService = tableStorageService;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
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
                var functionRequest = new
                {
                    firstName = customer.FirstName,
                    lastName = customer.LastName,
                    email = customer.Email,
                    phoneNumber = customer.PhoneNumber,
                    address = customer.Address
                };

                HttpResponseMessage response =
                    await _httpClient.PostAsJsonAsync(
                        "http://localhost:7058/api/StoreCustomer",
                        functionRequest);

                if (!response.IsSuccessStatusCode)
                {
                    string error =
                        await response.Content.ReadAsStringAsync();

                    _logger.LogError(
                        "StoreCustomerFunction returned {StatusCode}. Response: {Error}",
                        response.StatusCode,
                        error);

                    ModelState.AddModelError(
                        string.Empty,
                        "The customer could not be saved.");

                    return View(customer);
                }

                TempData["SuccessMessage"] =
                    "The customer profile was created successfully using Azure Functions.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "An error occurred while calling StoreCustomerFunction.");

                ModelState.AddModelError(
                    string.Empty,
                    "The customer could not be saved.");

                return View(customer);
            }
        }
    }
}