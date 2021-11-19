using System;
using System.Threading;
using System.Threading.Tasks;
using DiscAnalyzerModel.Enums;
using DiscAnalyzerModel.Resourses;
using Microsoft.Extensions.Logging;

namespace DiscAnalyzerModel
{
    public class FileSystemItemFactory
    {
        public (Task task, FileSystemItem resultItem) CreateNewAsync(string fullPath,
            ItemBaseProperty basePropertyForPercentOfParentCalculation, ILogger logger, CancellationToken token)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                throw new ArgumentException(Resources.IncorrectFilePath, nameof(fullPath));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            return FileSystemItem.CreateItemAsync(fullPath, basePropertyForPercentOfParentCalculation,
                logger, token);
        }
    }
}
