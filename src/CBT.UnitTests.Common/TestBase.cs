using System;
using System.Collections.Generic;
using System.IO;

namespace CBT.UnitTests.Common
{
    public abstract class TestBase : IDisposable
    {
        private readonly string _testRootPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        public string TestRootPath
        {
            get
            {
                Directory.CreateDirectory(_testRootPath);
                return _testRootPath;
            }
        }

        public string CreateDirectory(string name, IDictionary<string, string> files = null)
        {
            DirectoryInfo directory = Directory.CreateDirectory(Path.Combine(TestRootPath, name));

            if (files != null)
            {
                foreach (KeyValuePair<string, string> file in files)
                {
                    FileInfo fileInfo = new FileInfo(Path.Combine(directory.FullName, file.Key));

                    fileInfo.Directory.Create();

                    File.WriteAllText(fileInfo.FullName, file.Value);
                }
            }

            return directory.FullName;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Directory.Exists(TestRootPath))
                {
                    Directory.Delete(TestRootPath, recursive: true);
                }
            }
        }

        protected string GetTempFileName()
        {
            Directory.CreateDirectory(TestRootPath);

            return Path.Combine(TestRootPath, Path.GetRandomFileName());
        }
    }
}