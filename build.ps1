# 用 .NET Framework 自带的 csc 编译 CursorStudio，无需 Visual Studio。
# 用法： powershell -ExecutionPolicy Bypass -File .\build.ps1

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

$candidates = @(
    'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe',
    'C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe'
)
$csc = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $csc) { throw '找不到 .NET Framework 的 csc.exe（需要 .NET Framework 4.x）' }

$dist = Join-Path $root 'dist'
New-Item -ItemType Directory -Path $dist -Force | Out-Null

# 先清理旧进程，避免 exe 被占用导致 CS0016
Get-Process -Name CursorStudio -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

$src1 = Join-Path $root 'src\CursorCore.cs'
$src2 = Join-Path $root 'src\CursorUI.cs'
$manifest = Join-Path $root 'src\CursorStudio.manifest'
$icon = Join-Path $root 'assets\icon.ico'
$out = Join-Path $dist 'CursorStudio.exe'

& $csc /nologo /codepage:65001 /target:winexe /platform:anycpu `
    ('/win32manifest:' + $manifest) `
    ('/win32icon:' + $icon) `
    ('/out:' + $out) `
    /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll `
    $src1 $src2

if ($LASTEXITCODE -ne 0) { throw ('csc 退出码 ' + $LASTEXITCODE) }

Copy-Item -LiteralPath $icon -Destination (Join-Path $dist 'CursorStudio.ico') -Force
Write-Host ('已生成 ' + $out)
