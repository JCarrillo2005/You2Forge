using System.Diagnostics;

namespace MediaForge.Services;

public class MediaConversionService
{
    private readonly ILogger<MediaConversionService> _logger;

    public MediaConversionService(
        ILogger<MediaConversionService> logger)
    {
        _logger = logger;
    }

    public async Task ConvertToMp3Async(
        string inputPath,
        string outputPath,
        string quality)
    {
        var bitrate = GetAudioBitrate(quality);

        var arguments =
            $"-y " +
            $"-i \"{inputPath}\" " +
            $"-vn " +
            $"-codec:a libmp3lame " +
            $"-b:a {bitrate}k " +
            $"\"{outputPath}\"";

        await ExecuteFfmpegAsync(arguments, outputPath);
    }

    public async Task ConvertToMp4Async(
        string videoPath,
        string audioPath,
        string outputPath)
    {
        var arguments =
            $"-y " +
            $"-i \"{videoPath}\" " +
            $"-i \"{audioPath}\" " +
            $"-map 0:v:0 " +
            $"-map 1:a:0 " +
            $"-c:v copy " +
            $"-c:a aac " +
            $"-b:a 192k " +
            $"-shortest " +
            $"\"{outputPath}\"";

        await ExecuteFfmpegAsync(arguments, outputPath);
    }

    private int GetAudioBitrate(string quality)
    {
        return quality.ToLower() switch
        {
            "64" => 64,
            "128" => 128,
            "192" => 192,
            "320" => 320,
            _ => 128
        };
    }

    private async Task ExecuteFfmpegAsync(
        string arguments,
        string outputPath)
    {
        _logger.LogInformation(
            "Iniciando FFmpeg.");

        _logger.LogInformation(
            "Argumentos: {Arguments}",
            arguments);

        var startInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = arguments,

            RedirectStandardOutput = true,
            RedirectStandardError = true,

            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process
        {
            StartInfo = startInfo
        };

        process.Start();

        // FFmpeg escribe principalmente en stderr.
        // Lo leemos mientras el proceso está trabajando.
        var errorTask =
            process.StandardError.ReadToEndAsync();

        var outputTask =
            process.StandardOutput.ReadToEndAsync();

        await process.WaitForExitAsync();

        var errorOutput =
            await errorTask;

        var standardOutput =
            await outputTask;

        if (process.ExitCode != 0)
        {
            _logger.LogError(
                "FFmpeg terminó con código {ExitCode}.",
                process.ExitCode);

            _logger.LogError(
                "FFmpeg error: {Error}",
                errorOutput);

            throw new Exception(
                $"FFmpeg no pudo procesar el archivo. " +
                $"Código de salida: {process.ExitCode}");
        }

        // Verificar que FFmpeg realmente creó el archivo.
        if (!File.Exists(outputPath))
        {
            throw new Exception(
                "FFmpeg terminó correctamente, " +
                "pero no se encontró el archivo de salida.");
        }

        var fileInfo =
            new FileInfo(outputPath);

        if (fileInfo.Length == 0)
        {
            throw new Exception(
                "FFmpeg creó un archivo vacío.");
        }

        _logger.LogInformation(
            "FFmpeg terminó correctamente.");

        _logger.LogInformation(
            "Archivo generado: {OutputPath}",
            outputPath);

        _logger.LogInformation(
            "Tamaño: {Size} bytes",
            fileInfo.Length);
    }
}