param([string]$ServerRoot='C:\xampp\htdocs\veterenar')
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
$target=(Resolve-Path -LiteralPath $ServerRoot).Path
$php='C:\xampp\php\php.exe'
$enc=[Text.UTF8Encoding]::new($false)
$backup=Join-Path $project ('.deployment-backups/journal-api-'+[guid]::NewGuid().ToString('N'))
$items=[Collections.Generic.List[object]]::new()
foreach($file in Get-ChildItem (Join-Path $project 'api') -Recurse -Filter '*.php') {
 $relative=[IO.Path]::GetRelativePath((Join-Path $project 'api'),$file.FullName)
 if($relative -eq 'db.php') {continue}
 $dest=if($relative.StartsWith('includes\')) {$relative} else {Join-Path 'api' $relative}
 $text=[IO.File]::ReadAllText($file.FullName)
 $text=$text.Replace('__DIR__ . "/../db.php"','__DIR__ . "/../../db.php"').Replace("__DIR__ . '/../db.php'","__DIR__ . '/../../db.php'")
 foreach($helper in @('doctor_booking.php','journal.php')) {
  $text=$text.Replace('__DIR__ . "/../includes/'+$helper+'"','__DIR__ . "/../../includes/'+$helper+'"')
 }
 $stage=Join-Path $backup ('prepared/'+$dest)
 New-Item -ItemType Directory -Path (Split-Path $stage -Parent) -Force | Out-Null
 [IO.File]::WriteAllText($stage,$text,$enc)
 & $php -l $stage | Out-Host
 if($LASTEXITCODE -ne 0) {throw "Invalid PHP: $relative"}
 $live=Join-Path $target $dest
 $exists=Test-Path -LiteralPath $live
 $saved=Join-Path $backup ('original/'+$dest)
 if($exists){New-Item -ItemType Directory -Path (Split-Path $saved -Parent) -Force | Out-Null;Copy-Item -LiteralPath $live -Destination $saved}
 $items.Add(@{Live=$live;Stage=$stage;Saved=$saved;Existed=$exists})
}
try {
 foreach($item in $items){New-Item -ItemType Directory -Path (Split-Path $item.Live -Parent) -Force | Out-Null;Copy-Item -LiteralPath $item.Stage -Destination $item.Live -Force}
 Write-Host "Clinic journal API deployed. Backup: $backup"
} catch {
 foreach($item in $items){if($item.Existed){Copy-Item -LiteralPath $item.Saved -Destination $item.Live -Force}else{if(Test-Path -LiteralPath $item.Live){Remove-Item -LiteralPath $item.Live}}}
 throw
}
