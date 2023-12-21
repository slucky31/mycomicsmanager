using Domain.Dto;
using Microsoft.EntityFrameworkCore;

namespace Persistence;

public interface IApplicationDbContext
{
    DbSet<LibraryDto> Libraries { get; set; }
}
