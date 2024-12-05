
csprojFile="myai.csproj"
versionNode=$(xmllint --xpath "string(//Project/PropertyGroup/Version)" "$csprojFile")

if [ -z "$versionNode" ]; then
    echo "Version node not found in the project file" >&2
    exit 1
fi

version="$versionNode"
if [ -z "$version" ]; then
    echo "Version text not found in the version node." >&2
    exit 1
fi

IFS='.' read -r -a versionParts <<< "$version"
if [ ${#versionParts[@]} -lt 3 ]; then
    echo "Version format is incorrect. Expected format: major.minor.patch" >&2
    exit 1
fi

versionParts[2]=$((versionParts[2] + 1))

newVersion="${versionParts[0]}.${versionParts[1]}.${versionParts[2]}"
xmlstarlet ed -L -u "//Project/PropertyGroup/Version" -v "$newVersion" "$csprojFile"
exit 0