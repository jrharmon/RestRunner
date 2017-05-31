$projectPath = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\RestRunner\RestRunner.csproj")
.\InitializeEnvironmentVariables.ps1
$publishDir = $env:publishDir
Write-Host "Publishing to: $publishDir"

$command = "msbuild $projectPath /target:publish /p:Configuration=Release /property:PublishDir=`"$($publishDir)`""
$command
Invoke-Expression -Command:$command

Write-Host "You must now increment the ApplicationRevision in RestRunner.csproj"
pause