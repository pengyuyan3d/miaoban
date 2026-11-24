# Build MiaoBan.exe (ASCII-only script: safe for Windows PowerShell 5.1)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$distDir = Join-Path $root 'dist'
New-Item -ItemType Directory -Force -Path $distDir | Out-Null
$out = Join-Path $distDir 'MiaoBan.exe'

$cscArgs = @(
    '/nologo',
    '/target:winexe',
    '/platform:x64',
    '/optimize+',
    '/unsafe',
    "/out:$out",
    '/r:System.dll',
    '/r:System.Drawing.dll',
    '/r:System.Windows.Forms.dll'
)

$icoPath = Join-Path $root 'art\miaoban.ico'
if (Test-Path $icoPath) {
    $cscArgs += "/win32icon:$icoPath"
}

$manifestPath = Join-Path $root 'src\app.manifest'
if (Test-Path $manifestPath) {
    $cscArgs += "/win32manifest:$manifestPath"
}

$cscArgs += @(
    "$root\src\MiaoBan.cs",
    "$root\src\CatArt.cs",
    "$root\src\Sprite.cs",
    "$root\src\SpritePack.cs",
    "$root\src\MenuForm.cs",
    "$root\src\AssemblyInfo.cs"
)

& $csc @cscArgs
if ($LASTEXITCODE -ne 0) { throw "compile failed: $LASTEXITCODE" }
$item = Get-Item $out
Write-Output ("OK " + $item.FullName + "  " + [math]::Round($item.Length / 1KB, 1) + " KB")
