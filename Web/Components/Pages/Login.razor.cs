using System.Security.Cryptography;
using Serilog;

namespace Web.Components.Pages;

public partial class Login
{    

    private static readonly Lazy<string[]> s_backgroundImages = new(() => LoadBackgroundImages());
    private string? _selectedImage;
    private const int MaxFiles = 10;

    protected override void OnInitialized()
    {
        var images = s_backgroundImages.Value;
        if (images.Length == 0)
        {
            _selectedImage = string.Empty;
            return;
        }

        _selectedImage = images[RandomNumberGenerator.GetInt32(images.Length)];
    }

    private static string[] LoadBackgroundImages()
    {
        try
        {
            var backgroundDir = Path.Combine(Environment.CurrentDirectory, "wwwroot", "background");
            if (Directory.Exists(backgroundDir))
            {
                return Directory
                    .EnumerateFiles(backgroundDir, "*.*", SearchOption.TopDirectoryOnly)
                    .Take(MaxFiles)
                    .Where(f => IsValidImageFile(f))
                    .Select(f => $"background/{SanitizeFileName(Path.GetFileName(f))}")
                    .ToArray();
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error loading background images: {ex.Message}");
        }

        return [];
    }

    private static bool IsValidImageFile(string filePath)
    {
        var extension = Path.GetExtension(filePath);
            return  extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
                    extension.Equals(".webp", StringComparison.OrdinalIgnoreCase) ||
                    extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                    extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
    }

    private static string SanitizeFileName(string fileName)
    {
        // Remove any potentially dangerous characters
        var sanitized = Path.GetFileName(fileName);
        return Uri.EscapeDataString(sanitized);
    }
}
