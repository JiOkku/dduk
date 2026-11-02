param([string]$EditorData = 'C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Data')
$ErrorActionPreference = 'Stop'
$testProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$testDotnetRoot = Join-Path $EditorData 'DotNetSdk'
$testSdk = Get-ChildItem (Join-Path $testDotnetRoot 'sdk') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$testRefPack = Get-ChildItem (Join-Path $testDotnetRoot 'packs/Microsoft.NETCore.App.Ref') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$testRuntime = Get-ChildItem (Join-Path $testDotnetRoot 'shared/Microsoft.NETCore.App') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$testOutput = Join-Path $env:TEMP 'dduk-wave-tests'
New-Item -ItemType Directory -Force -Path $testOutput | Out-Null
$testDll = Join-Path $testOutput 'WaveProgressionTests.dll'
$testArgs = @('/nologo', '/nostdlib+', '/target:exe', '/langversion:9.0', ('/out:"' + $testDll + '"'))
$testArgs += Get-ChildItem (Join-Path $testRefPack.FullName 'ref/net8.0') -Filter '*.dll' | ForEach-Object { '/reference:"' + $_.FullName + '"' }
$testArgs += @('Tests/Standalone/WaveProgressionTests.cs', 'Assets/Scripts/Waves/WaveManager2D.cs', 'Assets/Scripts/Player/PlayerGold2D.cs') | ForEach-Object { '"' + (Join-Path $testProjectRoot $_) + '"' }
$testResponse = Join-Path $testOutput 'compile.rsp'
[System.IO.File]::WriteAllLines($testResponse, $testArgs)
& (Join-Path $testDotnetRoot 'dotnet.exe') (Join-Path $testSdk.FullName 'Roslyn/bincore/csc.dll') /noconfig ('@' + $testResponse)
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
$testConfig = @{ runtimeOptions = @{ tfm = 'net8.0'; framework = @{ name = 'Microsoft.NETCore.App'; version = $testRuntime.Name } } } | ConvertTo-Json -Depth 5
[System.IO.File]::WriteAllText((Join-Path $testOutput 'WaveProgressionTests.runtimeconfig.json'), $testConfig)
& (Join-Path $testDotnetRoot 'dotnet.exe') $testDll
if ($LASTEXITCODE -ne 0) { throw 'Wave tests failed.' }
