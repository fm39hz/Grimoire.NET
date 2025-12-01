# Grimoire.NET Makefile

.PHONY: build run db-up db-down clean help

# Default target
help: ## Show this help message
	@echo "Grimoire.NET Makefile"
	@echo ""
	@echo "Usage:"
	@echo "  make build     - Build the application"
	@echo "  make run       - Run the application"
	@echo "  make db-up     - Start PostgreSQL database with Docker"
	@echo "  make db-down   - Stop PostgreSQL database"
	@echo "  make clean     - Clean build artifacts"
	@echo ""

build: ## Build the application
	dotnet build

run: ## Run the application
	dotnet run --project src/Grimoire.Api

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

clean: ## Clean build artifacts
	dotnet clean
	dotnet nuget locals all --clear

test: ## Run tests
	dotnet test