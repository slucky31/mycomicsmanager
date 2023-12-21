using Domain.Dto;
using Microsoft.EntityFrameworkCore;

namespace Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<LibraryDto> Libraries { get; set; }
}
