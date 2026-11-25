#!/bin/bash
set -e

cd "$(dirname "$0")"
TargetFramework=$1
ProjectName=$2
Destination="../../oqtane11/Oqtane.Server/Packages"

if [ ! -d "$Destination" ]; then
    echo "Oqtane package destination does not exist: $Destination" >&2
    exit 1
fi

mono ../../oqtane11/Oqtane.Package/nuget.exe pack "$ProjectName.nuspec" -Properties "targetframework=$TargetFramework;projectname=$ProjectName" -OutputDirectory "$Destination" -NonInteractive