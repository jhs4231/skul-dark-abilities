param([string]$GamePath = 'C:\Program Files (x86)\Steam\steamapps\common\Skul')
$ErrorActionPreference = 'Stop'
$managed = Join-Path $GamePath 'Skul_Data\Managed'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$core = Join-Path $PSScriptRoot 'tools\bepinex\BepInEx\core'
$out = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Force $out | Out-Null
$refs = @('Assembly-CSharp.dll','Assembly-CSharp-firstpass.dll','UnityEngine.dll','UnityEngine.CoreModule.dll','UnityEngine.Physics2DModule.dll','UnityEngine.IMGUIModule.dll','UnityEngine.TextRenderingModule.dll','UnityEngine.ParticleSystemModule.dll','UnityEngine.AnimationModule.dll','Unity.Mathematics.dll','InControl.dll','Plugins.Common.dll','Plugins.Data.dll','Plugins.Singletons.dll') | ForEach-Object { '/reference:' + (Join-Path $(if ($_.StartsWith('UnityEngine.') -or $_ -eq 'UnityEngine.dll') { Join-Path $PSScriptRoot 'tools\unstripped' } else { $managed }) $_) }
$refs += ('/reference:' + (Join-Path $core 'BepInEx.dll'))
$refs += ('/reference:' + (Join-Path $core '0Harmony.dll'))
$refs += ('/reference:' + (Join-Path $PSScriptRoot 'tools\unstripped\UnityEngine.InputLegacyModule.dll'))
$refs += ('/reference:' + (Join-Path $managed 'UnityEngine.UI.dll'))
$refs += ('/reference:' + (Join-Path $managed 'Platforms.dll'))
$refs += ('/reference:' + (Join-Path $managed 'Platforms.Common.dll'))
& $compiler /nologo /target:library /optimize+ ('/out:' + (Join-Path $out 'FanDarkAbilities.dll')) @refs @(Get-ChildItem (Join-Path $PSScriptRoot 'src') -Filter '*.cs' | ForEach-Object FullName)
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed' }

