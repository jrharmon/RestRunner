$projectPath = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\RestRunner\RestRunner.csproj")
$releaseBinDir = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\RestRunner\bin\Release")
.\InitializeEnvironmentVariables.ps1
$publishDir = $env:publishDir
$publishPortableDir = $env:publishPortableDir

Write-Host "Publishing to: $publishDir"
$command = "msbuild $projectPath /target:publish /p:Configuration=Release /property:PublishDir=`"$($publishDir)`""
$command
Invoke-Expression -Command:$command

Write-Host "Publishing to: $publishPortableDir"
Get-ChildItem $releaseBinDir -File | Copy-Item -Destination $publishPortableDir -Force

Write-Host "You must now increment the ApplicationRevision in RestRunner.csproj"
pause