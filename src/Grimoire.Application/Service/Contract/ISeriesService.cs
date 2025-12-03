namespace Grimoire.Application.Service.Contract;

using Domain.Entity.Book;
using Dto.Book;

public interface ISeriesService : ICrudService<SeriesModel, CreateSeriesRequestDto, UpdateSeriesRequestDto>;
