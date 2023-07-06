## 1. Run the tests

`dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:CoverletOutput=./TestResults/coverage.opencover.xml`

## 2. Publish

`dotnet publish -c:Release --property WarningLevel=0`