# Update .NET workloads
Write-Host "Updating .NET workloads..."
dotnet workload update
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to update .NET workloads."
    exit $LASTEXITCODE
}

# Publish Celestite.Desktop project
Write-Host "Publishing Celestite.Desktop for Windows..."
dotnet publish Celestite.Desktop/Celestite.Desktop.csproj `
    -p:StripSymbols=true `
    -p:Configuration=Release `
    -p:SelfContained=true `
    -p:PublishTrimmed=true `
    -p:PublishAot=true `
    -p:RuntimeIdentifier=win-x64 `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    --output ./Release/Windows

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to publish Celestite.Desktop."
    exit $LASTEXITCODE
}

Write-Host "Publish completed successfully. Output is in ./Release/Windows"