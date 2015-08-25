using System;
using System.IO;

namespace ReallyStopDebugger.Common
{
    public static class FileUtils
    {
        public static void AttemptHardClean(EnvDTE.DTE dte)
        {
            try
            {
                string solutionDirectory = Path.GetDirectoryName(dte.Solution.FullName);

                var binTargets = Directory.GetDirectories(solutionDirectory, "bin", SearchOption.AllDirectories);
                var objTargets = Directory.GetDirectories(solutionDirectory, "obj", SearchOption.AllDirectories);

                foreach (string target in binTargets)
                {
                    ForceDeleteDirectory(target, "bin");
                }

                foreach (string target in objTargets)
                {
                    ForceDeleteDirectory(target, "obj");
                }
            }
            catch (UnauthorizedAccessException) { }
        }

        public static void ForceDeleteDirectory(string path, string match)
        {
            var directory = new DirectoryInfo(path) { Attributes = FileAttributes.Normal };

            foreach (var info in directory.GetFileSystemInfos("*", SearchOption.AllDirectories))
            {
                info.Attributes = FileAttributes.Normal;
            }

            if (Path.GetFileName(path).Equals(match, System.StringComparison.InvariantCultureIgnoreCase))
            {
                directory.Delete(true);
            }
        }
    }
}
