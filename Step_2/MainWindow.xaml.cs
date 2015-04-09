using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Layers;
using Esri.ArcGISRuntime.Tasks.Geocoding;
using Esri.ArcGISRuntime.Tasks.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;

namespace MyMapApp
{
    /// <summary>
    /// Step 2: 背景地図表示
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// マップビューの読み込みが完了した際の処理
        /// </summary>
        private async void mainMapView_Loaded(object sender, RoutedEventArgs e)
        {
            //マップ内のすべてのレイヤが読み込まれるまで待機
            await mainMapView.LayersLoadedAsync();
        }
    }
}
