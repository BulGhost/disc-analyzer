using System;
using System.Collections.Generic;
using System.IO;
using DiscAnalyzerModel.Resourses;
using Microsoft.Extensions.Logging;

namespace DiscAnalyzerModel.HelperClasses
{
    internal class DirectoryStructure
    {
        private static ILogger _logger;
        private readonly string _fullPath;

        internal DirectoryStructure(string fullPath, ILogger logger)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                throw new ArgumentException(Resources.IncorrectFilePath, nameof(fullPath));
            }

            _fullPath = fullPath;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        internal List<string> GetDirectoryContents()
        {
            var items = new List<string>();
            var options = new EnumerationOptions { IgnoreInaccessible = true, AttributesToSkip = 0 };
            try
            {
                string[] dirs = Directory.GetDirectories(_fullPath, "*", options);
                if (dirs.Length > 0)
                {
                    items.AddRange(dirs);
                }

                string[] files = Directory.GetFiles(_fullPath, "*", options);
                if (files.Length > 0)
                {
                    items.AddRange(files);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during try to get directories and files inside {0}", _fullPath);
                throw;
            }

            return items;
        }
    }
}
