# Grimoire.NET Makefile

.PHONY: help

# Default target
help: ## Show this help message
	@echo "Grimoire.NET Makefile"
	@echo ""
	@echo "Usage:"
	@echo "  make build     - Build the application"
	@echo "  make init      - Initialize the application"
	@echo "  make run       - Run the application"
	@echo "  make debug     - Debug the application"
	@echo "  make db-up     - Start PostgreSQL database with Docker"
	@echo "  make db-down   - Stop PostgreSQL database"
	@echo "  make db-clear 	- Clear database and stop PostgreSQL database"
	@echo "  make clean     - Clean build artifacts"
	@echo ""

build: ## Build the application
	dotnet build

run: build ## Run the API
	clear
	dotnet run --project src/Grimoire.Api

run-job: build ## Run the background job worker
	clear
	dotnet run --project src/Grimoire.Job

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
