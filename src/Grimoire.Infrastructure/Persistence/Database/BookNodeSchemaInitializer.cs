namespace Grimoire.Infrastructure.Persistence.Database;

using Microsoft.EntityFrameworkCore;

public static class BookNodeSchemaInitializer {
	public static async Task EnsureBookNodesAsync(this ApplicationDbContext context, CancellationToken cancellationToken = default) {
		await context.Database.ExecuteSqlRawAsync("""
			CREATE EXTENSION IF NOT EXISTS ltree;
			""", cancellationToken);

		await context.Database.ExecuteSqlRawAsync("""
			CREATE TABLE IF NOT EXISTS book_nodes (
				id uuid PRIMARY KEY,
				type integer NOT NULL,
				parent_id uuid NULL REFERENCES book_nodes(id) ON DELETE CASCADE,
				"order" double precision NOT NULL,
				title character varying(500) NOT NULL,
				path ltree NOT NULL,
				created_at timestamp with time zone NOT NULL,
				updated_at timestamp with time zone NOT NULL
			);
			""", cancellationToken);

		await context.Database.ExecuteSqlRawAsync("""
			CREATE INDEX IF NOT EXISTS ix_book_nodes_parent_id ON book_nodes(parent_id);
			""", cancellationToken);

		await context.Database.ExecuteSqlRawAsync("""
			CREATE INDEX IF NOT EXISTS ix_book_nodes_path ON book_nodes USING gist(path);
			""", cancellationToken);

		await context.Database.ExecuteSqlRawAsync("""
			CREATE UNIQUE INDEX IF NOT EXISTS ix_book_nodes_parent_id_order ON book_nodes(parent_id, "order");
			""", cancellationToken);

		var hasLegacyTables = await context.Database
			.SqlQueryRaw<bool>("""
				SELECT (to_regclass('public.series') IS NOT NULL
					AND to_regclass('public.volumes') IS NOT NULL
					AND to_regclass('public.chapters') IS NOT NULL
					AND to_regclass('public.assets') IS NOT NULL) AS "Value"
			""")
			.SingleAsync(cancellationToken);

		if (!hasLegacyTables) {
			return;
		}

		await context.Database.ExecuteSqlRawAsync("""
			ALTER TABLE assets ADD COLUMN IF NOT EXISTS owner_node_id uuid NULL;
			""", cancellationToken);

		await context.Database.ExecuteSqlRawAsync("""
			CREATE INDEX IF NOT EXISTS ix_assets_owner_node_id ON assets(owner_node_id);
			""", cancellationToken);

		await context.Database.ExecuteSqlRawAsync("""
			INSERT INTO book_nodes (id, type, parent_id, "order", title, path, created_at, updated_at)
			SELECT s.id, 0, NULL, 0, s.title, ('n' || replace(s.id::text, '-', ''))::ltree, s.created_at, s.updated_at
			FROM series s
			WHERE NOT EXISTS (SELECT 1 FROM book_nodes n WHERE n.id = s.id);
			""", cancellationToken);

		await context.Database.ExecuteSqlRawAsync("""
			INSERT INTO book_nodes (id, type, parent_id, "order", title, path, created_at, updated_at)
			SELECT v.id, 1, v.series_id, v."order", v.title, ('n' || replace(v.series_id::text, '-', '') || '.' || 'n' || replace(v.id::text, '-', ''))::ltree, v.created_at, v.updated_at
			FROM volumes v
			WHERE NOT EXISTS (SELECT 1 FROM book_nodes n WHERE n.id = v.id);
			""", cancellationToken);

		await context.Database.ExecuteSqlRawAsync("""
			INSERT INTO book_nodes (id, type, parent_id, "order", title, path, created_at, updated_at)
			SELECT c.id, 2, c.volume_id, c."order", c.title, ('n' || replace(v.series_id::text, '-', '') || '.' || 'n' || replace(c.volume_id::text, '-', '') || '.' || 'n' || replace(c.id::text, '-', ''))::ltree, c.created_at, c.updated_at
			FROM chapters c
			INNER JOIN volumes v ON c.volume_id = v.id
			WHERE NOT EXISTS (SELECT 1 FROM book_nodes n WHERE n.id = c.id);
			""", cancellationToken);
	}
}
