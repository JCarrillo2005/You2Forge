using System.Diagnostics;
using MediaForge.Models;
using MediaForge.Services;
using Microsoft.AspNetCore.Mvc;

namespace MediaForge.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly YouTubeService _youtubeService;

    public HomeController(
        ILogger<HomeController> logger,
        YouTubeService youtubeService)
    {
        _logger = logger;
        _youtubeService = youtubeService;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Process(
        [FromBody] MediaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
        {
            return BadRequest(new
            {
                success = false,
                message = "La URL es obligatoria."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Format))
        {
            return BadRequest(new
            {
                success = false,
                message = "El formato es obligatorio."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Quality))
        {
            return BadRequest(new
            {
                success = false,
                message = "La calidad es obligatoria."
            });
        }

        try
        {
            var result =
                await _youtubeService.DownloadVideoAsync(
                    request.Url,
                    request.Format,
                    request.Quality);

            return Ok(new
            {
                success = true,
                message = "Procesamiento completado.",
                file = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error procesando el contenido.");

            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(
        Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true)]
    public IActionResult Error()
    {
        return View(
            new ErrorViewModel
            {
                RequestId =
                    Activity.Current?.Id
                    ?? HttpContext.TraceIdentifier
            });
    }
}