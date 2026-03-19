# Check Code Coverage

Run the test suite with code coverage collection and report the results.

## Steps

1. Run all tests with coverage:
   ```bash
   dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage-results
   ```

2. Generate a human-readable report:
   ```bash
   reportgenerator \
     -reports:"coverage-results/**/coverage.cobertura.xml" \
     -targetdir:"coverage-report" \
     -reporttypes:TextSummary
   ```

3. Display the summary:
   ```bash
   cat coverage-report/Summary.txt
   ```

4. If coverage is below 100%:
   - Identify uncovered lines from the report
   - List the specific files and line numbers that need tests
   - Suggest test cases to cover the gaps

## Target

The project requires **100% line coverage** and **100% method coverage**. Any uncovered code must be addressed before merging.
