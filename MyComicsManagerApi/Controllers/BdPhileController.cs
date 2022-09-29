using MyComicsManagerApi.Services;
using Microsoft.AspNetCore.Mvc;
using MyComicsManager.Model.Shared.Models;

namespace MyComicsManagerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BdPhileController : ControllerBase
    {
        private readonly ComicService _comicService;
        
        public BdPhileController(ComicService comicService)
        {
            _comicService = comicService;
        }
        
        [HttpGet("{isbn:length(13)}", Name = "GetComicInfo")]
        public ActionResult<Comic> Get(string isbn)
        {
            var comic = new Comic
            {
                Isbn = isbn
            };
            
            comic = _comicService.SearchComicInfo(comic,false);

            if (comic == null)
            {
                return NotFound();
            }

            return comic;
        }
        
    }
}