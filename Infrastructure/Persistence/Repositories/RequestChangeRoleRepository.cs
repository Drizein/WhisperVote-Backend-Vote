using Application.Interfaces;
using Domain.Entities;

namespace Infrastructure.Persistence.Repositories;

public class RequestChangeRoleRepository(CDbContext context)
    : _BaseRepository<RequestChangeRole>(context), IRequestChangeRoleRepository
{
}