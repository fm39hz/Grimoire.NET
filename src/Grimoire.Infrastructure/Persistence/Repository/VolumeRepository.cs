namespace Grimoire.Infrastructure.Persistence.Repository;

using Database;
using Domain.Common.Repository;
using Domain.Entity.Book;

public sealed class VolumeRepository(ApplicationDbContext context)
    : CrudRepository<VolumeModel>(context), IVolumeRepository {
    
}
