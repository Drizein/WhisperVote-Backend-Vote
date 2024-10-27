using Application.Interfaces;
using Domain.Entities;

namespace Infrastructure.Persistence.Repositories;

public class OptionRepository(CDbContext context) : _BaseRepository<Option>(context), IOptionRepository
{
}