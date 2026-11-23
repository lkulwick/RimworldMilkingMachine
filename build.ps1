param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$DotnetPath,
    [switch]$NoDeploy,
    [switch]$Rebuild,
    [switch]$Clean
)

$ErrorActionPreference = 'Stop'
if (-not $DotnetPath) {
    $localProps = Join-Path $PSScriptRoot 'Build.local.props'
    if (Test-Path -LiteralPath $localProps) {
        [xml]$settings = Get-Content -LiteralPath $localProps -Raw
        $DotnetPath = [string]$settings.Project.PropertyGroup.DotnetPath
    }
    if (-not $DotnetPath) { $DotnetPath = 'dotnet' }
}

# VS MSBuild exports its own tool locations. Let the SDK host resolve its own tools.
foreach ($variable in @('MSBuildSDKsPath', 'MSBUILD_EXE_PATH', 'MSBuildExtensionsPath')) {
    Remove-Item -LiteralPath "Env:$variable" -ErrorAction SilentlyContinue
}
$verb = 'build'
if ($Clean) { $verb = 'clean' }
$buildArgs = @($verb, (Join-Path $PSScriptRoot '.vscode\mod.Build.csproj'), '-c', $Configuration, '--nologo')
if ($Rebuild) { $buildArgs += '--no-incremental' }
if ($NoDeploy) { $buildArgs += '-p:DeployMod=false' }
& $DotnetPath @buildArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
