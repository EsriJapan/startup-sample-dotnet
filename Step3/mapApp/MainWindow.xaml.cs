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
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using Esri.ArcGISRuntime.Data;
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
            // 背景に ArcGIS Online の衛星画像サービスを表示する
            MyMapView.Map.Basemap = Basemap.CreateImagery();
            // マップのレイヤーを取得してレイヤーの透過率を設定
            MyMapView.Map.OperationalLayers[0].Opacity = 0.5;
            // マップのスケール範囲を設定
            MyMapView.Map.MinScale = 5000000;
            MyMapView.Map.MaxScale = 5000;

            // マップ ビューのタップイベント
            MyMapView.GeoViewTapped += MyMapView_GeoViewTapped;
        }

        private async void MyMapView_GeoViewTapped(object sender, GeoViewInputEventArgs e)
        {
            // 識別対象のレイヤーを取得
            var layer = MyMapView.Map.OperationalLayers[0] as FeatureLayer;
            
            // フィーチャのハイライトをクリア
            layer.ClearSelection();

            // コールアウトを非表示
            MyMapView.DismissCallout();

            // タップした地点のスクリーン ポイントを取得
            Point tapScreenPoint = e.Position;

            // タップした地点の許容範囲
            var pixelTolerance = 1;
            // ポップアップのみを取得するかどうか
            var returnPopupsOnly = false;

            // タップ地点上にあるフィーチャを取得
            IdentifyLayerResult idLayerResults = await MyMapView.IdentifyLayerAsync(layer, tapScreenPoint, pixelTolerance, returnPopupsOnly);
            
            if (idLayerResults.GeoElements.Count > 0) {
                // 選択したフィーチャをハイライト
                Feature idFeature = idLayerResults.GeoElements[0] as Feature;
                layer.SelectFeature(idFeature);

                // コールアウトのコンテンツを作成
                var layerName = layer.Name;

                var attributes = new System.Text.StringBuilder();
                if (idFeature.Attributes.Count > 0)
                {
                    foreach (var attribute in idFeature.Attributes)
                    {
                        // フィーチャの属性（key:属性のフィールド名、value:属性のフィールド値のペア）を取得
                        var fieldName = attribute.Key;
                        var fieldValue = attribute.Value;
                        attributes.AppendLine(fieldName + ": " + fieldValue);
                    }
                    attributes.AppendLine();
                }

                // コールアウトのコンテンツを定義
                CalloutDefinition myCalloutDefinition = new CalloutDefinition(layerName, attributes.ToString());
                // コールアウトを表示
                MyMapView.ShowCalloutAt(e.Location, myCalloutDefinition);
            }
        }

        // ズームインボタンのイベント ハンドラー
        private async void OnZoomin(object sender, RoutedEventArgs e)
        {
            // 現在のマップのスケールを取得し、1/2 のスケールで表示する
            var currentScale = MyMapView.MapScale;
            var scale = currentScale / 2;
            await MyMapView.SetViewpointScaleAsync(scale);
        }

        // ズームアウトボタンのイベント ハンドラー
        private async void OnZoomout(object sender, RoutedEventArgs e)
        {
            // 現在のマップのスケールを取得し、2 倍のスケールで表示する
            var currentScale = MyMapView.MapScale;
            var scale = currentScale * 2;
            await MyMapView.SetViewpointScaleAsync(scale);
        }
    }
}
