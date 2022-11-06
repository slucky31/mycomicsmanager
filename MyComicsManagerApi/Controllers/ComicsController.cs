using Microsoft.AspNetCore.Mvc;
using MyComicsManagerApi.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hangfire;
using MyComicsManager.Model.Shared.Models;

namespace MyComicsManagerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComicsController : ControllerBase
    {
        private readonly ComicService _comicService;
        // TODO : ne devrait pas être utilisé mais encapsulé dans le service ComicService
        private readonly ComicFileService _comicFileService;
        private readonly ImportService _importService;

        public ComicsController(ComicService comicService, ComicFileService comicFileService, ImportService importService)
        {
            _comicService = comicService;
            _comicFileService = comicFileService;
            _importService = importService;
        }

        [HttpGet]
        public ActionResult<List<Comic>> Get() =>
            _comicService.Get();
        
        [HttpGet("random/limit/{limit:int}")]
        public ActionResult<List<Comic>> GetRandomLimitBy(int limit) =>
            _comicService.GetRandomLimitBy(limit);
        
        [HttpGet("withoutIsbn/limit/{limit:int}")]
        public ActionResult<List<Comic>> ListComicsWithoutIsbnLimitBy(int limit) =>
            _comicService.GetWithoutIsbnLimitBy(limit);
        
        [HttpGet("orderBy/lastAdded/limit/{limit:int}")]
        public ActionResult<List<Comic>> ListComicsOrderByLastAddedLimitBy(int limit) =>
            _comicService.GetOrderByLastAddedLimitBy(limit);
        
        [HttpGet("orderBy/serie/limit/{limit:int}")]
        public ActionResult<List<Comic>> ListComicsOrderBySerieLimitBy(int limit) =>
            _comicService.GetOrderBySerieAndTome(limit);
        
        [HttpGet("find/{item}/limit/{limit:int}")]
        public ActionResult<List<Comic>> FindBySerieOrTitle(string item, int limit) =>
            _comicService.Find(item, limit);
        
        [HttpGet("find/serie/{item}/limit/{limit:int}")]
        public ActionResult<List<Comic>> FindBySerie(string item, int limit) =>
            _comicService.FindBySerie(item, limit);
        
        [HttpGet("importing")]
        public ActionResult<List<Comic>> GetImportingComics() =>
            _comicService.GetImportingComics();

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
            var createdComic = _comicService.Create(comic);

            if (createdComic == null)
            {
                return NotFound();
            }

            BackgroundJob.Enqueue(() => _importService.Import(comic));
            return CreatedAtRoute("GetComic", new { id = comic.Id }, comic);
        }

        [HttpPut("{id:length(24)}")]
        public IActionResult Update(string id, Comic comicIn)
        {
            var comic = _comicService.Get(id);

            if (comic == null)
            {
                return NotFound();
            }

            _comicService.Update(id, comicIn, true);

            return NoContent();
        }

        [HttpGet("searchcomicinfo/{id:length(24)}")]
        public ActionResult<Comic> SearchComicInfo(string id)
        {
            var comic = _comicService.Get(id);
            if (comic == null)
            {
                return NotFound();
            }

            comic = _comicService.SearchComicInfo(comic, true);
            if (comic == null)
            {
                return NotFound();
            }

            return _comicService.Get(id);
        }
        
        [HttpDelete("{id:length(24)}")]
        public IActionResult Delete(string id)
        {
            var comic = _comicService.Get(id);
            if (comic == null)
            {
                return NotFound();
            }

            _comicService.Remove(comic);

            return NoContent();
        }
        
        [HttpDelete("reset-import-status/{id:length(24)}")]
        public IActionResult ReImport(string id)
        {
            var comic = _comicService.Get(id);
            if (comic == null)
            {
                return NotFound();
            }
            
            BackgroundJob.Enqueue(() => _importService.ResetImportStatus(comic));

            return NoContent();
        }

        [HttpGet("extractcover/{id:length(24)}")]
        public ActionResult<Comic> SetAndExtractCoverImage(string id)
        {
            var comic = _comicService.Get(id);

            if (comic == null)
            {
                return NotFound();
            }

            _comicFileService.SetAndExtractCoverImage(comic);
            _comicService.Update(id, comic, true);

            return _comicService.Get(id);
        }

        [HttpGet("extractisbn/{id:length(24)}&{indexImage:int}")]
        public ActionResult<List<string>> ExtractIsbn(string id, int indexImage)
        {
            var comic = _comicService.Get(id);

            if (comic == null)
            {
                return NotFound();
            }

            // TODO : check index image < nb images

            // Evitement de l'utilisation de await / async
            // https://visualstudiomagazine.com/Blogs/Tool-Tracker/2019/10/calling-methods-async.aspx
            var task = _comicFileService.ExtractIsbnFromCbz(comic, indexImage);
            var isbnList = task.Result;

            return isbnList;
        }
        
        [HttpGet("extractTitle/{id:length(24)}")]
        public ActionResult<string> ExtractTitle(string id)
        {
            var comic = _comicService.Get(id);

            if (comic == null)
            {
                return NotFound();
            }
            
            var task = _comicFileService.ExtractTitleFromCbz(comic);
            return task.Result;
        }

        [HttpGet("extractimages/{id:length(24)}&{nbImagesToExtract:int}&{first:bool}")]
        public ActionResult<List<string>> ExtractImages(string id, int nbImagesToExtract, bool first)
        {
            var comic = _comicService.Get(id);

            if (comic == null)
            {
                return NotFound();
            }

            // TODO : check index image < nb images
            return first ? _comicFileService.ExtractFirstImages(comic, nbImagesToExtract) : _comicFileService.ExtractLastImages(comic, nbImagesToExtract);
            
        }
        
        [HttpGet("series")]
        public ActionResult<List<string>> Series()
        {
            return _comicService.GetSeries();
        }

        [HttpGet("deleteDotFiles")]
        public ActionResult<Comic> DeleteDotFiles()
        {
            BackgroundJob.Enqueue(() => _comicService.DeleteDotFiles());
            
            // TODO : gérer le retour 202 et la méthode de vérification
            return Ok();
        }

        

        [HttpDelete("deleteallcomicsfromlib/{id:length(24)}")]
        public IActionResult DeleteAllComicsFromLib(string id)
        {
            _comicService.RemoveAllComicsFromLibrary(id);

            return NoContent();
        }
        
        [HttpGet("findbypageorderbyserie")]
        public ActionResult<PaginationComics> FindByPage(string searchItem, int pageId, int pageSize)
        {
            return _comicService.FindByPageOrderBySerie(searchItem, pageId,pageSize).Result;
        }
    }
}