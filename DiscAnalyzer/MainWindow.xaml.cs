using System.Collections.ObjectModel;
using System.Windows.Controls.Ribbon;
using System.Windows.Input;
using DiscAnalyzer.ViewModels;
using Aga.Controls.Tree;

namespace DiscAnalyzer
{
    public partial class MainWindow : RibbonWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            //DataContext = new FileSystemItemViewModel("D:\\Дмитрий\\Авто", true);
            //TODO: Bound with Select directory button

            //Tree.ItemsSource = new ObservableCollection<FileSystemItemViewModel>
            //{
            //    new("D:\\Дмитрий\\Авто", true)
            //};

            Tree.Model = new FileSystemItemViewModel("D:\\Дмитрий\\Авто", true);
        }
    }
}
