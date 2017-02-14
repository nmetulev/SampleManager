@echo off

@echo.
@echo *******************************************
@echo * RESTORING NUGET  			*
@echo *******************************************
@echo.
tools\nuget.exe restore SampleManager.sln -NonInteractive

@echo.
@echo *******************************************
@echo * BUILD STARTING   			*
@echo *******************************************
@echo.
call "c:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\common7\Tools\vsmsbuildcmd.bat"
cd SampleManager
msbuild /verbosity:quiet /fl /t:Rebuild /p:Configuration=Release,OutputPath=bin\Release\NuGet\ /property:GenerateLibraryLayout=false /p:NoWarn=0618 SampleManager\SampleManager.csproj

@echo.
@echo *******************************************
@echo * COPYING BINARIES                        *
@echo *******************************************
@echo.

pushd tools

mkdir .\lib\uap10.0
mkdir .\lib\uap10.0\SampeManager
mkdir .\lib\uap10.0\SampleManager\Themes
mkdir .\lib\uap10.0\SampleManager\Properties
copy ..\SampleManager\bin\release\NuGet\SampleManager.dll .\lib\uap10.0\
copy ..\SampleManager\bin\release\NuGet\SampleManager.pri .\lib\uap10.0\
copy ..\SampleManager\bin\release\NuGet\SampleManager.xr.xml .\lib\uap10.0\SampleManager\
copy ..\SampleManager\bin\release\NuGet\SampleDescription.xbf .\lib\uap10.0\SampleManager\Themes
copy ..\SampleManager\Properties\SampleManager.rd.xml .\lib\uap10.0\SampleManager\Properties

@echo.
@echo *******************************************
@echo * BUILDING NUGET 				*
@echo *******************************************
@echo.

mkdir .\package
nuget pack SampleManager.nuspec -o .\package

@echo.
@echo *******************************************
@echo * DONE 		 			*
@echo *******************************************
@echo.

explorer .\package

popd