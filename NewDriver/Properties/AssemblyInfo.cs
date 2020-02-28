using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;

[assembly:AssemblyTitle("NewDriver")]
[assembly:AssemblyProduct("NewDriver")]
[assembly:AssemblyDescription("description of NewDriver.")]
[assembly:AssemblyCompany("Colt Manufacturing Company LLC")]
[assembly:AssemblyCopyright("Copyright © 2020, Colt Manufacturing Company LLC")]
#if DEBUG
[assembly:AssemblyConfiguration("Debug version")]
#else
[assembly:AssemblyConfiguration("Release version")]
#endif
[assembly:ComVisible(false)]

[assembly:AssemblyVersion("1.0.0.0")]


// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: SuppressMessage("Style", "IDE1006:Naming Styles")]