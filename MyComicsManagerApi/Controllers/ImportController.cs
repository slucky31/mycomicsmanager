using MyComicsManagerApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using MyComicsManager.Model.Shared.Models;

namespace MyComicsManagerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImportController : ControllerBase
    {
        private readonly ImportService _importService;
        private readonly ILogger<ImportController> _logger;

        public ImportController(ILogger<ImportController> logger, ImportService importService)
        {
            _importService = importService;
            _logger = logger;
        }
        
        [HttpGet]
        public ActionResult<List<Comic>> Get() =>
            _importService.GetUploadedFiles();
        
    }
}