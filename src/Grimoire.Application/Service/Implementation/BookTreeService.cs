namespace Grimoire.Application.Service.Implementation;

using Contract;
using Domain.Common;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Domain.Exception;
using Dto.Book;
using Dto.Book.Tree;
using Dto.Common;
using Mapper;

public sealed class BookTreeService(
	IBookTreeRepository treeRepository,
	ISeriesRepository seriesRepository,
	IVolumeRepository volumeRepository,
	IChapterRepository chapterRepository,
	IBookMapper mapper) : CrudServiceBase<BookNodeModel>, IBookTreeService {
	private const string DefaultShelfId = "bookshelf:default";
	private const string DefaultShelfTitle = "Book Shelf";

	public async Task<BookTreeDto> GetTree(Guid seriesId, bool includeContent = false, CancellationToken cancellationToken = default) {
		var nodes = await treeRepository.FindSeriesTree(seriesId, cancellationToken);
		var seriesNode = nodes.FirstOrDefault(n => n.Id == seriesId && n.Type == BookNodeType.Series)
			?? throw new EntityNotFoundException($"Series with id {seriesId} not found");

		var root = new BookTreeNodeDto {
			Id = DefaultShelfId,
			Type = BookTreeNodeType.BookShelf,
			Title = DefaultShelfTitle,
			Children = [BuildDtoNode(seriesNode, nodes)]
		};

		return new BookTreeDto(root);
	}

	public async Task<SeriesModel?> FindSeries(Guid seriesId, CancellationToken cancellationToken = default) {
		var series = await seriesRepository.FindOne(seriesId, cancellationToken);
		var node = await treeRepository.FindOne(seriesId, cancellationToken);
		if (series is null || node is null) {
			return series;
		}

		series.Title = node.Title;
		return series;
	}

	public async Task<SeriesModel> CreateSeries(CreateSeriesRequestDto dto, CancellationToken cancellationToken = default) {
		var series = await seriesRepository.Create(mapper.CreateSeries(dto), cancellationToken);
		await treeRepository.Create(new BookNodeModel {
			Id = series.Id,
			Type = BookNodeType.Series,
			Title = series.Title,
			Order = 0,
			ParentId = null
		}, cancellationToken);

		return series;
	}

	public async Task<(SeriesModel Series, bool Created)> GetOrCreateSeries(CreateSeriesRequestDto dto, CancellationToken cancellationToken = default) {
		var existing = await seriesRepository.FindOneByTitle(dto.Title, cancellationToken);
		if (existing is not null) {
			await EnsureNode(existing.Id, BookNodeType.Series, null, existing.Title, 0, cancellationToken);
			return (existing, false);
		}

		return (await CreateSeries(dto, cancellationToken), true);
	}

	public async Task<SeriesModel> UpdateSeries(Guid seriesId, UpdateSeriesRequestDto dto, CancellationToken cancellationToken = default) {
		var series = await seriesRepository.FindOne(seriesId, cancellationToken) ??
			throw new EntityNotFoundException($"Series with id {seriesId} not found");
		var node = await RequireNode(seriesId, BookNodeType.Series, cancellationToken);

		var currentTitle = series.Title;
		mapper.UpdateSeries(dto, series);
		if (dto.Title is not null) {
			node.Title = dto.Title;
			series.Title = dto.Title;
		}
		else {
			series.Title = currentTitle;
		}

		await treeRepository.Update(node, cancellationToken);
		return await seriesRepository.Update(series, cancellationToken);
	}

	public async Task<BookNodeModel> CreateNode(Guid id, BookNodeType type, Guid? parentId, string title, float order, CancellationToken cancellationToken = default) {
		await ValidateParent(type, parentId, cancellationToken);
		var existing = await treeRepository.FindOne(id, cancellationToken);
		if (existing is not null) {
			return await UpdateNode(id, title, order, cancellationToken);
		}

		var duplicate = await treeRepository.FindChildByOrder(parentId, order, cancellationToken);
		if (duplicate is not null && duplicate.Id != id) {
			throw new InvalidOperationException($"A {type} node already exists at order {order}");
		}

		return await treeRepository.Create(new BookNodeModel {
			Id = id,
			Type = type,
			ParentId = parentId,
			Title = title,
			Order = order
		}, cancellationToken);
	}

	public async Task<BookNodeModel> UpdateNode(Guid id, string? title, float? order, CancellationToken cancellationToken = default) {
		var node = await treeRepository.FindOneTracked(id, cancellationToken) ??
			throw new EntityNotFoundException($"Book node with id {id} not found");

		if (title is not null) {
			node.Title = title;
		}

		if (order is not null) {
			var duplicate = await treeRepository.FindChildByOrder(node.ParentId, order.Value, cancellationToken);
			if (duplicate is not null && duplicate.Id != id) {
				throw new InvalidOperationException($"A {node.Type} node already exists at order {order}");
			}

			node.Order = order.Value;
		}

		return await treeRepository.Update(node, cancellationToken);
	}

	public async Task<VolumeModel> CreateVolume(CreateVolumeRequestDto dto, CancellationToken cancellationToken = default) {
		var seriesId = PrefixedId.ToGuid(dto.SeriesId, EntityPrefix.Series);
		_ = await RequireNode(seriesId, BookNodeType.Series, cancellationToken);

		var existingNode = await treeRepository.FindChildByOrder(seriesId, dto.Order, cancellationToken);
		if (existingNode is not null) {
			return await UpdateVolume(existingNode.Id, new UpdateVolumeRequestDto(dto.Order, dto.Title, dto.Metadata), cancellationToken);
		}

		var volume = mapper.CreateVolume(dto, seriesId);
		var created = await volumeRepository.Create(volume, cancellationToken);
		await treeRepository.Create(new BookNodeModel {
			Id = created.Id,
			Type = BookNodeType.Volume,
			ParentId = seriesId,
			Order = created.Order,
			Title = created.Title
		}, cancellationToken);

		return created;
	}

	public async Task<VolumeModel> UpdateVolume(Guid volumeId, UpdateVolumeRequestDto dto, CancellationToken cancellationToken = default) {
		var volume = await volumeRepository.FindOne(volumeId, cancellationToken) ??
			throw new EntityNotFoundException($"Volume with id {volumeId} not found");
		var node = await RequireNode(volumeId, BookNodeType.Volume, cancellationToken);

		var currentTitle = volume.Title;
		var currentOrder = volume.Order;
		mapper.UpdateVolume(dto, volume);
		if (dto.Title is not null) {
			node.Title = dto.Title;
			volume.Title = dto.Title;
		}
		else {
			volume.Title = currentTitle;
		}

		if (dto.Order is not null) {
			node.Order = dto.Order.Value;
			volume.Order = dto.Order.Value;
		}
		else {
			volume.Order = currentOrder;
		}

		await treeRepository.Update(node, cancellationToken);
		return await volumeRepository.Update(volume, cancellationToken);
	}

	public async Task<IEnumerable<VolumeModel>> FindVolumes(Guid seriesId, CancellationToken cancellationToken = default) {
		_ = await RequireNode(seriesId, BookNodeType.Series, cancellationToken);
		var children = (await treeRepository.FindChildren(seriesId, cancellationToken)).ToList();
		return await LoadVolumes(children, cancellationToken);
	}

	public async Task<PagedResult<VolumeModel>> FindVolumes(Guid seriesId, PaginationRequest pagination, CancellationToken cancellationToken = default) {
		_ = await RequireNode(seriesId, BookNodeType.Series, cancellationToken);
		var children = (await treeRepository.FindChildren(seriesId, pagination.PageIndex, pagination.PageSize, cancellationToken)).ToList();
		var total = await treeRepository.CountChildren(seriesId, cancellationToken);
		return new PagedResult<VolumeModel>(await LoadVolumes(children, cancellationToken), total, pagination.PageIndex, pagination.PageSize);
	}

	public async Task<IEnumerable<ChapterModel>> FindChapters(Guid volumeId, CancellationToken cancellationToken = default) {
		_ = await RequireNode(volumeId, BookNodeType.Volume, cancellationToken);
		var children = (await treeRepository.FindChildren(volumeId, cancellationToken)).ToList();
		return await LoadChapters(children, cancellationToken);
	}

	public async Task<PagedResult<ChapterModel>> FindChapters(Guid volumeId, PaginationRequest pagination, CancellationToken cancellationToken = default) {
		_ = await RequireNode(volumeId, BookNodeType.Volume, cancellationToken);
		var children = (await treeRepository.FindChildren(volumeId, pagination.PageIndex, pagination.PageSize, cancellationToken)).ToList();
		var total = await treeRepository.CountChildren(volumeId, cancellationToken);
		return new PagedResult<ChapterModel>(await LoadChapters(children, cancellationToken), total, pagination.PageIndex, pagination.PageSize);
	}

	public async Task MoveNode(Guid nodeId, Guid? newParentId, float newOrder, CancellationToken cancellationToken = default) {
		var node = await treeRepository.FindOneTracked(nodeId, cancellationToken) ??
			throw new EntityNotFoundException($"Book node with id {nodeId} not found");
		await ValidateParent(node.Type, newParentId, cancellationToken);
		var duplicate = await treeRepository.FindChildByOrder(newParentId, newOrder, cancellationToken);
		if (duplicate is not null && duplicate.Id != nodeId) {
			throw new InvalidOperationException($"A {node.Type} node already exists at order {newOrder}");
		}

		node.ParentId = newParentId;
		node.Order = newOrder;
		await treeRepository.Update(node, cancellationToken);

		if (node.Type == BookNodeType.Volume) {
			var volume = await volumeRepository.FindOne(node.Id, cancellationToken);
			if (volume is not null && newParentId is not null) {
				volume.SeriesId = newParentId.Value;
				volume.Order = newOrder;
				await volumeRepository.Update(volume, cancellationToken);
			}
		}
		else if (node.Type == BookNodeType.Chapter) {
			var chapter = await chapterRepository.FindOne(node.Id, cancellationToken);
			if (chapter is not null && newParentId is not null) {
				chapter.VolumeId = newParentId.Value;
				chapter.Order = newOrder;
				await chapterRepository.Update(chapter, cancellationToken);
			}
		}
	}

	public async Task<int> DeleteSubtree(Guid nodeId, CancellationToken cancellationToken = default) {
		var nodes = await treeRepository.FindSubtree(nodeId, cancellationToken);
		foreach (var node in nodes.Where(n => n.Type == BookNodeType.Chapter)) {
			await chapterRepository.Delete(node.Id, cancellationToken);
		}

		foreach (var node in nodes.Where(n => n.Type == BookNodeType.Volume)) {
			await volumeRepository.Delete(node.Id, cancellationToken);
		}

		foreach (var node in nodes.Where(n => n.Type == BookNodeType.Series)) {
			await seriesRepository.Delete(node.Id, cancellationToken);
		}

		await treeRepository.DeleteMany(nodes.Select(n => n.Id), cancellationToken);
		return nodes.Count;
	}

	private static BookTreeNodeDto BuildDtoNode(BookNodeModel node, IReadOnlyList<BookNodeModel> nodes) =>
		new() {
			Id = ToPrefixedId(node),
			Type = ToDtoType(node.Type),
			Title = node.Title,
			Order = node.Type == BookNodeType.Series ? null : node.Order,
			ParentId = node.ParentId is null ? DefaultShelfId : ToPrefixedId(nodes.First(n => n.Id == node.ParentId)),
			Children = nodes
				.Where(n => n.ParentId == node.Id)
				.OrderBy(n => n.Order)
				.Select(child => BuildDtoNode(child, nodes))
				.ToList()
		};

	private async Task<BookNodeModel> RequireNode(Guid id, BookNodeType type, CancellationToken cancellationToken) {
		var node = await treeRepository.FindOne(id, cancellationToken) ??
			throw new EntityNotFoundException($"Book node with id {id} not found");
		if (node.Type != type) {
			throw new InvalidOperationException($"Book node {id} must be {type}, got {node.Type}");
		}

		return node;
	}

	private async Task EnsureNode(Guid id, BookNodeType type, Guid? parentId, string title, float order, CancellationToken cancellationToken) {
		if (await treeRepository.FindOne(id, cancellationToken) is not null) {
			return;
		}

		await treeRepository.Create(new BookNodeModel {
			Id = id,
			Type = type,
			ParentId = parentId,
			Title = title,
			Order = order
		}, cancellationToken);
	}

	private async Task ValidateParent(BookNodeType type, Guid? parentId, CancellationToken cancellationToken) {
		if (type == BookNodeType.Series) {
			if (parentId is not null) {
				throw new InvalidOperationException("Series nodes must be root-level nodes");
			}
			return;
		}

		if (parentId is null) {
			throw new InvalidOperationException($"{type} nodes must have a parent");
		}

		var parent = await treeRepository.FindOne(parentId.Value, cancellationToken) ??
			throw new EntityNotFoundException($"Parent node with id {parentId} not found");
		var expected = type == BookNodeType.Volume ? BookNodeType.Series : BookNodeType.Volume;
		if (parent.Type != expected) {
			throw new InvalidOperationException($"{type} nodes must be parented by {expected} nodes");
		}
	}

	private async Task<List<VolumeModel>> LoadVolumes(IReadOnlyList<BookNodeModel> nodes, CancellationToken cancellationToken) {
		var volumes = new List<VolumeModel>(nodes.Count);
		foreach (var node in nodes.Where(n => n.Type == BookNodeType.Volume)) {
			var volume = await volumeRepository.FindOne(node.Id, cancellationToken);
			if (volume is null) {
				continue;
			}

			volume.Title = node.Title;
			volume.Order = node.Order;
			volumes.Add(volume);
		}

		return volumes;
	}

	private async Task<List<ChapterModel>> LoadChapters(IReadOnlyList<BookNodeModel> nodes, CancellationToken cancellationToken) {
		var chapters = new List<ChapterModel>(nodes.Count);
		foreach (var node in nodes.Where(n => n.Type == BookNodeType.Chapter)) {
			var chapter = await chapterRepository.FindOne(node.Id, cancellationToken);
			if (chapter is null) {
				continue;
			}

			chapter.Title = node.Title;
			chapter.Order = node.Order;
			chapters.Add(chapter);
		}

		return chapters;
	}

	private static BookTreeNodeType ToDtoType(BookNodeType type) =>
		type switch {
			BookNodeType.Series => BookTreeNodeType.Series,
			BookNodeType.Volume => BookTreeNodeType.Volume,
			BookNodeType.Chapter => BookTreeNodeType.Chapter,
			_ => BookTreeNodeType.BookShelf
		};

	private static string ToPrefixedId(BookNodeModel node) =>
		node.Type switch {
			BookNodeType.Series => PrefixedId.ToString(EntityPrefix.Series, node.Id),
			BookNodeType.Volume => PrefixedId.ToString(EntityPrefix.Volume, node.Id),
			BookNodeType.Chapter => PrefixedId.ToString(EntityPrefix.Chapter, node.Id),
			_ => node.Id.ToString()
		};

}
