using MyComicsManagerApi.Models;
using MyComicsManagerApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace MyComicsManagerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComicsController : ControllerBase
    {
        private readonly ComicService _comicService;

        private readonly ILogger<ComicsController> _logger;

        public ComicsController(ILogger<ComicsController> logger, ComicService comicService)
        {
            _comicService = comicService;
            _logger = logger;
        }

        [HttpGet]
        public ActionResult<List<Comic>> Get() =>
            _comicService.Get();

        [HttpGet("{id:length(24)}", Name = "GetComic")]
        public ActionResult<Comic> Get(string id)
        {
            var comic = _comicService.Get(id);

            if (comic == null)
            {
                return NotFound();
            }

            return comic;
        }

        [HttpPost]
        public ActionResult<Comic> Create(Comic comic)
        {   
            _comicService.Create(comic);

            return CreatedAtRoute("GetComic", new { id = comic.Id.ToString() }, comic);
        }

        [HttpPut("{id:length(24)}")]
        public IActionResult Update(string id, Comic comicIn)
        {
            var comic = _comicService.Get(id);

            if (comic == null)
            {
                return NotFound();
            }

            _comicService.Update(id, comicIn);

            return NoContent();
        }

        [HttpDelete("{id:length(24)}")]
        public IActionResult Delete(string id)
        {
            var comic = _comicService.Get(id);

            if (comic == null)
            {
                return NotFound();
            }

            _comicService.Remove(comic.Id);

            return NoContent();
        }
    }
}