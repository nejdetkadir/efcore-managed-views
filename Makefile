.PHONY: build test test-unit test-integration test-functional coverage clean restore lint pack

restore:
	dotnet restore

build: restore
	dotnet build

build-release: restore
	dotnet build --configuration Release

test:
	dotnet test

test-unit:
	dotnet test tests/EntityFrameworkCore.ManagedViews.Tests
	dotnet test tests/EntityFrameworkCore.ManagedViews.PostgreSQL.Tests

test-integration:
	dotnet test tests/EntityFrameworkCore.ManagedViews.IntegrationTests

test-functional:
	dotnet test tests/EntityFrameworkCore.ManagedViews.FunctionalTests

coverage:
	dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage-results

coverage-report: coverage
	reportgenerator -reports:"coverage-results/**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:TextSummary
	cat coverage-report/Summary.txt

clean:
	dotnet clean
	rm -rf bin/ obj/ coverage-results/ coverage-report/ TestResults/

pack:
	dotnet pack --configuration Release --output ./artifacts

lint:
	dotnet build --configuration Release /p:TreatWarningsAsErrors=true
