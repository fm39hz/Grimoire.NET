# Source reconciliation core

Status: accepted implementation contract.

## Decision

Grimoire is a source-agnostic editorial refinery. Acquisition tools remain outside the system and submit a valid `SourcePackage`. Grimoire owns neither the acquired bytes nor a universal bibliographic truth. It owns the explicit transformation from observed source material into a reviewable editorial tree and then into publication artifacts.

The core flow is:

```text
SourcePackage + current BookTree + ResearchProfile + ImportBindings
    -> ReconciliationPlan
    -> policy and human decisions
    -> transactional execution
    -> revised BookTree
    -> publishing
```

Planning is pure and cannot mutate repositories. Every mutation must be represented by an inspectable plan operation. Execution rejects plans whose captured series revision differs from the current revision.

## Boundaries

- Crawlers and downloaders are producers, not Grimoire components. Site-specific DOM rules belong to them.
- Ingress validates a generic package and persists its source identity and staged payload.
- Research gathers evidence and candidates. Evidence never silently becomes editorial truth.
- Reconciliation resolves identity, aligns structures, compares content and emits operations/issues.
- Execution applies approved operations atomically and advances `Series.Revision` once.
- Obsidian is the review and editing surface. Frontmatter IDs are exact identity bindings.
- Publishing consumes only the editorial tree; it does not inspect acquisition sources.

## Source package contract

A package has a schema version, producer identity/version, idempotency key, source descriptor, optional target hints, `Patch` or `Snapshot` semantics, a generic source-node tree and assets.

Every source node has a producer-stable external key, optional target ID, kind (`Container`, `Content`, `Asset`), open role hint, title/order hints, optional content plus hash, relations and children. Display metadata is preserved; normalized values exist only for matching.

Segment content may carry footnotes alongside its serialized segment list. Container metadata is an open JSON object; the executor currently materializes volume cover, ISBN and publication date. Producers resolve local asset files to Grimoire asset IDs before submitting inline segments.

`Patch` omission means no change. `Snapshot` omission may propose archival/removal but always requires review unless an explicit future policy permits it.

## Persistent identity

- `ImportSource`: stable identity for one producer source.
- `ImportBinding`: source-node key to editorial-node identity, with `Primary`, `Supplement` or `Reference` role, base content hash/snapshot and pin state.
- `ImportRun`: idempotent analyze/decide/commit lifecycle with staged package, captured revision, plan and decisions.
- `SeriesResearchProfile`: aliases, creators, evidence, structural hints, coverage and discovery candidates.

Bindings and explicit target IDs always outrank heuristic matching. `Order` is a positional hint, never identity.

## Reconciliation rules

Sibling trees are aligned as sequences using anchors and dynamic programming. Candidate scores combine exact binding, logical keys, normalized/fuzzy title, compatible role, content hash, order proximity and neighboring consistency. Incompatible node types cannot match. Thresholds and score explanations must be returned in the plan.

Content uses three-way comparison: last imported base (`B`), current local (`L`) and incoming remote (`R`). Upstream replacement is safe only when local content still equals the base. Divergent local and remote edits are a conflict; prose is never auto-merged.

Composition roles are explicit. A supplement cannot replace primary content. A pinned primary is sticky. Competing primary sources require review. Reference nodes provide evidence without materializing content. Placeholders and delegation notices are evidence/coverage unless a producer explicitly submits them as editorial content.

## Policy

Safe automatic operations include creating new nodes, exact-bound upstream updates when local equals base, same-hash no-ops, supplement attachments and binding refreshes. Ambiguous identity, unbound replacement, concurrent edits, competing primary sources, cross-parent moves, snapshot removals and pinned replacements require review.

Invalid IDs, cycles, duplicate external keys, missing references, stale revisions and domain-invariant violations are rejected.

## Rollout

The feature switches under `Features:IngestionCore` default off:

- `Enabled`: exposes the import-run API.
- `ShadowMode`: analyzes legacy traffic without mutating from the new plan.
- `ResearchEnabled`: permits configured provider calls.
- `LegacyAdaptersEnabled`: routes legacy EPUB/sync requests through adapters.

Rollout proceeds through pure kernel, analyze-only persistence/API, shadow comparison, executor, research, multi-source composition, Obsidian review UX, legacy cutover and publication batches. The development database may be recreated while the schema is in flux; migrations are introduced once ingestion persistence stabilizes.

## Implemented API lifecycle

- `POST /api/v1/imports` validates and analyzes a `SourcePackage`; repeated producer/idempotency keys return the same run.
- `GET /api/v1/imports/{id}/plan` exposes scored operations and issues without mutation.
- `PUT /api/v1/imports/{id}/decisions` records explicit approve/reject decisions.
- `POST /api/v1/imports/{id}/commit-safe` applies only automatic operations and leaves rewritten review work pending.
- `POST /api/v1/imports/{id}/commit` applies the decided plan transactionally and rejects stale revisions.
- `/api/v1/research/series/{seriesId}` exposes evidence, manual confirmations, refresh and discovered-source candidates.

## Publication packages

Publishing accepts four modes: `Anthology`, `Single`, `OnePerVolume`, and `CustomGroups`. The latter two produce one downloadable ZIP containing the requested EPUB/format artifacts plus `manifest.json`. The manifest records schema version, series identity and revision, group-to-volume membership, byte sizes and SHA-256 checksums. Export deduplication keys include the normalized mode, targets, groups and structure, so unlike requests cannot reuse the wrong artifact.

The job status response exposes the package filename/content type and artifact summaries. The ZIP remains the canonical portable result; publishing only reads the committed editorial tree and never source packages or crawler metadata.

## Hako producer flow

`hako-crawler-js` derives a work key from the stable novel slug after Hako's numeric entry ID. Duplicate Hako entries with the same slug remain separate `ImportSource` records but are imported into one editorial series. The source with the widest chapter coverage supplies the bootstrap title/metadata; every observed title is sent as an alias hint.

Hako-specific structure stays in the producer adapter: explicit `Arc N` ordinals override page-local list indexes, illustration-only pseudo-volumes are folded into the following arc as frontmatter chapters, empty/notice volumes become placeholder evidence, and trailing unnumbered material receives a collision-free structural order. Packages are streamed in bounded chapter batches and committed sequentially per work, preventing request-size spikes and stale-plan races. Reimporting unchanged bundles returns the same committed runs.

## Required invariants

- Reimporting the same package is idempotent.
- Local edits are never silently overwritten.
- Every committed mutation is attributable to a plan and decision.
- Research failure cannot block manual import.
- Source acquisition and publication remain separate.
- No identity decision depends on sibling order alone.
