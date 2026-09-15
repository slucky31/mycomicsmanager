using Application.Interfaces;
using Domain.Libraries;

namespace Application.ImportJobs.Process;

public record ProcessImportJobRepositories(
    IImportJobRepository ImportJobs,
    IRepository<Library, Guid> Libraries,
    IBookRepository Books,
    IUnitOfWork UnitOfWork);
