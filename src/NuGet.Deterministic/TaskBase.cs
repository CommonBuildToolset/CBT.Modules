using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.Packaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace NuGet.Tasks.Deterministic
{
    public abstract class TaskBase : Task
    {
        /// <summary>
        /// Stores a list of assembly search paths where dependencies should be searched for.
        /// </summary>
        protected readonly ICollection<string> AssemblySearchPaths = new List<string>();

        private readonly HashSet<string> _filesWritten = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Stores a list of loaded assemblies in the event that the same assembly is requested multiple times.
        /// </summary>
        private readonly IDictionary<AssemblyName, Assembly> _loadedAssemblies = new Dictionary<AssemblyName, Assembly>();

        protected TaskBase()
        {
            string executingAssemblyLocation = Assembly.GetExecutingAssembly().Location;

            if (!String.IsNullOrWhiteSpace(executingAssemblyLocation))
            {
                // When loading an assembly from a byte[], the Assembly.Location is not set so it shouldn't be considered
                //
                AssemblySearchPaths.Add(Directory.GetParent(executingAssemblyLocation).FullName);
            }

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        [Output]
        public ITaskItem[] FilesWritten => _filesWritten.Select(i => new TaskItem(i)).ToArray();

        protected void AddFileWrite(params string[] paths)
        {
            _filesWritten.AddRange(paths);
        }

        protected virtual Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            AssemblyName assemblyName = new AssemblyName(args.Name);

            // Return the assembly if its already been loaded
            //
            if (_loadedAssemblies.ContainsKey(assemblyName))
            {
                return _loadedAssemblies[assemblyName];
            }

            // Return the first assembly search path that contains the requested assembly
            //
            string assemblyPath = AssemblySearchPaths.Select(i => Path.Combine(i, $"{assemblyName.Name}.dll")).FirstOrDefault(File.Exists);

            if (assemblyPath != null)
            {
                // Load the assembly and keep it in the list of loaded assemblies
                //
                _loadedAssemblies[assemblyName] = Assembly.Load(File.ReadAllBytes(assemblyPath));

                return _loadedAssemblies[assemblyName];
            }

            return null;
        }
    }
}