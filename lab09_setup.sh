#!/usr/bin/env bash
# Налаштування лабораторної 9: запускати з кореня проєкту (де лежить CrossApp.slnx).
set -e
cd "$(dirname "$0")"

# 1) Пакети конфігурації ставляться в Cli, а не в Core: домен про файли конфігурації не знає.
for pkg in \
  Microsoft.Extensions.Configuration \
  Microsoft.Extensions.Configuration.Json \
  Microsoft.Extensions.Configuration.EnvironmentVariables \
  Microsoft.Extensions.Configuration.Binder \
  Microsoft.Extensions.Configuration.UserSecrets \
  Microsoft.Extensions.Configuration.CommandLine; do
  dotnet add src/Cli package "$pkg" --version "10.*"
done

# 2) appsettings*.json копіюються у вихідний і публікаційний каталоги.
if ! grep -q "appsettings" src/Cli/Cli.csproj; then
  sed -i 's|</Project>|  <ItemGroup>\n    <None Update="appsettings*.json">\n      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>\n      <CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>\n    </None>\n  </ItemGroup>\n\n</Project>|' src/Cli/Cli.csproj
fi

# 3) user-secrets: у репозиторій потрапляє лише UserSecretsId, значення лежить поза репозиторієм.
dotnet user-secrets init --project src/Cli
dotnet user-secrets set "Integration:ApiKey" "local-dev-key" --project src/Cli
dotnet user-secrets list --project src/Cli

# 4) .gitignore: дані, тимчасові файли, секрети.
touch .gitignore
for line in "**/data/items.json" "*.tmp" "secrets.json"; do
  grep -qxF "$line" .gitignore || echo "$line" >> .gitignore
done

dotnet build
dotnet test
