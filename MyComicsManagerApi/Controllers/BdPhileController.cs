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

        /// <summary>
        /// Initializes a new instance of the <see cref="BdPhileController"/> class.
        /// </summary>
        /// <param name="comicService">The comic service.</param>
        public BdPhileController(ComicService comicService)
        {
            _comicService = comicService;
        }

        /// <summary>
        /// Retrieves comic information based on the provided ISBN.
        /// </summary>
        /// <param name="isbn">The ISBN of the comic.</param>
        /// <returns>The comic information if found, or a "Not Found" response if the comic is not found.</returns>
        [HttpGet("{isbn:length(13)}", Name = "GetComicInfo")]
        public ActionResult<Comic> Get(string isbn)
        {
            var comic = new Comic
            {
                Isbn = isbn
            };

            comic = _comicService.SearchComicInfo(comic, false);

            if (comic == null)
            {
                return NotFound();
            }

            return comic;
        }
    }
}