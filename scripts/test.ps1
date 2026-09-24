$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
dotnet build (Join-Path $projectRoot 'MinanaMac.csproj') -c Release
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
$app = Join-Path $projectRoot 'bin/Release/net10.0-windows/MinanaMac.exe'
$validation = Start-Process -FilePath $app -ArgumentList '--self-test' -WindowStyle Hidden -Wait -PassThru
if ($validation.ExitCode -ne 0) { throw 'MAC validation tests failed.' }
$diagnostic = Join-Path $projectRoot 'bin/ui-test-error.txt'
$ui = Start-Process -FilePath $app -ArgumentList @('--ui-test', ('"' + $diagnostic + '"')) -WindowStyle Hidden -Wait -PassThru
if ($ui.ExitCode -ne 0) {
    if (Test-Path -LiteralPath $diagnostic) { Get-Content -LiteralPath $diagnostic }
    throw 'Read-only UI tests failed.'
}
Write-Output 'PASS: address validation, generation, read-only UI, and helper argument checks. No network settings changed.'
