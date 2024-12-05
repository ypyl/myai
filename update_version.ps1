$csprojFile = "myai.csproj"
[xml]$xml = Get-Content $csprojFile
$versionNode = $xml.Project.PropertyGroup.Version
if ($versionNode -eq $null) {
    Write-Error "Version node not found in the project file"
    exit 1
}
$version = $versionNode
if ($version -eq $null -or $version -eq "") {
    Write-Error "Version text not found in the version node."
    exit 1
}
$versionParts = $version -split '\.'
if ($versionParts.Length -lt 3) {
    Write-Error "Version format is incorrect. Expected format: major.minor.patch"
    exit 1
}

$versionParts[2] = [int]$versionParts[2] + 1

$newVersion = "$($versionParts[0]).$($versionParts[1]).$($versionParts[2])"
$xml.Project.PropertyGroup.Version = $newVersion
$xml.Save($csprojFile)
exit 0
