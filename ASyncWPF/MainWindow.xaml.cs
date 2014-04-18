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
using ProtoBuf;

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
            var size = int.Parse(sizeTb.Text);
            var changedPer = int.Parse(changedTb.Text);

            Task.Factory.StartNew(() => RunFunctionTimed(() => GenServerData(size, changedPer)));
        }

        private void GenP1Clicked(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() => RunFunctionTimed(() => GenPatch1File()));
        }

        private void GenP2Clicked(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() => RunFunctionTimed(() => GenPatch2File()));
        }

        static Dictionary<string, string> _serverDic = new Dictionary<string, string>();

        static string _dataDir;
        static string DataDir
        {
            get
            {
                return _dataDir;
            }
        }

        private void GenServerData(int size, int changedPer)
        {
            _serverDic.Clear();
            Console.WriteLine("Begin gen data, size = {0}, changedPer = {1}", size, changedPer);
            // Gen data for server
            foreach (var item in DataGen.Gen(size, changedPer))
            {
                _serverDic.Add(item.Key, item.Value);
            }
        }

        private void GenPatch1File()
        {
            Console.WriteLine("Begin gen p1");
            using (var bffile = File.OpenRead(System.IO.Path.Combine(DataDir, Helper.BFFileName)))
            {
                using (var pfile = File.Create(System.IO.Path.Combine(DataDir, Helper.P1FileName)))
                {
                    KeyValSync.ServerGenPatch1File(_serverDic, _serverDic.Count, bffile, pfile);
                }
            }
        }

        private void GenPatch2File()
        {
            Console.WriteLine("Begin gen p2");

            using (var ibfFile = File.OpenRead(System.IO.Path.Combine(DataDir, Helper.IBFFileName)))
            {
                using (var p2File = File.Create(System.IO.Path.Combine(DataDir, Helper.P2FileName)))
                {
                    KeyValSync.ServerGenPatch2FromIBF(_serverDic, key =>
                    {
                        return _serverDic[key];
                    }, ibfFile, p2File);
                }
            }
        }

        private void RunFunctionTimed(Action act)
        {
            var sw = new Stopwatch();
            sw.Start();
            act();
            sw.Stop();
            Console.WriteLine("Done in {0}", sw.Elapsed);
        }

        private void SaveDbClicked(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Saving db");
            RunFunctionTimed(() =>
            {
                using (var f = File.Create("db.dat"))
                {
                    Serializer.Serialize(f, _serverDic);
                }
            });
        }
    }
}
