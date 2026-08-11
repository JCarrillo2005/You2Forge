using MediaForge.Models;
using MediaForge.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace MediaForge.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly YouTubeService _youtubeService;

        public HomeController(ILogger<HomeController> logger, YouTubeService youtubeService)
        {
            _logger = logger;
            _youtubeService = youtubeService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Process([FromBody] MediaRequest request)
        {
            try
            {
                // Validate URL 
                if (string.IsNullOrWhiteSpace(request.Url))
                    return BadRequest("La URL es obligatoria.");

                // Validate YouTube URL format
                if (!IsValidYouTubeUrl(request.Url))
                    return BadRequest("La URL no es válida. Debe ser de YouTube.");

                // Download the video
                var downloadPath = await _youtubeService.DownloadVideoAsync(
                    request.Url,
                    request.Format,
                    request.Quality
                );

                // Generate the download URL based on the request
                var downloadUrl = $"{Request.Scheme}://{Request.Host}{downloadPath}";

                return Ok(new
                {
                    message = "¡Descarga completada exitosamente!",
                    downloadUrl = downloadUrl,
                    url = request.Url,
                    format = request.Format,
                    quality = request.Quality
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar la solicitud");
                return BadRequest($"Error: {ex.Message}");
            }
        }

        private bool IsValidYouTubeUrl(string url)
        {
            return url.Contains("youtube.com/watch") ||
                   url.Contains("youtu.be/") ||
                   url.Contains("youtube.com/playlist");
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
