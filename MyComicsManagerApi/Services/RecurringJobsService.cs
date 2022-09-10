using Hangfire;
using Serilog;

namespace MyComicsManagerApi.Services;

public class RecurringJobsService
{
    public RecurringJobsService(ComicService comicService)
    {
        // https://www.freeformatter.com/cron-expression-generator-quartz.html
        // Remarque : Supprimer l'étoile des années car ne semble par gérer par HangFire
        // At second :00, at minute :00, every hour starting at 00am, of every day
        RecurringJob.AddOrUpdate("ConvertComicsToWebP", () => comicService.RecurringJobConvertComicsToWebP(), Cron.Hourly);
        
        
        // Lancement des tâches récurrentes
        // https://stackoverflow.com/questions/42077770/recurring-jobs-with-hangfire-and-asp-net-core
      
    }
}