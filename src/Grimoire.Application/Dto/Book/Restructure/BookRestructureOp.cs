namespace Grimoire.Application.Dto.Book.Restructure;

using System.Text.Json.Serialization;
using Segment;

/// <summary>
///     Base for a single atomic restructuring operation. Serialized polymorphically via <c>$type</c>
///     (mirroring <c>SegmentDto</c>). The server interprets each op, validates it, and runs it
///     through a trusted domain service — the caller (AI/user) declares intent, never structure.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(MoveNodeOp), "moveNode")]
[JsonDerivedType(typeof(DeleteNodeOp), "deleteNode")]
[JsonDerivedType(typeof(MergeChaptersOp), "mergeChapters")]
[JsonDerivedType(typeof(SplitChapterOp), "splitChapter")]
[JsonDerivedType(typeof(MergeVolumesOp), "mergeVolumes")]
[JsonDerivedType(typeof(SplitVolumeOp), "splitVolume")]
[JsonDerivedType(typeof(ReorderSiblingsOp), "reorderSiblings")]
[JsonDerivedType(typeof(UpdateSegmentTextOp), "updateSegmentText")]
public abstract record BookRestructureOp;

/// <summary>Move a volume or chapter under a new parent, with an order.</summary>
public sealed record MoveNodeOp(
	string NodeId,
	string NewParentId,
	double NewOrder) : BookRestructureOp;

/// <summary>Delete a node (series/volume/chapter) and its subtree.</summary>
public sealed record DeleteNodeOp(
	string NodeId) : BookRestructureOp;

/// <summary>Merge chapters into a base chapter (same volume).</summary>
public sealed record MergeChaptersOp(
	string BaseChapterId,
	List<string> ChapterIds) : BookRestructureOp;

/// <summary>Split a chapter into two or more chapters at segment boundaries.</summary>
public sealed record SplitChapterOp(
	string ChapterId,
	List<SplitPointDto> SplitPoints) : BookRestructureOp;

/// <summary>Merge volumes into a base volume (same series).</summary>
public sealed record MergeVolumesOp(
	string BaseVolumeId,
	List<string> VolumeIds) : BookRestructureOp;

/// <summary>Split a volume at a chapter boundary into two volumes.</summary>
public sealed record SplitVolumeOp(
	string VolumeId,
	double AtChapterOrder,
	string NewVolumeTitle) : BookRestructureOp;

/// <summary>Reorder sibling nodes under a parent.</summary>
public sealed record ReorderSiblingsOp(
	string ParentId,
	List<string> OrderedChildIds) : BookRestructureOp;

/// <summary>Update the text runs of a single text segment (the surgical edit).</summary>
public sealed record UpdateSegmentTextOp(
	string SegmentId,
	List<TextRunDto> Runs) : BookRestructureOp;
