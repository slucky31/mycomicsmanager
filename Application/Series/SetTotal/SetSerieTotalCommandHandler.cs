using Application.Abstractions.Messaging;
using Application.Interfaces;
using Domain.Libraries;
using Domain.Primitives;
using Domain.Series;

namespace Application.Series.SetTotal;

public sealed class SetSerieTotalCommandHandler(
    ISeriesInfoRepository seriesInfoRepository,
    IRepository<Library, Guid> libraryRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<SetSerieTotalCommand>
{
    public async Task<Result> Handle(SetSerieTotalCommand command, CancellationToken cancellationToken)
    {
        if (command is null || command.UserId == Guid.Empty || command.LibraryId == Guid.Empty
            || string.IsNullOrWhiteSpace(command.Name) || command.Name.Trim().Length > SeriesConstants.MaxNameLength)
        {
            return SeriesError.BadRequest;
        }

        if (command.TotalVolumes is < 1 or > SeriesConstants.MaxTotalVolumes)
        {
            return SeriesError.InvalidTotalVolumes;
        }

        var library = await libraryRepository.GetByIdAsync(command.LibraryId);
        if (library is null || library.UserId != command.UserId)
        {
            return LibrariesError.NotFound;
        }

        var existing = await seriesInfoRepository.GetAsync(command.LibraryId, SerieNames.Normalize(command.Name), cancellationToken);
        var changed = existing is null ? Add(command) : existing.Update(command.TotalVolumes, command.IsFinished);
        if (changed.IsFailure)
        {
            return changed;
        }

        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        return saved.IsFailure ? saved.Error! : Result.Success();
    }

    private Result Add(SetSerieTotalCommand command)
    {
        var created = SeriesInfo.Create(command.LibraryId, command.Name, command.TotalVolumes, command.IsFinished);
        if (created.IsFailure)
        {
            return created.Error!;
        }

        seriesInfoRepository.Add(created.Value!);
        return Result.Success();
    }
}
