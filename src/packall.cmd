@ECHO OFF
FOR /F "usebackq" %%A IN (`dir %~dp0*.nuspec /s /b`) DO (
	"NuGet.exe" pack "%%A" -OutputDirectory %~dp0
)