@ECHO OFF
SETLOCAL ENABLEDELAYEDEXPANSION

SET "NETFXTAG=-beta1"
IF "%ProgramFiles(x86)%" NEQ "" (
	SET "REFERENCEASSEMBLYROOT=%ProgramFiles(x86)%\Reference Assemblies"
) ELSE (
	SET "REFERENCEASSEMBLYROOT=%ProgramFiles%\Reference Assemblies"
)

FOR %%A IN (4.0.0,4.5.0,4.5.1,4.5.2,4.6.0,4.6.1) DO (
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
		NuGet PACK "%~dp0CBT.DotNetFx.nuspec" -Properties "Version=%%A%NETFXTAG%;DisplayName=!DisplayName!;Root=!ROOT!" -NoPackageAnalysis -NoDefaultExcludes -O "%~dp0.."
	)
)