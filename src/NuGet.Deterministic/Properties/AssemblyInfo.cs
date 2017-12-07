using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyCompany("Common Build Toolset (CBT)")]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
[assembly: AssemblyCopyright("Copyright © 2016")]
[assembly: AssemblyDescription("MSBuild task library for generating .props with nuget versions.")]
[assembly: AssemblyProduct("Common Build Toolset (CBT)")]
[assembly: AssemblyTitle("CBT.NuGet.Deterministic")]
[assembly: AssemblyTrademark("")]
[assembly: CLSCompliant(false)]
[assembly: ComVisible(false)]

[assembly: InternalsVisibleTo("NuGet.Deterministic.UnitTests")]