param([string]$ServerRoot='C:\xampp\htdocs\veterenar', [string]$Database='vet_clinic')
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
$target=(Resolve-Path -LiteralPath $ServerRoot).Path
$php='C:\xampp\php\php.exe'
$utf8=[Text.UTF8Encoding]::new($false)
if($Database -notmatch '^[a-zA-Z0-9_]+$') { throw 'Invalid database name' }
$expected=@{
 'admin.php'='BE13E28A2A2404760DA638BF6A1C9CF5AD724CD53613672CC15195730850A77A'
 'index.php'='A410F4AD5783D48B4ABA1D4B366132BE029429081265E9661391DA2D6B0FEE5D'
}
$items=[Collections.Generic.List[object]]::new()
foreach($relative in @('appointments/create.php','appointments/list.php','clients/profile.php','schedule/get_zapis_admin.php')) {
 $text=[IO.File]::ReadAllText((Join-Path $project ('api/'+$relative)))
 $text=$text.Replace('__DIR__ . "/../db.php"','__DIR__ . "/../../db.php"')
 $text=$text.Replace('__DIR__ . "/../includes/doctor_booking.php"','__DIR__ . "/../../includes/doctor_booking.php"')
 $items.Add(@{Relative='api/'+$relative;Text=$text})
}
foreach($relative in $expected.Keys) {
 if((Get-FileHash -LiteralPath (Join-Path $target $relative)).Hash -ne $expected[$relative]) { throw "Live file changed: $relative" }
 $items.Add(@{Relative=$relative;Text=[IO.File]::ReadAllText((Join-Path $project ('server/'+$relative)))})
}
$backup=Join-Path $project ('.deployment-backups/field-names-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $backup -Force | Out-Null
foreach($item in $items) {
 $stage=Join-Path $backup ('prepared/'+$item.Relative)
 New-Item -ItemType Directory -Path (Split-Path $stage -Parent) -Force | Out-Null
 [IO.File]::WriteAllText($stage,$item.Text,$utf8)
 & $php -l $stage | Out-Host
 if($LASTEXITCODE -ne 0) { throw 'PHP syntax error' }
 $saved=Join-Path $backup ('files/'+$item.Relative)
 New-Item -ItemType Directory -Path (Split-Path $saved -Parent) -Force | Out-Null
 Copy-Item -LiteralPath (Join-Path $target $item.Relative) -Destination $saved
}
$access=Join-Path $target '.htaccess'
$hadAccess=Test-Path -LiteralPath $access
if($hadAccess) { Copy-Item -LiteralPath $access -Destination (Join-Path $backup 'original.htaccess') }
[IO.File]::WriteAllText($access,"Require all denied`r`n",$utf8)
try {
 Start-Sleep -Seconds 10
 & C:\xampp\mysql\bin\mysqldump.exe -u root --single-transaction --triggers "--result-file=$backup/database.sql" $Database
 if($LASTEXITCODE -ne 0) { throw 'Database backup failed' }
 foreach($item in $items) { [IO.File]::WriteAllText((Join-Path $target $item.Relative),$item.Text,$utf8) }
 & $php (Join-Path $PSScriptRoot 'Migrate-FieldNames.php') $Database
 if($LASTEXITCODE -ne 0) { throw 'Schema migration failed' }
 if($hadAccess) { Copy-Item -LiteralPath (Join-Path $backup 'original.htaccess') -Destination $access -Force }
 else { Remove-Item -LiteralPath $access }
 Write-Host "Fields and server updated. Backup: $backup"
} catch {
 Write-Host "Deployment stopped; maintenance remains enabled. Backup: $backup"
 throw
}
