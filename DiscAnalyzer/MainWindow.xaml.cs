using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Input;
using Aga.Controls.Tree;
using DiscAnalyzer.Commands;
using DiscAnalyzer.ViewModels;

namespace DiscAnalyzer
{
    public partial class MainWindow : RibbonWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public ApplicationViewModel ViewModel { get; set; } = new();

        public TreeList XProp =>
            Tree;
    }
}
