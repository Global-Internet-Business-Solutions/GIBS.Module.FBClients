@echo off
setlocal
set "TargetFramework=%~1"
set "ProjectName=%~2"
pushd "%~dp0" || exit /b 1

if not exist "..\..\oqtane11\Oqtane.Server\Packages\" (
  echo Oqtane package destination does not exist: ..\..\oqtane11\Oqtane.Server\Packages >&2
  popd
  exit /b 1
)
"..\..\oqtane11\Oqtane.Package\nuget.exe" pack "%ProjectName%.nuspec" -Properties "targetframework=%TargetFramework%;projectname=%ProjectName%" -OutputDirectory "..\..\oqtane11\Oqtane.Server\Packages" -NonInteractive
set "PackExitCode=%ERRORLEVEL%"
popd
exit /b %PackExitCode%