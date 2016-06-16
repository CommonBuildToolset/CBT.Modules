// <copyright>%AssemblyInfoCopyright%</copyright>

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// The following assembly information is common to all product assemblies.
// If you get compiler errors CS0579, "Duplicate '<attributename>' attribute", check your 
// Properties\AssemblyInfo.cs file and remove any lines duplicating the ones below.
// For AssemblyVersion: we only vary on version major and minor; this is avoid requiring people to update csproj references for each build
[assembly: AssemblyVersion("%AssemblyVersion%")]
[assembly: AssemblyFileVersion("%AssemblyFileVersion%")]
// NuGet is using that field for figuring out what version to give to packages based on assemblies.
[assembly: AssemblyInformationalVersion("%AssemblyInformationalVersion%")]
