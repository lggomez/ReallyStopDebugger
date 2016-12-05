// Copyright (c) Luis Gómez. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the src\ReallyStopDebugger directory for full license information.

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
                var solutionDirectory = Path.GetDirectoryName(dte.Solution.FullName);

                var binTargets = Directory.GetDirectories(solutionDirectory, "bin", SearchOption.AllDirectories);
                var objTargets = Directory.GetDirectories(solutionDirectory, "obj", SearchOption.AllDirectories);

                foreach (var target in binTargets)
                {
                    ForceDeleteDirectory(target, "bin");
                }

                foreach (var target in objTargets)
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

            if (Path.GetFileName(path).Equals(match, StringComparison.InvariantCultureIgnoreCase))
            {
                directory.Delete(true);
            }
        }
    }
}
