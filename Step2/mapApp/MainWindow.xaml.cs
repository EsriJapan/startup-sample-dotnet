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
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime;
using System.Threading;

namespace mapApp
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Initialize();

        }

        public async void Initialize()
        {
            MobileMapPackage myMapPackage = await MobileMapPackage.OpenAsync(@"..\..\..\..\SampleData\sample_maps.mmpk");
            // マップ ビューの持つ Map プロパティにモバイル マップ パッケージが持つマップを割り当てる
            MyMapView.Map = myMapPackage.Maps.First();
            // マップのレイヤーを取得してレイヤーの透過率を設定
            MyMapView.Map.OperationalLayers[0].Opacity = 0.5;
        }


    }
}
