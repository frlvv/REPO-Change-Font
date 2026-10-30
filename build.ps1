param(
  [string]$GamePath = 'C:\Program Files (x86)\Steam\steamapps\common\REPO',
  [string]$Python = 'python'
)
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$expected = '531d9fe2f51fc526e16358645e51aa1aa2ed5ea89c49c4a115b15bb989e4dd4b'
$candidates = @((Join-Path $GamePath 'REPO_Data\sharedassets0.assets'), (Join-Path $GamePath 'FontPatchBackup-26294889\sharedassets0.assets.original'))
$source = $candidates | Where-Object { (Test-Path -LiteralPath $_) -and ((Get-FileHash -Algorithm SHA256 -LiteralPath $_).Hash.ToLowerInvariant() -eq $expected) } | Select-Object -First 1
if (-not $source) { throw 'Original supported game assets not found. Restore or verify game files first.' }
$build = Join-Path $PSScriptRoot 'build'
New-Item -ItemType Directory -Force $build, 'dist' | Out-Null
& $Python 'tools/build_asset.py' --game-path $GamePath --source $source --output $build
if ($LASTEXITCODE -ne 0) { throw 'Failed to prepare font assets.' }
& $Python 'tools/make_template.py' --game-path $GamePath --source $source --output $build
if ($LASTEXITCODE -ne 0) { throw 'Failed to create template.' }
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $csc /nologo /target:winexe /platform:x64 /optimize+ /out:'dist\R.E.P.O. Change Font.exe' /win32icon:assets\icon.ico /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll /resource:build\app-template.json.gz,RepoFontTemplate /resource:assets\icon.ico,RepoFontIcon src\FontChangerCore.cs src\FontChangerUI2.cs src\AssemblyInfo.cs
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
Write-Host 'Built dist\R.E.P.O. Change Font.exe'

