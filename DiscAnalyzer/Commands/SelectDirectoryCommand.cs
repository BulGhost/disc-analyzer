using Aga.Controls.Tree;
using DiscAnalyzer.ViewModels;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace DiscAnalyzer.Commands
{
    class SelectDirectoryCommand : CommandBase
    {
        public override bool CanExecute(object parameter)
        {
            return true;
        }

        public override void Execute(object parameter)
        {
            var openDlg = new CommonOpenFileDialog { IsFolderPicker = true };
            if (openDlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var treeList = (TreeList)parameter;
                treeList.Model = new ApplicationViewModel(openDlg.FileName);
                treeList.Nodes[0].IsExpanded = true;
            }
        }
    }
}
