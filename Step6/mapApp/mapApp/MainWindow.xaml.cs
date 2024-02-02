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

using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI.Controls;
using Esri.ArcGISRuntime.UI;


namespace mapApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // 現在地をマップ上に表示する
            MainMapView.LocationDisplay.IsEnabled = true;
            MainMapView.LocationDisplay.AutoPanMode = LocationDisplayAutoPanMode.Recenter;

        }


        // ズームインボタンのイベント ハンドラー
        private async void OnZoomin(object sender, RoutedEventArgs e)
        {
            // 現在のマップのスケールを取得し、1/2 のスケールで表示する
            double currentScale = MainMapView.MapScale;
            double scale = currentScale / 2;
            await MainMapView.SetViewpointScaleAsync(scale);
        }

        // ズームアウトボタンのイベント ハンドラー
        private async void OnZoomout(object sender, RoutedEventArgs e)
        {
            // 現在のマップのスケールを取得し、2 倍のスケールで表示する
            double currentScale = MainMapView.MapScale;
            double scale = currentScale * 2;
            await MainMapView.SetViewpointScaleAsync(scale);
        }

        // マップ ビューのタップイベント ハンドラー
        public async void MainMapView_GeoViewTapped(object sender, GeoViewInputEventArgs e)
        {
            // 既に表示されているコールアウトを非表示にする
            MainMapView.DismissCallout();

            // タップした地点のスクリーン座標を取得する
            Point tapScreenPoint = e.Position;

            // タップした地点のマップ座標を取得する
            MapPoint tapMapPoint = e.Location!;

            // 識別範囲（タップした地点を中心）とするスクリーン座標の幅と高さ
            int pixelTolerance = 1;

            // ポップアップ オブジェクト作成の有無
            var returnPopupsOnly = false;

            // タップ地点に含まれるレイヤーを識別し、結果を取得する
            IReadOnlyList<IdentifyLayerResult> identifyLayerResults = await MainMapView.IdentifyLayersAsync(tapScreenPoint, pixelTolerance, returnPopupsOnly);

            var currentMapViewModel = (MapViewModel)this.FindResource("MapViewModel");

            // コールアウトの表示内容を作成する
            CalloutDefinition? calloutDefinition = currentMapViewModel.Identify(identifyLayerResults);

            // コールアウトをマップ上に表示する
            if (calloutDefinition != null)
            {
                MainMapView.ShowCalloutAt(tapMapPoint, calloutDefinition);
            }
        }

    }
}
