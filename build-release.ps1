param([string]$Version = '0.1.0')

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath($PSScriptRoot)
$project = Join-Path $repositoryRoot 'NppTranslatePanel\NppTranslatePanel.csproj'
$artifactRoot = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot ("artifacts\v" + $Version)))

if (-not $artifactRoot.StartsWith($repositoryRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'Artifact path escaped the repository root.'
}

$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (-not (Test-Path -LiteralPath $vswhere)) {
    throw 'Visual Studio Installer (vswhere.exe) was not found.'
}
$msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (-not $msbuild) {
    throw 'MSBuild was not found.'
}

if (Test-Path -LiteralPath $artifactRoot) {
    Remove-Item -LiteralPath $artifactRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $artifactRoot | Out-Null

foreach ($architecture in @('x64', 'x86')) {
    $runtime = 'win-' + $architecture
    $stageRoot = Join-Path $artifactRoot ('stage-' + $architecture)
    $pluginDirectory = Join-Path $stageRoot 'NppTranslatePanel'
    $arguments = @(
        $project, '/t:Restore,Build', '/p:Configuration=Release',
        ('/p:Platform=' + $architecture), ('/p:RuntimeIdentifier=' + $runtime),
        ('/p:NppPluginsDir64=' + $stageRoot), ('/p:NppPluginsDir32=' + $stageRoot)
    )
    & $msbuild @arguments
    if ($LASTEXITCODE -ne 0) { throw "Release build failed for $architecture." }

    $dll = Join-Path $pluginDirectory 'NppTranslatePanel.dll'
    if (-not (Test-Path -LiteralPath $dll)) { throw "Release DLL was not produced for $architecture." }
    $packageDirectory = Join-Path $artifactRoot ('package-' + $architecture)
    New-Item -ItemType Directory -Path $packageDirectory | Out-Null
    Copy-Item -LiteralPath $dll -Destination (Join-Path $packageDirectory 'NppTranslatePanel.dll')
    $zip = Join-Path $artifactRoot ("NppTranslatePanel-$Version-$architecture.zip")
    Compress-Archive -Path (Join-Path $packageDirectory '*') -DestinationPath $zip -CompressionLevel Optimal
}

Get-ChildItem -LiteralPath $artifactRoot -Filter '*.zip' | ForEach-Object {
    $hash = Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256
    [PSCustomObject]@{ Package = $_.Name; SHA256 = $hash.Hash.ToLowerInvariant() }
} | Format-Table -AutoSize
