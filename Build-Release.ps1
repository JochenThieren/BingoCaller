<#
.SYNOPSIS
    Publishes, packages and checksums a BingoCaller release.

.DESCRIPTION
    There is no code-signing certificate for this project. As a free substitute, every release
    installer gets a published SHA256 checksum so a user can verify the download.

    Steps:
      0. Checks that the version in the .csproj, the installer script and CHANGELOG.md agree
      1. Cleans publish_BingoCaller\ and dist\
      2. dotnet publish (Release, win-x64, single file, framework-dependent). Single-file flags are given on
         the command line, NOT in the .csproj (setting PublishSingleFile in the project can break the
         WinForms designer).
      3. Compiles the Inno Setup installer into dist\
      4. Writes dist\<installer>.sha256 and prints a line to paste into CHANGELOG.md

    Requires the .NET SDK and Inno Setup 6 (https://jrsoftware.org/isinfo.php).
    Paths resolve relative to this script, so it works from any clone.
#>

$ErrorActionPreference = "Stop"

$repoDir    = $PSScriptRoot
$csproj     = Join-Path $repoDir "BingoCaller\BingoCaller.csproj"
$issFile    = Join-Path $repoDir "BingoCaller_INNO.iss"
$publishDir = Join-Path $repoDir "publish_BingoCaller"
$distDir    = Join-Path $repoDir "dist"

# --- Inno Setup compiler ---------------------------------------------------------------------------------
$isccCandidates = @(
    (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"),
    (Join-Path $env:ProgramFiles        "Inno Setup 6\ISCC.exe"),
    (Join-Path $env:LOCALAPPDATA        "Programs\Inno Setup 6\ISCC.exe")
)
$iscc = $isccCandidates | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
if (-not $iscc) { throw "Inno Setup 6 (ISCC.exe) not found. Install it from https://jrsoftware.org/isinfo.php" }

# --- 0. version consistency ------------------------------------------------------------------------------
Write-Host "== 0/4: Checking version consistency ==" -ForegroundColor Cyan
$csprojVersion = ([xml](Get-Content $csproj)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
$issVersion    = [regex]::Match((Get-Content $issFile -Raw), '#define\s+MyAppVersion\s+"([^"]+)"').Groups[1].Value
$changelogHas  = (Get-Content (Join-Path $repoDir "CHANGELOG.md") -Raw) -match [regex]::Escape("## [$csprojVersion]")
if ($csprojVersion -ne $issVersion) { throw "Version mismatch: csproj=$csprojVersion, installer script=$issVersion" }
if (-not $changelogHas)             { throw "CHANGELOG.md has no '## [$csprojVersion]' entry" }
Write-Host "  Version $csprojVersion is consistent"

# --- 1. clean --------------------------------------------------------------------------------------------
Write-Host "== 1/4: Cleaning previous output ==" -ForegroundColor Cyan
foreach ($dir in @($publishDir, $distDir)) {
    if (Test-Path $dir) { Remove-Item -Recurse -Force -Confirm:$false $dir }
}

# --- 2. publish ------------------------------------------------------------------------------------------
Write-Host "== 2/4: dotnet publish (Release, single file) ==" -ForegroundColor Cyan
dotnet publish $csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

# --- 3. installer ----------------------------------------------------------------------------------------
Write-Host "== 3/4: Compiling installer (Inno Setup) ==" -ForegroundColor Cyan
& $iscc $issFile
if ($LASTEXITCODE -ne 0) { throw "ISCC.exe failed with exit code $LASTEXITCODE" }

# --- 4. checksum -----------------------------------------------------------------------------------------
Write-Host "== 4/4: Computing SHA256 checksum ==" -ForegroundColor Cyan
$installer = Get-ChildItem $distDir -Filter "setup_BingoCaller_*.exe" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $installer) { throw "No installer found in $distDir" }

$hash = Get-FileHash $installer.FullName -Algorithm SHA256
"$($hash.Hash)  $($installer.Name)" | Out-File -Encoding ascii "$($installer.FullName).sha256"

Write-Host ""
Write-Host "Release built: $($installer.FullName)" -ForegroundColor Green
Write-Host "Paste into CHANGELOG.md under [$csprojVersion]:"
Write-Host "SHA256 ($($installer.Name)): ``$($hash.Hash)``"
