using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace MediaForge.Services;

public class YouTubeService
{
    private readonly IWebHostEnvironment _environment;
    private readonly YoutubeClient _youtube;
    private readonly MediaConversionService _conversionService;

    public YouTubeService(
        IWebHostEnvironment environment,
        MediaConversionService conversionService)
    {
        _environment = environment;
        _conversionService = conversionService;

        _youtube = new YoutubeClient();
    }

    public async Task<string> DownloadVideoAsync(
        string url,
        string format,
        string quality)
    {
        try
        {
            var video =
                await _youtube.Videos.GetAsync(url);

            var streamManifest =
                await _youtube.Videos.Streams
                    .GetManifestAsync(video.Id);

            var downloadsDirectory =
                Path.Combine(
                    _environment.WebRootPath,
                    "downloads");

            var tempDirectory =
                Path.Combine(
                    _environment.WebRootPath,
                    "temp");

            Directory.CreateDirectory(
                downloadsDirectory);

            Directory.CreateDirectory(
                tempDirectory);

            var safeTitle =
                SanitizeFileName(video.Title);

            if (string.Equals(
                format,
                "mp3",
                StringComparison.OrdinalIgnoreCase))
            {
                return await ProcessMp3Async(
                    streamManifest,
                    safeTitle,
                    quality,
                    tempDirectory,
                    downloadsDirectory);
            }

            if (string.Equals(
                format,
                "mp4",
                StringComparison.OrdinalIgnoreCase))
            {
                return await ProcessMp4Async(
                    streamManifest,
                    safeTitle,
                    quality,
                    tempDirectory,
                    downloadsDirectory);
            }

            throw new Exception(
                "Formato no soportado.");
        }
        catch (Exception ex)
        {
            throw new Exception(
                $"Error al procesar el contenido: {ex.Message}",
                ex);
        }
    }

    private async Task<string> ProcessMp3Async(
        StreamManifest manifest,
        string title,
        string quality,
        string tempDirectory,
        string downloadsDirectory)
    {
        var audioStream =
            GetAudioStream(
                manifest,
                quality);

        if (audioStream == null)
        {
            throw new Exception(
                "No se encontró un stream de audio.");
        }

        var uniqueId =
            Guid.NewGuid().ToString("N");

        var tempAudioPath =
            Path.Combine(
                tempDirectory,
                $"{uniqueId}.audio");

        var tempOutputPath =
            Path.Combine(
                tempDirectory,
                $"{uniqueId}.mp3");

        var finalOutputPath =
            GetUniqueOutputPath(
                downloadsDirectory,
                title,
                "mp3");

        try
        {
            // Descargar audio original.
            await _youtube.Videos.Streams
                .DownloadAsync(
                    audioStream,
                    tempAudioPath);

            // Convertir a MP3 dentro de TEMP.
            await _conversionService.ConvertToMp3Async(
                tempAudioPath,
                tempOutputPath,
                quality);

            // Mover solamente cuando FFmpeg terminó.
            MoveCompletedFile(
                tempOutputPath,
                finalOutputPath);

            return $"/downloads/{Uri.EscapeDataString(
                Path.GetFileName(finalOutputPath))}";
        }
        finally
        {
            DeleteFile(tempAudioPath);
            DeleteFile(tempOutputPath);
        }
    }

    private async Task<string> ProcessMp4Async(
        StreamManifest manifest,
        string title,
        string quality,
        string tempDirectory,
        string downloadsDirectory)
    {
        var videoStream =
            GetVideoStream(
                manifest,
                quality);

        if (videoStream == null)
        {
            throw new Exception(
                "No se encontró un stream de video.");
        }

        var audioStream =
            GetAudioStream(
                manifest,
                "128");

        if (audioStream == null)
        {
            throw new Exception(
                "No se encontró un stream de audio.");
        }

        var uniqueId =
            Guid.NewGuid().ToString("N");

        var tempVideoPath =
            Path.Combine(
                tempDirectory,
                $"{uniqueId}.video");

        var tempAudioPath =
            Path.Combine(
                tempDirectory,
                $"{uniqueId}.audio");

        var tempOutputPath =
            Path.Combine(
                tempDirectory,
                $"{uniqueId}.mp4");

        var finalOutputPath =
            GetUniqueOutputPath(
                downloadsDirectory,
                title,
                "mp4");

        try
        {
            // ==========================================
            // DESCARGAR VIDEO
            // ==========================================

            await _youtube.Videos.Streams
                .DownloadAsync(
                    videoStream,
                    tempVideoPath);


            // ==========================================
            // DESCARGAR AUDIO
            // ==========================================

            await _youtube.Videos.Streams
                .DownloadAsync(
                    audioStream,
                    tempAudioPath);


            // ==========================================
            // COMBINAR CON FFMPEG
            // ==========================================

            await _conversionService.ConvertToMp4Async(
                tempVideoPath,
                tempAudioPath,
                tempOutputPath);


            // ==========================================
            // MOVER MP4 TERMINADO
            // ==========================================

            MoveCompletedFile(
                tempOutputPath,
                finalOutputPath);


            return $"/downloads/{Uri.EscapeDataString(
                Path.GetFileName(finalOutputPath))}";
        }
        finally
        {
            DeleteFile(tempVideoPath);
            DeleteFile(tempAudioPath);
            DeleteFile(tempOutputPath);
        }
    }

    private IStreamInfo? GetVideoStream(
        StreamManifest manifest,
        string quality)
    {
        try
        {
            int targetHeight =
                quality.ToLower() switch
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
                manifest
                    .GetVideoStreams()
                    .ToList();

            if (!videoStreams.Any())
            {
                return null;
            }

            var matchingStreams =
                videoStreams
                    .Where(
                        s => s.VideoQuality.MaxHeight
                             == targetHeight)
                    .ToList();

            if (!matchingStreams.Any())
            {
                matchingStreams =
                    videoStreams
                        .OrderByDescending(
                            s => s.VideoQuality.MaxHeight)
                        .ToList();
            }

            return matchingStreams.FirstOrDefault();
        }
        catch
        {
            return manifest
                .GetVideoStreams()
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
            int bitrate =
                quality.ToLower() switch
                {
                    "64" => 64,
                    "128" => 128,
                    "192" => 192,
                    "320" => 320,
                    _ => 128
                };

            var audioStreams =
                manifest
                    .GetAudioStreams()
                    .Where(
                        s => s.Bitrate.KiloBitsPerSecond
                             >= bitrate)
                    .OrderBy(
                        s => s.Bitrate)
                    .ToList();

            if (!audioStreams.Any())
            {
                audioStreams =
                    manifest
                        .GetAudioStreams()
                        .OrderByDescending(
                            s => s.Bitrate)
                        .ToList();
            }

            return audioStreams.FirstOrDefault();
        }
        catch
        {
            return manifest
                .GetAudioStreams()
                .OrderByDescending(
                    s => s.Bitrate)
                .FirstOrDefault();
        }
    }

    private string GetUniqueOutputPath(
        string directory,
        string title,
        string extension)
    {
        var basePath =
            Path.Combine(
                directory,
                $"{title}.{extension}");

        if (!File.Exists(basePath))
        {
            return basePath;
        }

        var counter = 2;

        while (true)
        {
            var path =
                Path.Combine(
                    directory,
                    $"{title} ({counter}).{extension}");

            if (!File.Exists(path))
            {
                return path;
            }

            counter++;
        }
    }

    private void MoveCompletedFile(
        string sourcePath,
        string destinationPath)
    {
        if (!File.Exists(sourcePath))
        {
            throw new Exception(
                "El archivo procesado no existe.");
        }

        var fileInfo =
            new FileInfo(sourcePath);

        if (fileInfo.Length == 0)
        {
            throw new Exception(
                "El archivo procesado está vacío.");
        }

        File.Move(
            sourcePath,
            destinationPath);

        if (!File.Exists(destinationPath))
        {
            throw new Exception(
                "No se pudo mover el archivo procesado.");
        }
    }

    private void DeleteFile(
        string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch (Exception ex)
        {
            // No interrumpir la descarga si falla
            // la limpieza de un archivo temporal.
            Console.WriteLine(
                $"No se pudo eliminar temporal: {ex.Message}");
        }
    }

    private string SanitizeFileName(
        string fileName)
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

        if (fileName.Length > 150)
        {
            fileName =
                fileName[..150];
        }

        return fileName.Trim();
    }
}