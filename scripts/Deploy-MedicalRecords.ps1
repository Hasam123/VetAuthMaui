param(
    [string]$ServerRoot = 'C:\xampp\htdocs\veterenar',
    [string]$Database = 'vet_clinic',
    [string]$Php = 'C:\xampp\php\php.exe',
    [string]$MysqlDump = 'C:\xampp\mysql\bin\mysqldump.exe'
)
# Run locally from PowerShell after closing the application.
# Installs the API, web dependency and database migration together.
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
$target = (Resolve-Path -LiteralPath $ServerRoot).Path
$utf8 = [Text.UTF8Encoding]::new($false)
if ($Database -notmatch '^[a-zA-Z0-9_]+$') { throw 'Invalid database name' }
$originalWebHash = '4292F9F6EE6D21BC6D767525457B305245787F69875788912C0E68A47A015C7B'
if ((Get-FileHash -LiteralPath (Join-Path $target 'admin.php') -Algorithm SHA256).Hash -ne $originalWebHash) {
    throw 'The live admin.php has changed. Reconcile it with server/admin.php before deploying.'
}
$prepared = [Collections.Generic.List[object]]::new()
foreach ($file in Get-ChildItem (Join-Path $project 'api') -Recurse -Filter '*.php') {
    $relative = [IO.Path]::GetRelativePath((Join-Path $project 'api'), $file.FullName)
    if ($relative -eq 'db.php') { continue } # Preserve server connection settings.
    $destination = if ($relative.StartsWith('includes\')) { $relative } else { Join-Path 'api' $relative }
    $text = [IO.File]::ReadAllText($file.FullName)
    $text = $text.Replace('__DIR__ . "/../db.php"', '__DIR__ . "/../../db.php"')
    $text = $text.Replace('__DIR__ . "/../includes/doctor_booking.php"', '__DIR__ . "/../../includes/doctor_booking.php"')
    $prepared.Add(@{ Relative = $destination; Text = $text })
}
$prepared.Add(@{ Relative = 'admin.php'; Text = [IO.File]::ReadAllText((Join-Path $project 'server/admin.php')) })
$backup = Join-Path $project ('.deployment-backups\medical-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $backup -Force | Out-Null
# Check the exact transformed PHP, not just the repository sources.
foreach ($file in $prepared) {
    $staged = Join-Path $backup ('prepared\' + $file.Relative)
    New-Item -ItemType Directory -Path (Split-Path $staged -Parent) -Force | Out-Null
    [IO.File]::WriteAllText($staged, $file.Text, $utf8)
    & $Php -l $staged | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "Invalid PHP: $($file.Relative)" }
    $live = Join-Path $target $file.Relative
    if (-not (Test-Path -LiteralPath $live -PathType Leaf)) { throw "Missing server dependency: $live" }
    $saved = Join-Path $backup ('files\' + $file.Relative)
    New-Item -ItemType Directory -Path (Split-Path $saved -Parent) -Force | Out-Null
    Copy-Item -LiteralPath $live -Destination $saved
}
$access = Join-Path $target '.htaccess'
$hadAccess = Test-Path -LiteralPath $access
if ($hadAccess) { Copy-Item -LiteralPath $access -Destination (Join-Path $backup 'original.htaccess') }
# Apache serves neither the web panel nor API while the schema is switching.
[IO.File]::WriteAllText($access, "Require all denied`r`n", $utf8)
try {
    # Allow requests already executing before maintenance to finish.
    Start-Sleep -Seconds 10
    $dumpPath = Join-Path $backup 'database.sql'
    & $MysqlDump -u root --single-transaction --routines --triggers --default-character-set=utf8mb4 "--result-file=$dumpPath" $Database
    if ($LASTEXITCODE -ne 0) { throw 'Database backup failed' }
    foreach ($file in $prepared) {
        [IO.File]::WriteAllText((Join-Path $target $file.Relative), $file.Text, $utf8)
    }
    & $Php (Join-Path $PSScriptRoot 'Migrate-MedicalRecords.php') $Database
    if ($LASTEXITCODE -ne 0) { throw 'Database migration failed' }
    if ($hadAccess) { Copy-Item -LiteralPath (Join-Path $backup 'original.htaccess') -Destination $access -Force }
    else { Remove-Item -LiteralPath $access }
    Write-Host 'Medical records, API and web panel deployed. Admin accounts preserved.'
    Write-Host "Backup: $backup"
} catch {
    # Keep maintenance enabled: do not expose mismatched API/schema to clients.
    Write-Error "Deployment stopped. Maintenance remains enabled. Backup: $backup. $($_.Exception.Message)"
    throw
}
