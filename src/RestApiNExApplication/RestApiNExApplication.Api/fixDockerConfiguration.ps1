#Script to process Docker solution settings
#this is temporary solution for docker-compose issue for COPY command
#

$currLocation = Get-Location
$currLocationSolution = "$currLocation\..\.." | Convert-Path
$currLocationDockerFolder = "$currLocation\.." | Convert-Path
$currLocationDockerFolder = Join-Path $currLocationDockerFolder "\docker-compose"


Copy-Item -Path $currLocationDockerFolder\*.* -Destination $currLocationSolution
Remove-Item $currLocationDockerFolder -Confirm:$false -Force

#replace docker-compose project path in vs
$currLocationSolutionFile =  Join-Path $currLocationSolution "RestApiNExApplication.sln"
$content = Get-Content -Path $currLocationSolutionFile
$newContent = $content.Replace("RestApiNExApplication\docker-compose\docker-compose.dcproj", "docker-compose.dcproj")
$newContent | Set-Content -Path $currLocationSolutionFile


#$filePath = 'C:\file.txt'
#$tempFilePath = "$env:TEMP\$($filePath | Split-Path -Leaf)"
#$find = 'foo'
#$replace = 'bar'

#(Get-Content -Path $filePath) -replace $find, $replace | Add-Content -Path $tempFilePath

#Remove-Item -Path $filePath
#Move-Item -Path $tempFilePath -Destination $filePath