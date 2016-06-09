@ECHO OFF
FOR /F "usebackq" %%A IN (`dir %~dp0*.nuspec /s /b ^| findstr /v /i "CBT.DotNetFx"`) DO (
	"NuGet.exe" pack "%%A" -OutputDirectory %~dp0
)