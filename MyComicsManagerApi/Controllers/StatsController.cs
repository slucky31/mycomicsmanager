using MyComicsManagerApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace MyComicsManagerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatsController : ControllerBase
    {
        private readonly StatisticService _statisticService;
        
        public StatsController(StatisticService statisticService)
        {
            _statisticService = statisticService;
        }
        
        [HttpGet("comics")]
        public ActionResult<long> GetNbComics()
        {
            return _statisticService.CountComics();
        }
        
        [HttpGet("comicsWithoutSeries")]
        public ActionResult<long> GetNbComicsWithoutSeries()
        {
            return _statisticService.CountComicsWithoutSerie();
        }
        
        [HttpGet("comicsWithoutIsbn")]
        public ActionResult<long> GetNbComicsWithoutIsbn()
        {
            return _statisticService.CountComicsWithoutIsbn();
        }
        
        [HttpGet("comicsRead")]
        public ActionResult<long> GetNbComicsRead()
        {
            return _statisticService.CountComicsRead();
        }
        
        [HttpGet("comicsUnRead")]
        public ActionResult<long> GetNbComicsUnRead()
        {
            return _statisticService.CountComicsUnRead();
        }
        
        [HttpGet("comicsUnWebpFormated")]
        public ActionResult<long> GetNbComicsUnWebpFormated()
        {
            return _statisticService.CountComicsUnWebpFormated();
        }
        
        [HttpGet("comicsImportedWithErrors")]
        public ActionResult<long> GetNbComicsImportedWithErrors()
        {
            return _statisticService.CountComicsImportedWithErrors();
        }
        
        [HttpGet("series")]
        public ActionResult<long> GetNbSeries()
        {
            return _statisticService.CountSeries();
        }
    }
}