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
	IUnitOfWork unitOfWork,
	IBookMapper mapper) : CrudServiceBase<BookNodeModel>, IBookTreeService {
	private const string DefaultShelfId = "bookshelf:default";
	private const string DefaultShelfTitle = "Book Shelf";

	private async Task ExecuteInTransaction(Func<Task> action, CancellationToken cancellationToken) {
		await unitOfWork.BeginTransactionAsync(cancellationToken);
		try {
			await action();
			await unitOfWork.CommitTransactionAsync(cancellationToken);
		}
		catch {
			await unitOfWork.RollbackTransactionAsync(cancellationToken);
			throw;
		}
	}

	private async Task<T> ExecuteInTransaction<T>(Func<Task<T>> action, CancellationToken cancellationToken) {
		await unitOfWork.BeginTransactionAsync(cancellationToken);
		try {
			var result = await action();
			await unitOfWork.CommitTransactionAsync(cancellationToken);
			return result;
		}
		catch {
			await unitOfWork.RollbackTransactionAsync(cancellationToken);
			throw;
		}
	}

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
		return await ExecuteInTransaction(async () => {
			var series = await seriesRepository.Create(mapper.CreateSeries(dto), cancellationToken);
			await treeRepository.Create(new BookNodeModel {
				Id = series.Id,
				Type = BookNodeType.Series,
				Title = series.Title,
				Order = 0,
				ParentId = null,
				Path = BookNodeModel.CalculatePath(series.Id, null)
			}, cancellationToken);

			return series;
		}, cancellationToken);
	}

	public async Task<(SeriesModel Series, bool Created)> GetOrCreateSeries(CreateSeriesRequestDto dto, CancellationToken cancellationToken = default) {
		var normalizedTitle = dto.Title.Trim();
		var existing = await seriesRepository.FindOneByTitle(normalizedTitle, cancellationToken);
		if (existing is not null) {
			await EnsureNode(existing.Id, BookNodeType.Series, null, existing.Title, 0, cancellationToken);
			return (existing, false);
		}

		var normalizedDto = dto with { Title = normalizedTitle };
		return (await CreateSeries(normalizedDto, cancellationToken), true);
	}

	public async Task<SeriesModel> UpdateSeries(Guid seriesId, UpdateSeriesRequestDto dto, CancellationToken cancellationToken = default) {
		var series = await seriesRepository.FindOneTracked(seriesId, cancellationToken) ??
			throw new EntityNotFoundException($"Series with id {seriesId} not found");
		var node = await RequireNode(seriesId, BookNodeType.Series, cancellationToken);

		mapper.UpdateSeries(dto, series);
		node.Title = series.Title;

		return await ExecuteInTransaction(async () => {
			await treeRepository.Update(node, cancellationToken);
			return await seriesRepository.Update(series, cancellationToken);
		}, cancellationToken);
	}

	public async Task<BookNodeModel> CreateNode(Guid id, BookNodeType type, Guid? parentId, string title, double order, CancellationToken cancellationToken = default) {
		await ValidateParent(type, parentId, cancellationToken);
		var existing = await treeRepository.FindOne(id, cancellationToken);
		if (existing is not null) {
			return await UpdateNode(id, title, order, cancellationToken);
		}

		var duplicate = await treeRepository.FindChildByOrder(parentId, order, cancellationToken);
		if (duplicate is not null && duplicate.Id != id) {
			throw new InvalidOperationException($"A {type} node already exists at order {order}");
		}

		var parentPath = parentId is null
			? null
			: (await treeRepository.FindOne(parentId.Value, cancellationToken))?.Path;
		var path = BookNodeModel.CalculatePath(id, parentPath);

		return await treeRepository.Create(new BookNodeModel {
			Id = id,
			Type = type,
			ParentId = parentId,
			Title = title,
			Order = order,
			Path = path
		}, cancellationToken);
	}

	public async Task<BookNodeModel> UpdateNode(Guid id, string? title, double? order, CancellationToken cancellationToken = default) {
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
		var parentNode = await RequireNode(seriesId, BookNodeType.Series, cancellationToken);

		var existingNode = await treeRepository.FindChildByOrder(seriesId, dto.Order, cancellationToken);
		if (existingNode is not null) {
			return await UpdateVolume(existingNode.Id, new UpdateVolumeRequestDto(dto.Order, dto.Title, dto.Metadata), cancellationToken);
		}

		return await ExecuteInTransaction(async () => {
			var volume = mapper.CreateVolume(dto, seriesId);
			var created = await volumeRepository.Create(volume, cancellationToken);
			await treeRepository.Create(new BookNodeModel {
				Id = created.Id,
				Type = BookNodeType.Volume,
				ParentId = seriesId,
				Order = created.Order,
				Title = created.Title,
				Path = BookNodeModel.CalculatePath(created.Id, parentNode.Path)
			}, cancellationToken);

			return created;
		}, cancellationToken);
	}

	public async Task<VolumeModel> UpdateVolume(Guid volumeId, UpdateVolumeRequestDto dto, CancellationToken cancellationToken = default) {
		var volume = await volumeRepository.FindOneTracked(volumeId, cancellationToken) ??
			throw new EntityNotFoundException($"Volume with id {volumeId} not found");
		var node = await RequireNode(volumeId, BookNodeType.Volume, cancellationToken);

		mapper.UpdateVolume(dto, volume);
		node.Title = volume.Title;
		node.Order = volume.Order;

		var result = await ExecuteInTransaction(async () => {
			await treeRepository.Update(node, cancellationToken);
			return await volumeRepository.Update(volume, cancellationToken);
		}, cancellationToken);

		if (dto.SeriesId is not null) {
			var newParentId = PrefixedId.ToGuid(dto.SeriesId, EntityPrefix.Series);
			await MoveNode(volumeId, newParentId, dto.Order ?? volume.Order, cancellationToken);
			var refreshed = await volumeRepository.FindOneTracked(volumeId, cancellationToken);
			if (refreshed is not null) {
				result = refreshed;
			}
		}

		return result;
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

	public async Task MoveNode(Guid nodeId, Guid? newParentId, double newOrder, CancellationToken cancellationToken = default) {
		var node = await treeRepository.FindOneTracked(nodeId, cancellationToken) ??
			throw new EntityNotFoundException($"Book node with id {nodeId} not found");
		await ValidateParent(node.Type, newParentId, cancellationToken);
		var duplicate = await treeRepository.FindChildByOrder(newParentId, newOrder, cancellationToken);
		if (duplicate is not null && duplicate.Id != nodeId) {
			throw new InvalidOperationException($"A {node.Type} node already exists at order {newOrder}");
		}

		var oldPath = node.Path;
		var parent = newParentId is null ? null : await treeRepository.FindOne(newParentId.Value, cancellationToken);
		if (parent is not null && !string.IsNullOrEmpty(oldPath) && !string.IsNullOrEmpty(parent.Path)
			&& (parent.Path == oldPath || parent.Path.StartsWith(oldPath + "."))) {
			throw new InvalidOperationException("Cannot move a node into its own subtree.");
		}
		var newPath = BookNodeModel.CalculatePath(nodeId, parent?.Path);

		node.ParentId = newParentId;
		node.Order = newOrder;
		node.Path = newPath;

		await ExecuteInTransaction(async () => {
			await treeRepository.Update(node, cancellationToken);
			if (oldPath != newPath) {
				await treeRepository.UpdateSubtreePaths(nodeId, oldPath, newPath, cancellationToken);
			}

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
		}, cancellationToken);
	}

	public async Task<int> DeleteSubtree(Guid nodeId, CancellationToken cancellationToken = default) {
		var nodes = await treeRepository.FindSubtree(nodeId, cancellationToken);
		if (nodes.Count == 0) return 0;

		var ids = nodes.Select(n => n.Id).ToList();
		var chapterIds = nodes.Where(n => n.Type == BookNodeType.Chapter).Select(n => n.Id).ToList();
		var volumeIds = nodes.Where(n => n.Type == BookNodeType.Volume).Select(n => n.Id).ToList();
		var seriesIds = nodes.Where(n => n.Type == BookNodeType.Series).Select(n => n.Id).ToList();

		await ExecuteInTransaction(async () => {
			if (chapterIds.Count > 0) {
				await chapterRepository.DeleteMany(chapterIds, cancellationToken);
			}
			if (volumeIds.Count > 0) {
				await volumeRepository.DeleteMany(volumeIds, cancellationToken);
			}
			if (seriesIds.Count > 0) {
				await seriesRepository.DeleteMany(seriesIds, cancellationToken);
			}
			await treeRepository.DeleteMany(ids, cancellationToken);
		}, cancellationToken);

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

	private async Task EnsureNode(Guid id, BookNodeType type, Guid? parentId, string title, double order, CancellationToken cancellationToken) {
		if (await treeRepository.FindOne(id, cancellationToken) is not null) {
			return;
		}

		var parentPath = parentId is null
			? null
			: (await treeRepository.FindOne(parentId.Value, cancellationToken))?.Path;
		var path = BookNodeModel.CalculatePath(id, parentPath);

		await treeRepository.Create(new BookNodeModel {
			Id = id,
			Type = type,
			ParentId = parentId,
			Title = title,
			Order = order,
			Path = path
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

	private async Task<List<VolumeModel>> LoadVolumes(List<BookNodeModel> nodes, CancellationToken cancellationToken) {
		var volumeNodes = nodes.Where(n => n.Type == BookNodeType.Volume).ToList();
		if (volumeNodes.Count == 0) {
			return [];
		}

		var ids = volumeNodes.Select(n => n.Id).ToList();
		var volumes = (await volumeRepository.FindByIds(ids, cancellationToken)).ToDictionary(v => v.Id);

		var result = new List<VolumeModel>(volumeNodes.Count);
		foreach (var node in volumeNodes) {
			if (volumes.TryGetValue(node.Id, out var volume)) {
				volume.Title = node.Title;
				volume.Order = node.Order;
				result.Add(volume);
			}
		}

		return result;
	}

	private async Task<List<ChapterModel>> LoadChapters(List<BookNodeModel> nodes, CancellationToken cancellationToken) {
		var chapterNodes = nodes.Where(n => n.Type == BookNodeType.Chapter).ToList();
		if (chapterNodes.Count == 0) {
			return [];
		}

		var ids = chapterNodes.Select(n => n.Id).ToList();
		var chapters = (await chapterRepository.FindByIds(ids, cancellationToken)).ToDictionary(c => c.Id);

		var result = new List<ChapterModel>(chapterNodes.Count);
		foreach (var node in chapterNodes) {
			if (chapters.TryGetValue(node.Id, out var chapter)) {
				chapter.Title = node.Title;
				chapter.Order = node.Order;
				result.Add(chapter);
			}
		}

		return result;
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

	public async Task ReconcileNodesBulk(
		Guid seriesId,
		List<(Guid Id, BookNodeType Type, Guid? ParentId, string Title, double Order)> nodes,
		CancellationToken cancellationToken = default) {

		var existingNodes = (await treeRepository.FindSeriesTree(seriesId, cancellationToken))
			.ToDictionary(n => n.Id);

		var parentIds = new Dictionary<Guid, Guid?>();
		foreach (var n in nodes) {
			parentIds[n.Id] = n.ParentId;
		}
		foreach (var n in existingNodes.Values) {
			if (!parentIds.ContainsKey(n.Id)) {
				parentIds[n.Id] = n.ParentId;
			}
		}

		var visiting = new HashSet<Guid>();
		string GetPath(Guid id, Guid? pId) {
			if (pId is null || !visiting.Add(id)) {
				return BookNodeModel.CalculatePath(id, null);
			}
			parentIds.TryGetValue(pId.Value, out var grandparentsId);
			var parentPath = GetPath(pId.Value, grandparentsId);
			var path = BookNodeModel.CalculatePath(id, parentPath);
			visiting.Remove(id);
			return path;
		}

		var toCreate = new List<BookNodeModel>();
		var toUpdate = new List<BookNodeModel>();

		foreach (var item in nodes) {
			if (existingNodes.TryGetValue(item.Id, out var existing)) {
				existing.Title = item.Title;
				existing.Order = item.Order;
				existing.ParentId = item.ParentId;
				existing.Path = GetPath(item.Id, item.ParentId);
				toUpdate.Add(existing);
			}
			else {
				toCreate.Add(new BookNodeModel {
					Id = item.Id,
					Type = item.Type,
					ParentId = item.ParentId,
					Title = item.Title,
					Order = item.Order,
					Path = GetPath(item.Id, item.ParentId)
				});
			}
		}

		var inputIds = nodes.Select(n => n.Id).ToHashSet();
		var inputTypes = nodes.Select(n => n.Type).ToHashSet();
		var toDeleteIds = existingNodes.Values
			.Where(n => inputTypes.Contains(n.Type) && !inputIds.Contains(n.Id))
			.Select(n => n.Id)
			.ToList();

		await ExecuteInTransaction(async () => {
			if (toDeleteIds.Count > 0) {
				var nodesToDelete = toDeleteIds.Select(id => existingNodes[id]).ToList();
				var chapterIds = nodesToDelete.Where(n => n.Type == BookNodeType.Chapter).Select(n => n.Id).ToList();
				var volumeIds = nodesToDelete.Where(n => n.Type == BookNodeType.Volume).Select(n => n.Id).ToList();
				var seriesIds = nodesToDelete.Where(n => n.Type == BookNodeType.Series).Select(n => n.Id).ToList();

				if (chapterIds.Count > 0) {
					await chapterRepository.DeleteMany(chapterIds, cancellationToken);
				}
				if (volumeIds.Count > 0) {
					await volumeRepository.DeleteMany(volumeIds, cancellationToken);
				}
				if (seriesIds.Count > 0) {
					await seriesRepository.DeleteMany(seriesIds, cancellationToken);
				}
				await treeRepository.DeleteMany(toDeleteIds, cancellationToken);
			}

			if (toCreate.Count > 0) {
				await treeRepository.CreateBulk(toCreate, cancellationToken);
			}
			if (toUpdate.Count > 0) {
				await treeRepository.UpdateBulk(toUpdate, cancellationToken);
			}
		}, cancellationToken);
	}

}
