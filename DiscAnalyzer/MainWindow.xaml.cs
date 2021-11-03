using System;
using System.Windows.Controls.Ribbon;
using Microsoft.Extensions.Logging;

namespace DiscAnalyzer
{
    public partial class MainWindow : RibbonWindow
    {
        public MainWindow(ILogger<MainWindow> logger)
        {
            try
            {
                InitializeComponent();

                DataContext = new ApplicationViewModel(Tree, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during {0} constructing", nameof(MainWindow));
                throw;
            }
        }
    }
}
