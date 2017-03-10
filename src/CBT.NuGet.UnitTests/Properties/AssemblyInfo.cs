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
[assembly: AssemblyDescription("Unit tests for CBT.NuGet.dll")]
[assembly: AssemblyProduct("Common Build Toolset (CBT)")]
[assembly: AssemblyTitle("CBT.NuGet.UnitTests")]
[assembly: AssemblyTrademark("")]
[assembly: CLSCompliant(false)]
[assembly: ComVisible(false)]
