# Grimoire.NET Makefile

# === Configuration ===
# Create .env file to override these defaults
-include .env

CONFIGURATION ?= Debug
COMPOSE_FILE  ?= docker-compose.yml

# === Targets ===

.PHONY: help build run debug \
	test test-unit test-bun test-blackbox \
	verify init \
	db-up db-down db-clear \
	clean

.DEFAULT_GOAL := help

# === Build ===

build: ## Build the application
	dotnet build Grimoire.NET.slnx -c $(CONFIGURATION)

# === Test ===

test: build ## Run all default tests
	dotnet test Grimoire.NET.slnx -c $(CONFIGURATION) --no-build
	cd test && bun test

test-unit: ## Run .NET unit tests
	dotnet test Grimoire.NET.slnx -c $(CONFIGURATION)

test-bun: ## Run Bun whitebox and blackbox contract tests
	cd test && bun test

test-blackbox: ## Run Bun tests against a live API
	@if [ -z "$(GRIMOIRE_BLACKBOX_BASE_URL)" ]; then \
		echo "GRIMOIRE_BLACKBOX_BASE_URL is required, e.g. http://localhost:5062/api/v1"; \
		exit 1; \
	fi
	cd test && GRIMOIRE_BLACKBOX_BASE_URL="$(GRIMOIRE_BLACKBOX_BASE_URL)" bun test

# === Verify ===

verify: build test ## Build and run all tests

# === Run / Debug ===

run: build ## Run the API
	dotnet run --project src/Grimoire.Api -c $(CONFIGURATION)

debug: build ## Debug the API with hot reload
	dotnet watch --project src/Grimoire.Api -c $(CONFIGURATION)

init: db-clear db-up debug ## Initialize dev environment (reset DB + start debug)

# === Database (via Docker Compose) ===

db-up: ## Start PostgreSQL
	docker compose -f $(COMPOSE_FILE) up -d postgres
	@echo "PostgreSQL started. Wait a few seconds for it to be ready."

db-down: ## Stop PostgreSQL
	docker compose -f $(COMPOSE_FILE) down

db-clear: ## Remove DB data and stop
	docker compose -f $(COMPOSE_FILE) down -v
	rm -rf /tmp/grimoire-files/
	@echo "Database cleared and removed."

# === Cleanup ===

clean: db-clear ## Clean everything (DB + build + NuGet cache)
	dotnet clean Grimoire.NET.slnx -c $(CONFIGURATION)
	dotnet nuget locals all --clear

# === Help ===

help: ## Show this help
	@echo "Grimoire.NET Makefile"
	@echo ""
	@echo "Usage: make [target] [CONFIGURATION=Release]"
	@echo ""
	@echo "Build:"
	@echo "  make build          Build the application"
	@echo ""
	@echo "Test:"
	@echo "  make test           Build and run all tests"
	@echo "  make test-unit      Run .NET unit tests"
	@echo "  make test-bun       Run Bun contract tests"
	@echo "  make test-blackbox  Run Bun tests against live API"
	@echo ""
	@echo "Run:"
	@echo "  make run            Run the API"
	@echo "  make debug          Debug with hot reload"
	@echo "  make init           Reset DB and start debugging"
	@echo ""
	@echo "Database:"
	@echo "  make db-up          Start PostgreSQL (docker compose)"
	@echo "  make db-down        Stop PostgreSQL"
	@echo "  make db-clear       Remove data and stop"
	@echo ""
	@echo "Utility:"
	@echo "  make verify         Build + run all tests"
	@echo "  make clean          Remove DB + build + NuGet cache"
	@echo "  make help           Show this message"
	@echo ""
	@echo "Override defaults via .env or make args, e.g.:"
	@echo "  make build CONFIGURATION=Release"
	@echo "  make db-up COMPOSE_FILE=docker-compose.prod.yml"
