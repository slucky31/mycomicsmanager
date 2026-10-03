using Application.Abstractions.Messaging;
using Application.ImportJobs;
using Application.ImportJobs.Create;
using Application.Interfaces;
using Application.Libraries;
using Application.Libraries.Create;
using Domain.ImportJobs;
using Domain.Libraries;

namespace Application.FeedImports.Download;

// Handing the file over to the import pipeline: target library, import directory, ImportJob.
public sealed record FeedImportDepositServices(
    ILibraryReadService LibraryReadService,
    ICommandHandler<CreateLibraryCommand, Library> CreateLibraryHandler,
    IImportDirectoryStorage ImportDirectoryStorage,
    ICommandHandler<CreateImportJobCommand, ImportJob> CreateImportJobHandler,
    IImportJobEnqueuer ImportJobEnqueuer);
