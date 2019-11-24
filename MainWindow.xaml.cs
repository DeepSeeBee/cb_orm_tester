using CbOrm.Gen.Test;
using CbOrm.Test;
using CbOrm.Util;
using CbOrm.Xdl;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
using System.Windows.Threading;

namespace CbOrmTester
{

    public static class CDispatcherExtensions
    {
        public static void BeginInvoke(this Dispatcher aDispatcher, Action aAction)
        {
            aDispatcher.BeginInvoke(aAction);
        }
    }

    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var aDirectoryInfo = new DirectoryInfo(
                System.IO.Path.Combine(new FileInfo(this.GetType().Assembly.Location).Directory.Parent.Parent.Parent.FullName,
                    @"CbOrm\UnitTest"
                    ));
            var aFileInfo = new FileInfo(System.IO.Path.Combine(aDirectoryInfo.FullName, "TestSequence.xdl"));
            var aRows = CRflRow.NewFromTextFile(aFileInfo);
            var aVms = (from aRow in aRows
                        where aRow.RecognizeBool
                        select new CTestCaseVm(aFileInfo.Directory, aRow)).ToArray();
            this.TestCaseVms = aVms;
            this.RefreshTestCase();
            this.InterceptionBorder.Visibility = Visibility.Collapsed;
            this.TestSequenceFile = aFileInfo;
        }
        private readonly FileInfo TestSequenceFile;
        private IEnumerable<CTestCaseVm> TestCaseVms {
            get => (IEnumerable<CTestCaseVm>)this.DataContext;
            set => this.DataContext = value; }
        private CTestCaseVm TestCaseVmNullable { get => (CTestCaseVm)this.TestCasesListBox.SelectedItem; }
        private void TestCasesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.RefreshTestCase();
        }
        private void RefreshTestCase()
        {
            var aTestCaseVm = (CTestCaseVm)this.TestCasesListBox.SelectedItem;
            if (!aTestCaseVm.IsNullRef())
            {
                aTestCaseVm.Refresh();
            }
            this.TestCaseGrid.DataContext = aTestCaseVm;
        }


        private bool? Accept;
        private DispatcherFrame InterceptorFrame;
        private bool TestRunning;
        private void Run(IEnumerable<CTestCaseVm> aTestCaseVms)
        {
            if (this.TestRunning)
            {
                throw new InvalidOperationException();
            }
            else
            {

                foreach(var aTestCaseVm in this.TestCaseVms)
                {
                    aTestCaseVm.Ok = default(bool?);
                }

                this.TestRunning = true;
                this.Cursor = Cursors.Wait;
                try
                {
                    var aRunFrame = new DispatcherFrame();
                    var aRun = new Action(delegate ()
                    {
                        var aGenUnitTest = new CGenUnitTest(this.TestSequenceFile);
                        aGenUnitTest.TestInterceptor.Intercepting += delegate (object aSender, EventArgs aArgs)
                        {
                            var aTestInterceptEventArgs = aArgs as CTestResultEventArgs;
                            if (!aTestInterceptEventArgs.IsNullRef())
                            {
                                var aOk = (bool)aTestInterceptEventArgs.TestResult.Dyn().Ok;
                                if (!aOk)
                                {
                                    this.Accept = default(bool?);
                                    this.InterceptorFrame = new DispatcherFrame();
                                    this.Dispatcher.BeginInvoke(new Action(delegate ()
                                    {
                                        this.OutTestFileGui.Refresh();
                                        this.TestResultListBox.DataContext = aTestInterceptEventArgs.TestResult;
                                        this.InterceptionBorder.Visibility = Visibility.Visible;
                                    }));
                                    Dispatcher.PushFrame(this.InterceptorFrame);
                                    aGenUnitTest.TestInterceptor.Accepted = this.Accept.GetValueOrDefault(false);
                                    this.Dispatcher.BeginInvoke(new Action(delegate ()
                                    {
                                        this.InterceptionBorder.Visibility = Visibility.Collapsed;
                                    }));
                                }

                                this.Dispatcher.BeginInvoke(delegate ()
                                {
                                    var aTestCaseId = aTestInterceptEventArgs.TestCase.Name;
                                    var aTestCaseVm = (from aTest in this.TestCaseVms where aTest.Row.TypName == aTestCaseId select aTest).FirstOrDefault();
                                    if (!aTestCaseVm.IsNullRef())
                                    {
                                        aTestCaseVm.Ok = (bool)aTestInterceptEventArgs.TestResult.Dyn().Ok;
                                    }
                                });
                            }
                        };
                        var aTestCaseTyps = from aTestCaseVm in aTestCaseVms
                                            where aTestCaseVm.Row.RecognizeBool
                                            select aGenUnitTest.TestSequence.GetTypByName(aTestCaseVm.Row.TypName);
                        try
                        {
                            foreach (var aTestCaseTyp in aTestCaseTyps)
                            {
                                aGenUnitTest.Run(aTestCaseTyp);
                            }
                        }
                        catch(Exception)
                        {
                        }
                        aRunFrame.Continue = false;
                    });
                    var aThread = new System.Threading.Thread(new System.Threading.ThreadStart(aRun));
                    aThread.Start();
                    Dispatcher.PushFrame(aRunFrame);
                }
                finally
                {
                    this.TestRunning = false;
                    this.Cursor = Cursors.Arrow;
                }
            }
        }

        private void RunAllButton_Click(object sender, RoutedEventArgs e)
        {
            CGuiCommand.Invoke(delegate () { this.Run(this.TestCaseVms); });
        }

        private void RunSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            CGuiCommand.Invoke(delegate () { if (this.TestCaseVmNullable != null)
                    this.Run(new CTestCaseVm[] { this.TestCaseVmNullable }); });
        }

        private void TestResultListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(!this.TestCaseVmNullable.IsNullRef())
            {
                this.TestCaseVmNullable.Refresh();
            }
        }

        private void OnAcceptButtonClick(object sender, RoutedEventArgs e)
        {
            CGuiCommand.Invoke(delegate ()
            {
                this.Accept = true;
                this.InterceptorFrame.Continue = false;
            });
        }

        private void OnRejectButtonClick(object sender, RoutedEventArgs e)
        {
            CGuiCommand.Invoke(delegate ()
            {
                this.Accept = false;
                this.InterceptorFrame.Continue = false;
            });
        }
    }

    public class CFileVm  : CViewModel
    {
        public CFileVm(FileInfo aFileInfo)
        {
            this.FileInfo = aFileInfo;
            this.Refresh();
        }
        public FileInfo FileInfo { get; set; }


        private string ContentM;
        public string Content { get { return this.ContentM; }set { this.ContentM = value; this.OnPropertyChanged(nameof(this.Content)); } }

        public void Refresh()
        {
            this.Content = this.Exists
                         ? System.IO.File.ReadAllLines(this.FileInfo.FullName).JoinString(Environment.NewLine)
                         : string.Empty
                         ;
        }
        public bool Exists
        {
            get
            {
                return new FileInfo(this.FileInfo.FullName).Exists;
            }
        }

    }

    public abstract class CViewModel :INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string aPropertyName)
        {
            if (!this.PropertyChanged.IsNullRef())
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(aPropertyName));
            }
        }
    }
    public class CTestCaseVm  : CViewModel
    {
        public CTestCaseVm(DirectoryInfo aDir, CRflRow aRow)
        {
            var aMI = new CTestModelInterpreter();
            var aPrefix = System.IO.Path.Combine(aDir.FullName, "TestCases", aRow.TypName + "");
            var aInFile = new FileInfo(aPrefix + "-in.xdl");
            var aTestFile = new FileInfo(aPrefix + "-out-test.cs");
            var aOkFile = new FileInfo(aPrefix + "-out-ok.cs");
            this.InFileVm = new CFileVm(aInFile);
            this.OutTestFileVm = new CFileVm(aTestFile);
            this.OutOkFileVm = new CFileVm(aOkFile);
            this.Row = aRow;
        }
        public CRflRow Row { get; set; }
        public CFileVm InFileVm { get; set; }
        public CFileVm OutTestFileVm { get; set; }
        public CFileVm OutOkFileVm { get; set; }
        private bool? OkM;
        public bool? Ok
        {
            get => this.OkM;
            set
            {
                this.OkM = value;
                this.OnPropertyChanged(nameof(this.Ok));
            }
        }

        internal void Refresh()
        {
            this.InFileVm.Refresh();
            this.OutTestFileVm.Refresh();
            this.OutOkFileVm.Refresh();
        }
    }
}
