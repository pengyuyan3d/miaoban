# Build MiaoBan.exe  (ASCII-only script: safe for Windows PowerShell 5.1)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$out = Join-Path $root 'dist\MiaoBan.exe'

& $csc /nologo /target:winexe /platform:x64 /optimize+ /unsafe `
    /win32icon:"$root\art\miaoban.ico" /win32manifest:"$root\src\app.manifest" `
    /out:"$out" `
    /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll `
    "$root\src\MiaoBan.cs" "$root\src\CatArt.cs" "$root\src\Sprite.cs" "$root\src\SpritePack.cs" "$root\src\MenuForm.cs" "$root\src\AssemblyInfo.cs"

if ($LASTEXITCODE -ne 0) { throw "compile failed: $LASTEXITCODE" }
$item = Get-Item $out
Write-Output ("OK " + $item.FullName + "  " + [math]::Round($item.Length / 1KB, 1) + " KB")



