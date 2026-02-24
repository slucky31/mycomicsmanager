using System.Security.Cryptography;
using Microsoft.AspNetCore.Components;
using Serilog;

namespace Web.Components.Pages;

public sealed partial class Login : IDisposable
{
    [Inject] private IWebHostEnvironment WebHostEnvironment { get; set; } = default!;
    [Inject] private PersistentComponentState ApplicationState { get; set; } = default!;

    private string[]? _allBackgroundImages;
    private string? _selectedImage;
    private const int MaxFiles = 10;
    private PersistingComponentStateSubscription _persistingSubscription;

    protected override void OnInitialized()
    {
        _persistingSubscription = ApplicationState.RegisterOnPersisting(PersistSelectedImage);

        if (!ApplicationState.TryTakeFromJson<string>(nameof(_selectedImage), out var restored))
        {
            _allBackgroundImages ??= LoadBackgroundImages(WebHostEnvironment.WebRootPath);
            var images = _allBackgroundImages;
            if (images.Length == 0)
            {
                _selectedImage = string.Empty;
                return;
            }

            _selectedImage = images[RandomNumberGenerator.GetInt32(images.Length)];
        }
        else
        {
            _selectedImage = restored;
        }
    }

    private Task PersistSelectedImage()
    {
        ApplicationState.PersistAsJson(nameof(_selectedImage), _selectedImage);
        return Task.CompletedTask;
    }

    public void Dispose() => _persistingSubscription.Dispose();

    private static string[] LoadBackgroundImages(string webRootPath)
    {
        try
        {
            var backgroundDir = Path.Combine(webRootPath, "background");
            if (Directory.Exists(backgroundDir))
            {
                return Directory
                    .EnumerateFiles(backgroundDir, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(f => IsValidImageFile(f))
                    .Take(MaxFiles)
                    .Select(f => $"background/{SanitizeFileName(f)}")
                    .ToArray();
            }
        }
        catch (IOException ex)
        {
            Log.Error(ex, "Error loading background images due to I/O issue");
        }
        catch (UnauthorizedAccessException ex)
        {
            Log.Error(ex, "Error loading background images due to access denied");
        }

        return [];
    }

    private static bool IsValidImageFile(string filePath)
    {
        var extension = Path.GetExtension(filePath);

        return
            extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".webp", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
    }

    private static string SanitizeFileName(string fileName)
    {
        // Remove any potentially dangerous characters
        var sanitized = Path.GetFileName(fileName);
        return Uri.EscapeDataString(sanitized);
    }
}
