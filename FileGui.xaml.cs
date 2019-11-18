using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CbOrmTester
{

    public static class CGuiCommand
    {
        public static void Invoke(Action aCommand)
        {
            try
            {
                aCommand();
            }
            catch(Exception aExc)
            {
                MessageBox.Show(aExc.Message, string.Empty, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// Interaktionslogik für TestCaseGui.xaml
    /// </summary>
    public partial class FileGui : UserControl
    {
        public FileGui()
        {
            InitializeComponent();
        }

        private void OnShowButtonClock(object sender, RoutedEventArgs e)
        {
            CGuiCommand.Invoke(delegate ()
            {
                this.Show();
            });
        }

        public void Show()
        {
            var aFileVm = (CFileVm)this.DataContext;
            if (aFileVm != null)
            {                
                Process.Start(aFileVm.FileInfo.FullName);
                aFileVm.Refresh();
            }
        }

        internal void Refresh()
        {
            var aFileVm = (CFileVm)this.DataContext;
            if (aFileVm != null)
            {
                aFileVm.Refresh();
            }
        }
    }
}
