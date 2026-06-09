# test

## .NET unit tests

Run from repo root:

```bash
make test-unit
```

The `.NET` unit test project covers `BookTreeService` invariants without requiring PostgreSQL.

## Bun tests

To install dependencies:

```bash
bun install
```

Run from `test/`:

```bash
make test-bun
```

Default Bun coverage:

- Whitebox tree tests inspect source-level architecture hooks.
- Tree blackbox contract tests validate the response shape a client sees.
- Live tree blackbox tests are registered only when an API URL is provided.

To run tree blackbox tests against a live API:

```bash
make test-blackbox GRIMOIRE_BLACKBOX_BASE_URL=http://localhost:5062/api/v1
```

To run import:

```bash
bun run index.ts
```
