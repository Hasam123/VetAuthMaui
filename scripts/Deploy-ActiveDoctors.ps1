param(
    [string]$ServerRoot = 'C:\xampp\htdocs\veterenar'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$targetRoot = (Resolve-Path -LiteralPath $ServerRoot).Path
$php = 'C:\xampp\php\php.exe'
$utf8 = [System.Text.UTF8Encoding]::new($false)

$files = @(
    @{ Source = 'api\includes\doctor_booking.php'; Target = 'includes\doctor_booking.php' },
    @{ Source = 'api\doctors\list.php'; Target = 'api\doctors\list.php' },
    @{ Source = 'api\schedule\free_slots.php'; Target = 'api\schedule\free_slots.php' },
    @{ Source = 'api\appointments\create.php'; Target = 'api\appointments\create.php' }
)

# Prepare and check every source before changing any running server file.
$prepared = foreach ($file in $files) {
    $source = Join-Path $projectRoot $file.Source
    & $php -l $source | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "PHP syntax check failed: $source" }
    $text = [System.IO.File]::ReadAllText($source)
    $text = $text.Replace('__DIR__ . "/../db.php"', '__DIR__ . "/../../db.php"')
    $text = $text.Replace('__DIR__ . "/../includes/doctor_booking.php"', '__DIR__ . "/../../includes/doctor_booking.php"')
    $destination = Join-Path $targetRoot $file.Target
    if (-not (Test-Path -LiteralPath $destination -PathType Leaf)) {
        throw "Server file not found: $destination"
    }
    @{ Target = $destination; Text = $text; Relative = $file.Target }
}

$backupRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('VetAuth-active-doctors-' + [guid]::NewGuid().ToString('N'))
foreach ($file in $prepared) {
    $backup = Join-Path $backupRoot $file.Relative
    New-Item -ItemType Directory -Path (Split-Path $backup -Parent) -Force | Out-Null
    Copy-Item -LiteralPath $file.Target -Destination $backup
}

try {
    foreach ($file in $prepared) {
        [System.IO.File]::WriteAllText($file.Target, $file.Text, $utf8)
    }
    Write-Host 'Active doctor filtering deployed successfully.'
    Write-Host "Backup: $backupRoot"
} catch {
    foreach ($file in $prepared) {
        Copy-Item -LiteralPath (Join-Path $backupRoot $file.Relative) -Destination $file.Target -Force
    }
    throw
}
