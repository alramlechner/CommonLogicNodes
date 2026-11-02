# Builds all logic nodes (unsigned), runs the tests and packages each node into Zip\.
# Signing is deliberately NOT done here (see CLAUDE.md).
param(
    [string[]]$Nodes = @('DewPoint', 'SendMail', 'ModbusClient', 'AVRControl', 'HuaweiModbus', 'InfluxDbNode'),
    [string]$Configuration = 'Release',
    [switch]$Sign
)
$ErrorActionPreference = 'Stop'
$env:Path += ';C:\Program Files\dotnet'
Set-Location $PSScriptRoot

# empty PostBuildEvent: the csproj post-build step would sign with the private certificate
dotnet build LogicNodes.sln -c $Configuration -v minimal '/p:PostBuildEvent=' | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'build failed' }

dotnet vstest "LogicNodesTest\bin\$Configuration\LogicNodesTest.dll" '/TestAdapterPath:packages\NUnit3TestAdapter.3.10.0\build\net35' | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'tests failed' }

New-Item -ItemType Directory -Force Zip | Out-Null
foreach ($n in $Nodes) {
    & .\LogicNodesSDK\LogicNodeTool.exe create "$PWD\$n\bin\$Configuration\ " "$PWD\Zip" | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "packaging $n failed" }
}

if ($Sign) {
    # certificate settings come from the gitignored build.local.psd1 (see build.local.psd1.template)
    $cfgFile = Join-Path $PSScriptRoot 'build.local.psd1'
    if (-not (Test-Path $cfgFile)) { throw 'build.local.psd1 not found; copy build.local.psd1.template and fill it in' }
    $cfg = Import-PowerShellDataFile $cfgFile
    if (-not $cfg.CertPath) { throw 'CertPath missing in build.local.psd1' }
    & .\LogicNodesSDK\SignLogicNodes.exe $cfg.CertPath ([string]$cfg.CertPassword) "$PWD\Zip" | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'signing failed' }
}
