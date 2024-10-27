using Domain.Entities;
using Application.Interfaces;

namespace Infrastructure.Persistence.Repositories;

public class KeyPairRepository(CDbContext context) : _BaseRepository<KeyPair>(context), IKeyPairRepository;