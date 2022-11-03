using MongoDB.Driver;
using System.Collections.Generic;
using MyComicsManager.Model.Shared.Models;
using MyComicsManagerApi.Settings;
using Serilog;
using MyComicsManagerApi.Utils;

namespace MyComicsManagerApi.Services
{
    public class StatisticService
    {
        private static ILogger Log => Serilog.Log.ForContext<ComicService>();
        
        private readonly IMongoCollection<Comic> _comics;

        public StatisticService(IDatabaseSettings settings)
        {
            Log.Here().Debug("settings = {Settings}", settings);
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);
            _comics = database.GetCollection<Comic>(settings.ComicsCollectionName);
        }
        
        private long CountComicsRequest(FilterDefinition<Comic> filter)
        {
            return _comics.CountDocuments(filter);
        }
        
        public long CountComics()
        {
            var filter = Builders<Comic>.Filter.Where(comic => comic.ImportStatus == ImportStatus.IMPORTED);
            return CountComicsRequest(filter);
        }
        
        public long CountComicsWithoutSerie()
        {
            var filter = Builders<Comic>.Filter.Where(comic => comic.ImportStatus == ImportStatus.IMPORTED && string.IsNullOrEmpty(comic.Serie));
            return CountComicsRequest(filter);
        }
        
        public long CountComicsWithoutIsbn()
        {
            var filter = Builders<Comic>.Filter.Where(comic => comic.ImportStatus == ImportStatus.IMPORTED && string.IsNullOrEmpty(comic.Isbn));
            return CountComicsRequest(filter);
        }
        
        public long CountComicsRead()
        {
            var filter = Builders<Comic>.Filter.Where(comic => comic.ImportStatus == ImportStatus.IMPORTED && comic.ComicReviews.Count > 0);
            return CountComicsRequest(filter);
        }
        
        public long CountComicsUnRead()
        {
            var filter = Builders<Comic>.Filter.Where(comic => comic.ImportStatus == ImportStatus.IMPORTED && (comic.ComicReviews == null || comic.ComicReviews.Count == 0) );
            return CountComicsRequest(filter);
        }
        
        public long CountComicsUnWebpFormated()
        {
            var filter = Builders<Comic>.Filter.Where(comic => comic.ImportStatus == ImportStatus.IMPORTED && comic.WebPFormated == false );
            return CountComicsRequest(filter);
        }
        
        public long CountComicsImportedWithErrors()
        {
            var filter = Builders<Comic>.Filter.Where(comic => comic.ImportStatus == ImportStatus.ERROR );
            return CountComicsRequest(filter);
        }
        
        public List<string> GetSeries()
        {
            var filter = Builders<Comic>.Filter.Ne(comic => comic.Serie, null);
            return _comics.Distinct(comic => comic.Serie, filter).ToList();
        }
        
        public long CountSeries()
        {
            var filter = Builders<Comic>.Filter.Ne(comic => comic.Serie, null);
            return _comics.Distinct(comic => comic.Serie, filter).ToList().Count;
        }

        

    }
}
