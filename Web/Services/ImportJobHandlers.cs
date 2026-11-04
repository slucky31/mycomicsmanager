using Application.Abstractions.Messaging;
using Application.ImportJobs.Abandon;
using Application.ImportJobs.Create;
using Application.ImportJobs.Delete;
using Application.ImportJobs.ForceFail;
using Application.ImportJobs.GetById;
using Application.ImportJobs.List;
using Application.ImportJobs.Retry;
using Application.Libraries.GetById;
using Domain.ImportJobs;
using Domain.Libraries;

namespace Web.Services;

public record ImportJobHandlers(
    IQueryHandler<ListImportJobsQuery, IReadOnlyList<ImportJob>> ListJobs,
    IQueryHandler<GetImportJobQuery, ImportJob> GetJob,
    ICommandHandler<CreateImportJobCommand, ImportJob> CreateJob,
    ICommandHandler<DeleteImportJobCommand> DeleteJob,
    ICommandHandler<ForceFailImportJobCommand> ForceFailJob,
    ICommandHandler<RetryImportJobCommand> RetryJob,
    ICommandHandler<AbandonImportJobCommand> AbandonJob,
    IQueryHandler<GetLibraryQuery, Library> GetLibrary);
