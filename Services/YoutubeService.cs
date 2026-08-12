using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace MediaForge.Services;

public class YouTubeService
{
    private readonly IWebHostEnvironment _environment;
    private readonly YoutubeClient _youtube;

    public YouTubeService(IWebHostEnvironment environment)
    {
        _environment = environment;
        _youtube = new YoutubeClient();
    }

    public async Task<string> DownloadVideoAsync(
        string url,
        string format,
        string quality)
    {
        try
        {
            // Obtener información del video
            var video = await _youtube.Videos.GetAsync(url);

            // Obtener los streams disponibles
            var streamManifest =
                await _youtube.Videos.Streams.GetManifestAsync(video.Id);

            // Determinar el tipo de stream
            IStreamInfo? stream;

            if (format.Equals("mp4", StringComparison.OrdinalIgnoreCase))
            {
                stream = GetVideoStream(streamManifest, quality);
            }
            else
            {
                stream = GetAudioStream(streamManifest, quality);
            }

            if (stream == null)
            {
                throw new Exception(
                    "No se encontró un stream con la calidad solicitada.");
            }

            // Crear nombre seguro
            var safeTitle = SanitizeFileName(video.Title);

            var fileExtension =
                format.Equals("mp4", StringComparison.OrdinalIgnoreCase)
                    ? "mp4"
                    : "mp3";

            var fileName = $"{safeTitle}.{fileExtension}";

            // Carpeta temporal de descargas
            var downloadDirectory =
                Path.Combine(_environment.WebRootPath, "downloads");

            Directory.CreateDirectory(downloadDirectory);

            var downloadPath =
                Path.Combine(downloadDirectory, fileName);

            // Descargar stream
            await _youtube.Videos.Streams.DownloadAsync(
                stream,
                downloadPath);

            return $"/downloads/{fileName}";
        }
        catch (Exception ex)
        {
            throw new Exception(
                $"Error al procesar el contenido: {ex.Message}",
                ex);
        }
    }

    private IStreamInfo? GetVideoStream(
        StreamManifest manifest,
        string quality)
    {
        try
        {
            int targetHeight = quality.ToLower() switch
            {
                "144" or "144p" => 144,
                "240" or "240p" => 240,
                "360" or "360p" => 360,
                "480" or "480p" => 480,
                "720" or "720p" => 720,
                "1080" or "1080p" => 1080,
                "1440" or "1440p" => 1440,
                "2160" or "2160p" => 2160,
                _ => 720
            };

            var videoStreams =
                manifest.GetVideoStreams().ToList();

            if (!videoStreams.Any())
            {
                return null;
            }

            var matchingStreams = videoStreams
                .Where(s => s.VideoQuality.MaxHeight == targetHeight)
                .ToList();

            // Si no existe exactamente esa resolución,
            // utilizar el stream disponible de mayor calidad.
            if (!matchingStreams.Any())
            {
                matchingStreams = videoStreams
                    .OrderByDescending(
                        s => s.VideoQuality.MaxHeight)
                    .ToList();
            }

            // Priorizar streams que ya contienen audio.
            var streamWithAudio =
                matchingStreams
                    .OfType<MuxedStreamInfo>()
                    .FirstOrDefault();

            return streamWithAudio
                   ?? matchingStreams.FirstOrDefault();
        }
        catch
        {
            var videoStreams =
                manifest.GetVideoStreams().ToList();

            return videoStreams
                .OrderByDescending(
                    s => s.VideoQuality.MaxHeight)
                .FirstOrDefault();
        }
    }

    private IStreamInfo? GetAudioStream(
        StreamManifest manifest,
        string quality)
    {
        try
        {
            int bitrate = quality.ToLower() switch
            {
                "64" => 64,
                "128" => 128,
                "192" => 192,
                "320" => 320,
                _ => 128
            };

            var audioStreams = manifest
                .GetAudioStreams()
                .Where(
                    s => s.Bitrate.KiloBitsPerSecond >= bitrate)
                .OrderBy(s => s.Bitrate)
                .ToList();

            if (!audioStreams.Any())
            {
                audioStreams = manifest
                    .GetAudioStreams()
                    .OrderByDescending(s => s.Bitrate)
                    .ToList();
            }

            return audioStreams.FirstOrDefault();
        }
        catch
        {
            return manifest
                .GetAudioStreams()
                .OrderByDescending(s => s.Bitrate)
                .FirstOrDefault();
        }
    }

    private string SanitizeFileName(string fileName)
    {
        var invalidChars =
            Path.GetInvalidFileNameChars();

        foreach (var character in invalidChars)
        {
            fileName =
                fileName.Replace(
                    character.ToString(),
                    string.Empty);
        }

        return fileName;
    }
}