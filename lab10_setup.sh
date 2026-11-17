#!/usr/bin/env bash
# Налаштування лабораторної 10: запускати з кореня проєкту (де лежить CrossApp.slnx).
set -e
cd "$(dirname "$0")"

# 1) src/Api (Minimal API) уже містить Api.csproj із посиланням на Core і пакетами OpenApi, Scalar, FluentValidation.
dotnet sln list | grep -q "Api.csproj" || dotnet sln add src/Api/Api.csproj

# 2) Інтеграційні тести (додаткове завдання 4): tests/Api.Tests.
if [ ! -f tests/Api.Tests/Api.Tests.csproj ]; then
  dotnet new xunit -n Api.Tests -o tests/Api.Tests -f net10.0
  rm -f tests/Api.Tests/UnitTest1.cs
  dotnet add tests/Api.Tests reference src/Api/Api.csproj
  dotnet add tests/Api.Tests package Microsoft.AspNetCore.Mvc.Testing --version "10.*"
fi
dotnet sln list | grep -q "Api.Tests.csproj" || dotnet sln add tests/Api.Tests/Api.Tests.csproj

# 3) .gitignore для результатів тестів.
touch .gitignore
grep -qxF "TestResults/" .gitignore || echo "TestResults/" >> .gitignore

dotnet build
dotnet test
echo
echo "Запуск Api:  dotnet run --project src/Api --urls http://localhost:5080"
echo "Scalar UI:   http://localhost:5080/scalar/v1"
