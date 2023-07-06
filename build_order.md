# On commit pipeline

## 1. Run the tests

`dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:CoverletOutput=../Builds/TestResults/coverage.opencover.xml`

## 2. Run the docs Autobuilder

`dotnet run --project "./DocBuilder/DocBuilder.csproj"`

# On publish pipeline

## 3. Publish

`dotnet publish -c:Release --property WarningLevel=0`