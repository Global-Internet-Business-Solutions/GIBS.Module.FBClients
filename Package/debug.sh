#!/bin/bash

TargetFramework=$1
ProjectName=$2

cp -f "../Client/bin/Debug/$TargetFramework/$ProjectName$.Client.Oqtane.dll" "../../oqtane11/Oqtane.Server/bin/Debug/$TargetFramework/"
cp -f "../Client/bin/Debug/$TargetFramework/$ProjectName$.Client.Oqtane.pdb" "../../oqtane11/Oqtane.Server/bin/Debug/$TargetFramework/"
cp -f "../Server/bin/Debug/$TargetFramework/$ProjectName$.Server.Oqtane.dll" "../../oqtane11/Oqtane.Server/bin/Debug/$TargetFramework/"
cp -f "../Server/bin/Debug/$TargetFramework/$ProjectName$.Server.Oqtane.pdb" "../../oqtane11/Oqtane.Server/bin/Debug/$TargetFramework/"
cp -f "../Shared/bin/Debug/$TargetFramework/$ProjectName$.Shared.Oqtane.dll" "../../oqtane11/Oqtane.Server/bin/Debug/$TargetFramework/"
cp -f "../Shared/bin/Debug/$TargetFramework/$ProjectName$.Shared.Oqtane.pdb" "../../oqtane11/Oqtane.Server/bin/Debug/$TargetFramework/"
cp -f "../Server/bin/Debug/$TargetFramework/Twilio.dll" "../../oqtane11/Oqtane.Server/bin/Debug/$TargetFramework/"
cp -f "../Server/bin/Debug/$TargetFramework/Newtonsoft.Json.dll" "../../oqtane11/Oqtane.Server/bin/Debug/$TargetFramework/"
cp -rf "../Server/wwwroot/"* "../../oqtane11/Oqtane.Server/wwwroot/_content/%ProjectName%/"
