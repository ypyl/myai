$csprojFile = "myai.csproj"
[xml]$xml = Get-Content $csprojFile
$versionNode = $xml.Project.PropertyGroup.Version
$version = $versionNode.'#text'
$versionParts = $version -split '\.'
$versionParts[2] = [int]$versionParts[2] + 1
$newVersion = "$($versionParts[0]).$($versionParts[1]).$($versionParts[2])"
$versionNode.'#text' = $newVersion
$xml.Save($csprojFile)
