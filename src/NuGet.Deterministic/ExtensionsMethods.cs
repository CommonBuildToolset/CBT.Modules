using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.LibraryModel;
using NuGet.Packaging.Core;
using NuGet.ProjectModel;
using NuGet.Versioning;

namespace NuGet.Tasks.Deterministic
{
    public static class ExtensionsMethods
    {
        public static void LogErrorFromText(this TaskLoggingHelper log, string code, string message, params object[] args)
        {
            log.LogError(
                subcategory: null, 
                errorCode: code,
                helpKeyword: null, 
                file: null,
                lineNumber: 0,
                columnNumber: 0,
                endLineNumber: 0,
                endColumnNumber: 0,
                message: message,
                messageArgs: args
            );
        }

        public static void LogWarningFromText(this TaskLoggingHelper log, string code, string message, params object[] args)
        {
            log.LogWarning(
                subcategory: null,
                warningCode: code,
                helpKeyword: null,
                file: null,
                lineNumber: 0,
                columnNumber: 0,
                endLineNumber: 0,
                endColumnNumber: 0,
                message: message,
                messageArgs: args
            );
        }

        public static string GetMetadataNoThrow(this ITaskItem taskItem, string metadataName)
        {
            try
            {
                return taskItem.GetMetadata(metadataName);
            }
            catch (KeyNotFoundException)
            {
                return default(string);
            }
        }

        public static LockFileLibrary GetLibrary(this LockFile lockFile, LibraryDependency libraryDependency)
        {
            return lockFile.GetLibrary(libraryDependency.Name, libraryDependency.LibraryRange.VersionRange.MinVersion);
        }
        

        public static IEnumerable<LockFileLibrary> ResolveDependencies(this LockFileTargetLibrary targetLibrary, LockFile lockFile, LockFileTarget lockFileTarget)
        {
            foreach (PackageDependency dependency in targetLibrary.Dependencies)
            {
                LockFileTargetLibrary dependencyTargetLibrary = lockFileTarget.GetTargetLibrary(dependency.Id);

                yield return lockFile.GetLibrary(dependencyTargetLibrary.Name, dependencyTargetLibrary.Version);

                foreach (LockFileLibrary lockFileLibrary in dependencyTargetLibrary.ResolveDependencies(lockFile, lockFileTarget))
                {
                    yield return lockFileLibrary;
                }
            }
        }
    }
}