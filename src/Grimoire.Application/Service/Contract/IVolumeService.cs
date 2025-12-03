namespace Grimoire.Application.Service.Contract;

using Domain.Entity.Book;
using Dto.Book;

public interface IVolumeService : ICrudService<VolumeModel, CreateVolumeRequestDto, UpdateVolumeRequestDto>;
