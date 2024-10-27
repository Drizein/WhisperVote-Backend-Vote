using Application.Interfaces;
using Domain.Entities;

namespace Infrastructure.Persistence.Repositories;

public class TagRepository(CDbContext context) : _BaseRepository<Tag>(context), ITagRepository
{
}