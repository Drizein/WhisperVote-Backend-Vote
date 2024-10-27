using Application.Interfaces;
using Domain.Entities;

namespace Infrastructure.Persistence.Repositories;

public class VoteRepository(CDbContext context) : _BaseRepository<Vote>(context), IVoteRepository
{
}