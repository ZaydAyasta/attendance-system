[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$OutputDirectory,
    [string]$ConnectionString = $env:ConnectionStrings__DefaultConnection
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    throw 'Provide -ConnectionString or set ConnectionStrings__DefaultConnection.'
}

$pgDump = Get-Command pg_dump -ErrorAction SilentlyContinue
if ($null -eq $pgDump) {
    throw 'pg_dump was not found on PATH.'
}

$resolvedOutput = [System.IO.Path]::GetFullPath($OutputDirectory)
[System.IO.Directory]::CreateDirectory($resolvedOutput) | Out-Null
$timestamp = [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')
$backupPath = Join-Path $resolvedOutput "attendance-production-$timestamp.dump"

& $pgDump.Source --format=custom --file=$backupPath --dbname=$ConnectionString
if ($LASTEXITCODE -ne 0) {
    if (Test-Path -LiteralPath $backupPath) { Remove-Item -LiteralPath $backupPath -Force }
    throw "pg_dump failed with exit code $LASTEXITCODE."
}

Write-Host "Backup created: $backupPath"
