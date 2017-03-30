@ECHO OFF
SETLOCAL ENABLEDELAYEDEXPANSION

SET "NETFXTAG=-beta02"
IF "%ProgramFiles(x86)%" NEQ "" (
	SET "REFERENCEASSEMBLYROOT=%ProgramFiles(x86)%\Reference Assemblies"
) ELSE (
	SET "REFERENCEASSEMBLYROOT=%ProgramFiles%\Reference Assemblies"
)

CALL :Net35

FOR %%A IN (4.0.0,4.5.0,4.5.1,4.5.2,4.6.0,4.6.1,4.6.2) DO (
	SET DisplayName=net%%A
	SET DisplayName=!DisplayName:.=!
	IF "!DisplayName:~5,1!" EQU "0" (
		SET DisplayName=!DisplayName:~0,5!
	)
	SET DisplayVersion=v%%A
	IF "!DisplayVersion:~5,1!" EQU "0" (
		SET DisplayVersion=!DisplayVersion:~0,4!
	)
	
	SET "ROOT=%REFERENCEASSEMBLYROOT%\Microsoft\Framework\.NETFramework\!DisplayVersion!"

	IF NOT EXIST "!ROOT!" (
		ECHO .NET Framework reference assemblies were not found at !ROOT!.  Ensure the .NET Framework SDK is installed for this version and try again.
	) ELSE (
		ECHO Version %%A%NETFXTAG% - !DisplayName! - !DisplayVersion! - "!ROOT!"
		NuGet PACK "%~dp0CBT.DotNetFx.nuspec" -Properties "Version=%%A%NETFXTAG%;DisplayName=!DisplayName!;DisplayVersion=!DisplayVersion!;Root=!ROOT!" -NoPackageAnalysis -NoDefaultExcludes -O "%~dp0.."
	)
)

GOTO :EOF

:Net35
SET "ROOT=%REFERENCEASSEMBLYROOT%\Microsoft\Framework"
NuGet PACK "%~dp0CBT.DotNetFx.3.5.nuspec" -Properties "Version=3.5.0%NETFXTAG%;DisplayName=net35;DisplayVersion=v3.5;Root=%ROOT%" -NoPackageAnalysis -NoDefaultExcludes -O "%~dp0.."
IF ERRORLEVEL 1 EXIT /B 1