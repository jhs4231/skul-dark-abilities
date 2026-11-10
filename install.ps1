param([string]$GamePath = 'C:\Program Files (x86)\Steam\steamapps\common\Skul')
$ErrorActionPreference = 'Stop'
if (Get-Process Skul -ErrorAction SilentlyContinue) { throw 'Close Skul before installation.' }
if (!(Test-Path -LiteralPath (Join-Path $GamePath 'Skul.exe'))) { throw 'Skul.exe missing' }
$loader = Join-Path $PSScriptRoot 'tools\bepinex'
$libraries = Join-Path $PSScriptRoot 'tools\unstripped'
foreach ($file in @('mscorlib.dll', 'UnityEngine.CoreModule.dll')) {
    if (!(Test-Path -LiteralPath (Join-Path $libraries $file))) { throw "Missing runtime library: $file" }
}
foreach ($name in @('winhttp.dll','doorstop_config.ini','BepInEx','FanDarkAbilities_Runtime')) {
    if (Test-Path -LiteralPath (Join-Path $GamePath $name)) { throw "Existing installation detected: $name. No files copied." }
}
Copy-Item -LiteralPath (Join-Path $loader 'BepInEx') -Destination $GamePath -Recurse
Copy-Item -LiteralPath (Join-Path $loader 'winhttp.dll') -Destination $GamePath
Copy-Item -LiteralPath $libraries -Destination (Join-Path $GamePath 'FanDarkAbilities_Runtime') -Recurse
$configuration = Get-Content -LiteralPath (Join-Path $loader 'doorstop_config.ini') -Raw
$configuration = $configuration.Replace('dll_search_path_override =', 'dll_search_path_override = FanDarkAbilities_Runtime')
Set-Content -LiteralPath (Join-Path $GamePath 'doorstop_config.ini') -Value $configuration -Encoding ASCII
$plugins = Join-Path $GamePath 'BepInEx\plugins\FanDarkAbilities'
New-Item -ItemType Directory -Force $plugins | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'dist\FanDarkAbilities.dll') -Destination $plugins
Write-Output 'Installed prototype and BepInEx; original game files were not replaced.'
