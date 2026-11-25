@echo off
set TargetFramework=%1
set ProjectName=%2

XCOPY "..\Client\bin\Debug\%TargetFramework%\%ProjectName%.Client.Oqtane.dll" "..\..\oqtane11\Oqtane.Server\bin\Debug\%TargetFramework%\" /Y
XCOPY "..\Client\bin\Debug\%TargetFramework%\%ProjectName%.Client.Oqtane.pdb" "..\..\oqtane11\Oqtane.Server\bin\Debug\%TargetFramework%\" /Y
XCOPY "..\Server\bin\Debug\%TargetFramework%\%ProjectName%.Server.Oqtane.dll" "..\..\oqtane11\Oqtane.Server\bin\Debug\%TargetFramework%\" /Y
XCOPY "..\Server\bin\Debug\%TargetFramework%\%ProjectName%.Server.Oqtane.pdb" "..\..\oqtane11\Oqtane.Server\bin\Debug\%TargetFramework%\" /Y
XCOPY "..\Shared\bin\Debug\%TargetFramework%\%ProjectName%.Shared.Oqtane.dll" "..\..\oqtane11\Oqtane.Server\bin\Debug\%TargetFramework%\" /Y
XCOPY "..\Shared\bin\Debug\%TargetFramework%\%ProjectName%.Shared.Oqtane.pdb" "..\..\oqtane11\Oqtane.Server\bin\Debug\%TargetFramework%\" /Y
XCOPY "..\Server\bin\Debug\%TargetFramework%\Twilio.dll" "..\..\oqtane11\Oqtane.Server\bin\Debug\%TargetFramework%\" /Y
XCOPY "..\Server\bin\Debug\%TargetFramework%\Newtonsoft.Json.dll" "..\..\oqtane11\Oqtane.Server\bin\Debug\%TargetFramework%\" /Y
XCOPY "..\Server\wwwroot\*" "..\..\oqtane11\Oqtane.Server\wwwroot\_content\%ProjectName%\" /Y /S /I