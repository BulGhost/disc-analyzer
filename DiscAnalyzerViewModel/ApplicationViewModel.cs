using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Aga.Controls.Tree;
using AsyncAwaitBestPractices.MVVM;
using DiscAnalyzerModel;
using DiscAnalyzerModel.Enums;
using DiscAnalyzerModel.HelperClasses;
using DiscAnalyzerViewModel.Commands;
using DiscAnalyzerViewModel.Enums;
using DiscAnalyzerViewModel.HelperClasses;
using DiscAnalyzerViewModel.Resourses;
using Microsoft.Extensions.Logging;
using Ookii.Dialogs.Wpf;

namespace DiscAnalyzerViewModel
{
    public class ApplicationViewModel : INotifyPropertyChanged, ITreeModel
    {
        public event PropertyChangedEventHandler PropertyChanged;

        #region Fields

        private readonly ILogger _logger;
        private FileSystemItem _rootItem;
        private IAsyncCommand _openDialogCommand;
        private IAsyncCommand<string> _driveScanCommand;
        private RelayCommand _stopCommand;
        private IAsyncCommand _refreshCommand;
        private bool _readyToScan;
        private IAsyncCommand<ExpandLevel> _expandCommand;
        private Task _directoryAnalysis;
        private ItemBaseProperty _mode;
        private string _percentOfParentColumnName;

        #endregion

        #region Properties

        public TreeList TreeList { get; }

        public ItemBaseProperty Mode
        {
            get => _mode;
            set
            {
                if (_mode == value)
                {
                    return;
                }

                _mode = value;
                if (_mode == ItemBaseProperty.PercentOfParent)
                {
                    return;
                }

                FileSystemItem.BasePropertyForPercentOfParentCalculation = _mode;
            }
        }

        public string PercentOfParentColumnName
        {
            get
            {
                if (_mode == ItemBaseProperty.PercentOfParent)
                {
                    return _percentOfParentColumnName;
                }

                _percentOfParentColumnName = string.Format(Resources.PercentOfParentColumnName, _mode);
                return _percentOfParentColumnName;
            }
        }

        public Unit SizeUnit { get; set; }
        public CancellationTokenSource Source { get; set; }
        public bool AnalysisInProgress { get; set; }

        public bool ReadyToScan
        {
            get => _readyToScan;
            set
            {
                if (_readyToScan == value)
                {
                    return;
                }

                _readyToScan = value;
                RefreshCommand.RaiseCanExecuteChanged();
                ExpandCommand.RaiseCanExecuteChanged();
            }
        }

        public bool CanRefresh => _rootItem != null && _readyToScan;
        public string DiscFreeSpaceInfo { get; set; }
        public string FilesCountInfo { get; set; }
        public string ClusterSizeInfo { get; set; }

        #endregion

        public ApplicationViewModel(TreeList treeList, ILogger logger)
        {
            _logger = logger;
            TreeList = treeList;
            ReadyToScan = true;
        }

        #region ITreeModel members

        public IEnumerable GetChildren(object parent)
        {
            return parent == null
                ? new ObservableCollection<FileSystemItem> {_rootItem}
                : (parent as FileSystemItem)?.Children;
        }

        public bool HasChildren(object parent)
        {
            return parent is FileSystemItem item && item.Children != null;
        }

        #endregion

        #region Commands

        public IAsyncCommand OpenDialogCommand =>
            _openDialogCommand ??= new AsyncCommand(async () =>
                {
                    var dlg = new VistaFolderBrowserDialog();
                    if (dlg.ShowDialog() == true)
                    {
                        await RunDirectoryScanning(dlg.SelectedPath);
                    }
                },
                _ => ReadyToScan);

        public IAsyncCommand<string> DriveScanCommand =>
            _driveScanCommand ??= new AsyncCommand<string>(async path =>
                    await RunDirectoryScanning(path),
                _ => ReadyToScan);

        public RelayCommand StopCommand =>
            _stopCommand ??= new RelayCommand(() =>
                {
                    _logger.LogInformation("Stop analysis of {0} directory", _rootItem.FullPath);
                    Source?.Cancel();
                    AnalysisInProgress = false;
                },
                () => AnalysisInProgress);

        public IAsyncCommand RefreshCommand =>
            _refreshCommand ??= new AsyncCommand(async () =>
                    await RunDirectoryScanning(_rootItem.FullPath),
                _ => CanRefresh);

        public RelayCommand ExitCommand =>
            new(() =>
            {
                _logger.LogInformation("Shutdown application");
                Application.Current.Shutdown();
            });

        public IAsyncCommand<ExpandLevel> ExpandCommand =>
            _expandCommand ??= new AsyncCommand<ExpandLevel>(async level =>
                {
                    _logger.LogInformation("Expand nodes to level {0}", level);
                    await ExpandNodesAsync(level);
                },
                _ => ReadyToScan);

        #endregion

        private Task RunDirectoryScanning(string fullPath)
        {
            ReadyToScan = false;
            _logger.LogInformation("Refresh analysis of {0} directory", fullPath);
            Source?.Cancel();
            return AnalyzeDirectory(fullPath);
        }

        private async Task AnalyzeDirectory(string directoryPath)
        {
            TreeList.Model ??= this;
            UpdateStatusBarInfo(directoryPath);
            if (Source != null)
            {
                await CleanUpTreeList();
            }

            Source = new CancellationTokenSource();
            (_directoryAnalysis, _rootItem) = FileSystemItem.CreateItemAsync(directoryPath,
                Mode, _logger, Source.Token);
            AnalysisInProgress = true;
            ReadyToScan = true;
            TreeList.UpdateNodes();
            if (TreeList.Nodes.Count != 0)
            {
                TreeList.Nodes[0].IsExpanded = true;
            }

            try
            {
                await _directoryAnalysis;
                FilesCountInfo = string.Format(Resources.FilesCountInfo, _rootItem.Files);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Directory analysis is stopped");
            }

            AnalysisInProgress = false;
            Source = null;
            _logger.LogInformation("{0} directory analysis completed", directoryPath);
        }

        private async Task CleanUpTreeList()
        {
            try
            {
                ReadyToScan = false;
                await _directoryAnalysis;
            }
            catch (OperationCanceledException)
            {
                _rootItem = null;
                TreeList.UpdateNodes();
                _logger.LogInformation("TreeList cleaned up after refresh");
            }
        }

        private void UpdateStatusBarInfo(string path)
        {
            _logger.LogInformation("Status bar updating started");
            DriveInfo info;
            try
            {
                info = new DriveInfo(path[0].ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fail to get DriveInfo on path: {}", path);
                throw;
            }

            long freeSpaceInBytes = info.AvailableFreeSpace;
            string freeSpace = BytesConverter.ConvertAutomatically(freeSpaceInBytes);
            long totalSpaceInBytes = info.TotalSize;
            string totalSpace = BytesConverter.ConvertAutomatically(totalSpaceInBytes);

            DiscFreeSpaceInfo = string.Format(Resources.DiscFreeSpaceInfo, freeSpace, totalSpace);
            uint clusterSize = FileSizeOnDiskDeterminator.DetermineClusterSize(info.Name);
            ClusterSizeInfo = string.Format(Resources.ClusterSizeInfo, clusterSize, info.DriveFormat);
        }

        private async Task ExpandNodesAsync(ExpandLevel level)
        {
            _logger.LogInformation("Nodes expanding to level {0} started", level);
            var nodes = TreeList.Nodes;

            try
            {
                if (level == ExpandLevel.FullExpand)
                {
                    await ExpandAllNodesAsync(nodes);
                }
                else
                {
                    await ExpandNodesUpToLevelAsync(nodes, (int)level + 1);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Expand nodes failure");
                throw;
            }
        }

        private async Task ExpandAllNodesAsync(ICollection<TreeNode> nodes)
        {
            if (nodes == null || nodes.Count == 0)
            {
                return;
            }

            await Task.Run(async () =>
            {
                foreach (TreeNode node in nodes)
                {
                    await TreeList.Dispatcher.InvokeAsync(() => node.IsExpanded = true);
                    await ExpandAllNodesAsync(node.Nodes);
                }
            });
        }

        private async Task ExpandNodesUpToLevelAsync(ICollection<TreeNode> nodes, int level)
        {
            if (level < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            if (nodes == null || nodes.Count == 0)
            {
                return;
            }

            if (level == 0)
            {
                foreach (TreeNode node in nodes)
                    await TreeList.Dispatcher.InvokeAsync(() => node.IsExpanded = false);
                return;
            }

            await Task.Run(async () =>
            {
                foreach (TreeNode node in nodes)
                {
                    await TreeList.Dispatcher.InvokeAsync(() => node.IsExpanded = true);
                    await ExpandNodesUpToLevelAsync(node.Nodes, level - 1);
                }
            });
        }
    }
}