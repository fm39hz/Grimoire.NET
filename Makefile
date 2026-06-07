# Grimoire.NET Makefile

.PHONY: help build run debug test test-unit test-bun test-blackbox verify init db-up db-down db-clear clean

# Default target
help: ## Show this help message
	@echo "Grimoire.NET Makefile"
	@echo ""
	@echo "Usage:"
	@echo "  make build     - Build the application"
	@echo "  make test      - Run all default tests"
	@echo "  make test-unit - Run .NET unit tests"
	@echo "  make test-bun  - Run Bun whitebox/blackbox contract tests"
	@echo "  make test-blackbox GRIMOIRE_BLACKBOX_BASE_URL=http://localhost:5062/api/v1"
	@echo "  make verify    - Build and run default tests"
	@echo "  make init      - Initialize the application"
	@echo "  make run       - Run the application"
	@echo "  make debug     - Debug the application"
	@echo "  make db-up     - Start PostgreSQL database with Docker"
	@echo "  make db-down   - Stop PostgreSQL database"
	@echo "  make db-clear 	- Clear database and stop PostgreSQL database"
	@echo "  make clean     - Clean build artifacts"
	@echo ""

build: ## Build the application
	dotnet build Grimoire.NET.slnx

test: test-unit test-bun ## Run all default tests

test-unit: ## Run .NET unit tests
	dotnet test Grimoire.NET.slnx

test-bun: ## Run Bun whitebox and blackbox contract tests
	cd test && bun test

test-blackbox: ## Run Bun tests against a live API
	@if [ -z "$(GRIMOIRE_BLACKBOX_BASE_URL)" ]; then \
		echo "GRIMOIRE_BLACKBOX_BASE_URL is required, e.g. http://localhost:5062/api/v1"; \
		exit 1; \
	fi
	cd test && GRIMOIRE_BLACKBOX_BASE_URL="$(GRIMOIRE_BLACKBOX_BASE_URL)" bun test

verify: build test ## Build and run default tests

run: build ## Run the API
	clear
	dotnet run --project src/Grimoire.Api

init: db-clear db-up debug ## Initialize debug session

debug: build ## Debug application
	clear
	dotnet watch --project src/Grimoire.Api

db-up: ## Start PostgreSQL database with Docker
	docker run --name grimoire-db \
		-e POSTGRES_USER=admin \
		-e POSTGRES_PASSWORD=admin \
		-e POSTGRES_DB=grimoire \
		-v pgdata:/var/lib/postgresql/data \
		-p 5432:5432 \
		-d postgres:16-alpine
	@echo "PostgreSQL database started. Wait a few seconds for it to be ready."

db-down: ## Stop PostgreSQL database
	docker stop grimoire-db || true
	docker rm grimoire-db || true
	@echo "PostgreSQL database stopped and removed."

db-clear: ## Clear database and stop PostgreSQL database
	docker exec grimoire-db psql -U admin -d grimoire -c "DROP SCHEMA public CASCADE; CREATE SCHEMA public; GRANT ALL ON SCHEMA public TO admin; GRANT ALL ON SCHEMA public TO public;" || true
	docker stop grimoire-db || true
	docker rm grimoire-db || true
	rm -rf /tmp/grimoire-files/
	@echo "PostgreSQL database cleared, stopped and removed."

clean: db-clear ## Clean build artifacts
	dotnet clean
	dotnet nuget locals all --clear
