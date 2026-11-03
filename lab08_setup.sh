#!/usr/bin/env bash
# Налаштування лабораторної 8: запускати з кореня проєкту (де лежить CrossApp.slnx).
set -e
cd "$(dirname "$0")"

# 1) Бізнес-правила в сутностях кидають DomainException замість InvalidOperationException.
#    DomainException оголошено в просторі імен Core, тому видимий і в Core.Domain без using.
sed -i 's/InvalidOperationException/DomainException/g' src/Core/Domain/Product.cs src/Core/Domain/Warehouse.cs

# 2) Проєкт тестів (крок 1 методички); посилання лише на Core.
if [ ! -f tests/Core.Tests/Core.Tests.csproj ]; then
  dotnet new xunit -n Core.Tests -o tests/Core.Tests -f net10.0
  dotnet sln add tests/Core.Tests/Core.Tests.csproj
  dotnet add tests/Core.Tests reference src/Core/Core.csproj
  rm -f tests/Core.Tests/UnitTest1.cs
fi

dotnet build
dotnet test
