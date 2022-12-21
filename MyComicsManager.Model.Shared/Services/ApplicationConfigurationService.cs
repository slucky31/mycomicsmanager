using MyComicsManager.Model.Shared.Models;
using MyComicsManager.Model.Shared.Settings;

namespace MyComicsManager.Model.Shared.Services;

public class ApplicationConfigurationService
{
    private readonly string _applicationRootPath;
    private readonly IApplicationSettings _applicationSettings;

    public ApplicationConfigurationService(IApplicationSettings applicationSettings)
    {
        _applicationSettings = applicationSettings;
        _applicationRootPath = _applicationSettings.ApplicationRootPath;
        if (!_applicationRootPath.EndsWith(Path.DirectorySeparatorChar))
        {
            _applicationRootPath += Path.DirectorySeparatorChar;
        }
    }
    
    public string GetApplicationRootPath()
    {
        return _applicationRootPath;
    }
    
    public string GetEnvironmentName()
    {
        return _applicationSettings.EnvironmentName;
    }
    
    public string GetCloudinaryName()
    {
        return _applicationSettings.CloudinaryName;
    }
    
    public string GetCloudinaryApiKey()
    {
        return _applicationSettings.CloudinaryApiKey;
    }
    
    public string GetCloudinaryApiSecret()
    {
        return _applicationSettings.CloudinaryApiSecret;
    }
    
    public string GetPathLibrairies()
    {
        return _applicationRootPath + ApplicationConfiguration.LibsPath + Path.DirectorySeparatorChar;
    }

    public string GetPathFileImport()
    {
        return _applicationRootPath + ApplicationConfiguration.ImportPath + Path.DirectorySeparatorChar;
    }
    
    public string GetPathImportErrors()
    {
        return _applicationRootPath + ApplicationConfiguration.ImportErrorsPath + Path.DirectorySeparatorChar;
    }

    public string GetPathIsbn()
    {
        return _applicationRootPath + ApplicationConfiguration.IsbnPath + Path.DirectorySeparatorChar;
    }

    public string GetPathCovers()
    {
        return _applicationRootPath + ApplicationConfiguration.CoversPath + Path.DirectorySeparatorChar;
    }
    
    public string GetPathThumbs()
    {
        return _applicationRootPath + ApplicationConfiguration.ThumbsPath + Path.DirectorySeparatorChar;
    }
    
    public void CreateApplicationDirectories()
    {
        Directory.CreateDirectory(_applicationRootPath + ApplicationConfiguration.CoversPath);
        Directory.CreateDirectory(_applicationRootPath + ApplicationConfiguration.IsbnPath);
        Directory.CreateDirectory(_applicationRootPath + ApplicationConfiguration.ThumbsPath);
        
        Directory.CreateDirectory(_applicationRootPath + ApplicationConfiguration.LibsPath);
        
        Directory.CreateDirectory(_applicationRootPath + ApplicationConfiguration.ImportPath);
        Directory.CreateDirectory(_applicationRootPath + ApplicationConfiguration.ImportErrorsPath);
    }

    public string[] GetAuthorizedExtension()
    {
        return ApplicationConfiguration.AuthorizedExtensions;
    }
}