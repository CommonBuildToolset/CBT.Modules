' <copyright>%AssemblyInfoCopyright%</copyright>

Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

' The following assembly information is common to all product assemblies.
' If you get compiler errors CS0579, "Duplicate '<attributename>' attribute", check your 
' Properties\AssemblyInfo.vb file and remove any lines duplicating the ones below.
' For AssemblyVersion: we only vary on version major and minor; this is avoid requiring people to update vbproj references for each build
<Assembly: AssemblyVersion("%AssemblyVersion%")> 
<Assembly: AssemblyFileVersion("%AssemblyFileVersion%")> 
' NuGet is using that field for figuring out what version to give to packages based on assemblies.
<Assembly: AssemblyInformationalVersion("%AssemblyInformationalVersion%")>


