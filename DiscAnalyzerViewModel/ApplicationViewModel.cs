using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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
    public class ApplicationViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        #region Fields

        private readonly ILogger _logger;
        private IAsyncCommand _openDialogCommand;
        private IAsyncCommand<string> _driveScanCommand;
        private RelayCommand _stopCommand;
        private IAsyncCommand _refreshCommand;
        private bool _readyToScan;
        private Task _directoryAnalysis;
        private ItemBaseProperty _mode;
        private string _percentOfParentColumnName;
        private CancellationTokenSource _source;

        #endregion

        #region Properties

        public FileSystemItem RootItem { get; private set; }

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
        public string DiscFreeSpaceInfo { get; private set; }
        public string FilesCountInfo { get; private set; }
        public string ClusterSizeInfo { get; private set; }

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
                OpenDialogCommand.RaiseCanExecuteChanged();
                DriveScanCommand.RaiseCanExecuteChanged();
                RefreshCommand.RaiseCanExecuteChanged();
            }
        }

        public bool AnalysisInProgress { get; private set; }
        public bool CanRefresh => RootItem != null && _readyToScan;

        #endregion

        public ApplicationViewModel(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            ReadyToScan = true;
        }

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
                    _source?.Cancel();
                    _logger.LogInformation("Stopping analysis of {0} directory is initiated", RootItem.FullPath);
                    AnalysisInProgress = false;
                },
                () => AnalysisInProgress);

        public IAsyncCommand RefreshCommand =>
            _refreshCommand ??= new AsyncCommand(async () =>
                    await RunDirectoryScanning(RootItem.FullPath),
                _ => CanRefresh);

        public RelayCommand ExitCommand =>
            new(() =>
            {
                _logger.LogInformation("Shutdown application");
                Application.Current.Shutdown();
            });

        #endregion

        private Task RunDirectoryScanning(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                throw new ArgumentException(Resources.IncorrectFilePath, nameof(fullPath));
            }

            ReadyToScan = false;
            _logger.LogInformation("Start analysis of {0} directory", fullPath);
            _source?.Cancel();
            return AnalyzeDirectory(fullPath);
        }

        private async Task AnalyzeDirectory(string directoryPath)
        {
            UpdateStatusBarInfo(directoryPath);
            if (_source != null)
            {
                await CleanUpTreeList();
            }

            _source = new CancellationTokenSource();
            (_directoryAnalysis, RootItem) = new FileSystemItemFactory().CreateNewAsync(directoryPath,
                Mode, _logger, _source.Token);
            AnalysisInProgress = true;
            ReadyToScan = true;

            try
            {
                await _directoryAnalysis;
                FilesCountInfo = string.Format(Resources.FilesCountInfo, RootItem.Files);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("{0} directory analysis stopped", directoryPath);
            }

            AnalysisInProgress = false;
            if (!_source.IsCancellationRequested)
            {
                _logger.LogInformation("{0} directory analysis completed successfully", directoryPath);
            }

            _source = null;
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
                RootItem = null;
                _logger.LogInformation("Tree cleaned up after refresh");
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
                _logger.LogError(ex, "Fail to get DriveInfo on path: {0}", path);
                throw;
            }

            long freeSpaceInBytes = info.AvailableFreeSpace;
            string freeSpace = BytesConverter.ConvertAutomatically(freeSpaceInBytes);
            long totalSpaceInBytes = info.TotalSize;
            string totalSpace = BytesConverter.ConvertAutomatically(totalSpaceInBytes);

            DiscFreeSpaceInfo = string.Format(Resources.DiscFreeSpaceInfo, freeSpace, totalSpace);
            uint clusterSize = new FileSizeOnDiskDeterminator().DetermineClusterSize(info.Name);
            ClusterSizeInfo = string.Format(Resources.ClusterSizeInfo, clusterSize, info.DriveFormat);
        }
    }
}