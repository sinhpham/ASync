using System;
using System.Collections.Generic;
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
using ASyncLib;
using System.IO;
using System.Diagnostics;

namespace ASyncWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            _dataDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "aaa");
            InitializeComponent();
        }

        private void GendbClicked(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() => RunFunctionTimed(() => GenServerData(1000000, 50)));
        }

        private void GenP1Clicked(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() => RunFunctionTimed(() => GenPatch1File()));
        }

        private void GenP2Clicked(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() => RunFunctionTimed(() => GenPatch2File()));
        }

        static string _dataDir;
        static string DataDir
        {
            get
            {
                return _dataDir;
            }
        }

        private static void GenServerData(int size, int changedPercent)
        {
            Console.WriteLine("Begin gen data");
            // Gen data for server
            using (var trans = DbManager.Engine.GetTransaction())
            {
                trans.Technical_SetTable_OverwriteIsNotAllowed(DbManager.DefaultTableName);
                foreach (var item in DataGen.Gen(size, changedPercent))
                {
                    trans.Insert(DbManager.DefaultTableName, item.Key, item.Value);
                }
                trans.Commit();
            }
            //DbManager.Dispose();
        }

        private static void GenPatch1File()
        {
            Console.WriteLine("Begin gen p1");
            using (var trans = DbManager.Engine.GetTransaction())
            {
                trans.Technical_SetTable_OverwriteIsNotAllowed(DbManager.DefaultTableName);
                var serverDic = trans.SelectForward<string, string>(DbManager.DefaultTableName).Select(t => new KeyValuePair<string, string>(t.Key, t.Value));
                using (var bffile = File.OpenRead(System.IO.Path.Combine(DataDir, Helper.BFFileName)))
                {
                    using (var pfile = File.OpenWrite(System.IO.Path.Combine(DataDir, Helper.P1FileName)))
                    {
                        KeyValSync.ServerGenPatch1File(serverDic, (int)trans.Count(DbManager.DefaultTableName), bffile, pfile);
                    }
                }
            }
        }

        private static void GenPatch2File()
        {
            Console.WriteLine("Begin gen p2");
            using (var trans = DbManager.Engine.GetTransaction())
            {
                trans.Technical_SetTable_OverwriteIsNotAllowed(DbManager.DefaultTableName);
                var serverDic = trans.SelectForward<string, string>(DbManager.DefaultTableName).Select(t => new KeyValuePair<string, string>(t.Key, t.Value));

                using (var ibfFile = File.OpenRead(System.IO.Path.Combine(DataDir, Helper.IBFFileName)))
                {
                    using (var p2File = File.OpenWrite(System.IO.Path.Combine(DataDir, Helper.P2FileName)))
                    {
                        KeyValSync.ServerGenPatch2FromIBF(serverDic, key =>
                        {
                            var v = trans.Select<string, string>(DbManager.DefaultTableName, key);
                            if (!v.Exists)
                            {
                                throw new InvalidDataException();
                            }
                            return v.Value;
                        }, ibfFile, p2File);
                    }
                }
            }
            //DbManager.Dispose();
        }

        private static void RunFunctionTimed(Action act)
        {
            var sw = new Stopwatch();
            sw.Start();
            act();
            sw.Stop();
            Console.WriteLine("Done in {0}", sw.Elapsed);
        }
    }
}
