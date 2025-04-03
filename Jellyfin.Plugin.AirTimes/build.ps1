$projectName = "Jellyfin.Plugin.AirTimes"
$solutionPath = Join-Path -Path $PSScriptRoot -ChildPath "..\$projectName.sln"
$outputPath = Join-Path -Path $PSScriptRoot -ChildPath "bin"

# Build

Write-Host "Building the .NET application..."

dotnet publish $solutionPath `
  /p:Configuration=Debug `
  /p:Platform="Any CPU" `
  /p:PublishDir="$outputPath" `
  /property:GenerateFullPaths=true `
  /consoleloggerparameters:NoSummary

if (!$?) {
  Write-Host "Build failed. Please check the build logs for errors."
  exit 1
}

# Package

Write-Host "Build succeeded. Packaging the DLL..."

$dllPath = "$outputPath\$projectName.dll"

New-Item -ItemType Directory -Force -Path "$outputPath\dist"
Copy-Item -Path $dllPath -Destination "$outputPath\dist\$projectName.dll"

$zipPath = "$outputPath\$projectName.zip"
Compress-Archive -Force -Path "$outputPath\dist\*" -DestinationPath $zipPath
$zipChecksum = (Get-FileHash $zipPath -Algorithm MD5).Hash.ToLower()
Remove-Item -Recurse -Force "$outputPath\dist"

Write-Host "Distribution package created successfully."
Write-Host "Path: $zipPath"
Write-Host "Checksum: $zipChecksum"
