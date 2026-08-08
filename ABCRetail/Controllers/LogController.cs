using ABCRetail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Controllers
{
    public class LogController : Controller
    {
        private readonly IFileStorageService _fileService;

        public LogController(
            IFileStorageService fileService)
        {
            _fileService = fileService;
        }

        public async Task<IActionResult> Index()
        {
            List<string> files =
                await _fileService.GetLogFilesAsync();

            return View(files);
        }

        public async Task<IActionResult> Details(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return BadRequest();
            }

            string content =
                await _fileService.ReadLogAsync(fileName);

            ViewBag.FileName = fileName;

            return View("Details", content);
        }
    }
}
