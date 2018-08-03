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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;



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

            // ライセンスキーを登録して Lite ライセンスの認証を行う
            string licenseKey = "xxxx";
            ArcGISRuntimeEnvironment.SetLicense(licenseKey);

      

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

            // マップに追加されているレイヤーの一覧取得
            TreeViewMultipleTemplatesSample();


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
            // ポップアップ オブジェクト作成の有無
            var returnPopupsOnly = false;

            // タップ地点に含まれるレイヤーを識別し、結果を取得
            IdentifyLayerResult idLayerResults = await MyMapView.IdentifyLayerAsync(layer, tapScreenPoint, pixelTolerance, returnPopupsOnly);

            if (idLayerResults.GeoElements.Count > 0)
            {
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

        public async void TreeViewMultipleTemplatesSample()
        {
            long i = 0;
            // TreeView にバインドする値を保持するための レイヤー情報を格納する myLayerInfo の List オブジェクトを定義する
            List<myLayerInfo> layers = new List<myLayerInfo>();

            // マップに追加されているレイヤーの一覧を取得
            foreach (var mylayer in MyMapView.Map.AllLayers)
            {
                // マップに格納されているレイヤーにユニークな ID を設定 (レイヤーの表示・非表示機能を実装する際に使用する)
                mylayer.Id = i.ToString();

                // レイヤー名 (レイヤー オブジェクトから取得) とレイヤー ID をセットして myLayerInfo オブジェクトを作成する
                myLayerInfo myLayerInfo = new myLayerInfo() { myLayerName = mylayer.Name, myLayerID = mylayer.Id };

                // レイヤーに設定されているシンボル情報 (シンボル画像とその名称) を取得する
                var legendinfos = await mylayer.GetLegendInfosAsync();
                if (legendinfos != null)
                {
                    foreach(var item in legendinfos)
                    {
                        // シンボルを BitmapFrame 型に変換し画像として取得する
                        var sym = item.Symbol;
                        var imageData = await sym.CreateSwatchAsync(1 * 96);
                        var imgst = await imageData.GetEncodedBufferAsync();
                        var img = System.Drawing.Bitmap.FromStream(imgst);

                        // メモリストリームに BitmapFrame を保存して myLayerInfo オブジェクトにシンボルの画像と名称をセットする
                        using (Stream stream = new MemoryStream())
                        {
                            img.Save(stream, ImageFormat.Png);
                            stream.Seek(0, SeekOrigin.Begin);
                            myLayerInfo.Members.Insert(0, new mySymbolInfo() { mySymbolImage = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad), mySymbolName = item.Name });
                        }
                    }
                }
                // List オブジェクトの一番上に myLayerInfo を追加する
                layers.Insert(0, myLayerInfo);
                i = i + 1;
            }
            // 作成した List オブジェクトの値を TreeView に反映する
            ContentsTree.ItemsSource = layers;
        }

        // コンテンツ ウィンドウのチェックボックスのイベントハンドラー
        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            var checkBox = (CheckBox)sender;

            // レイヤーの表示・非表示を切り替える
            visivleLayer(checkBox.Tag.ToString());
        }

        // レイヤーの表示・非表示
        private void visivleLayer(string layername)
        {
            foreach (var mylayer in MyMapView.Map.AllLayers)
            {
                if (mylayer.Id == layername)
                {
                    if (mylayer.IsVisible)
                    {
                        // レイヤーを非表示にする
                        mylayer.IsVisible = false;
                    }
                    else
                    {
                        // レイヤーを表示する
                        mylayer.IsVisible = true;
                    }
                    
                }
            }
        }
    }

    // コンテンツ ウィンドウに表示するレイヤーの DataContext プロパティ
    public class myLayerInfo: BindableBase
    {
        public myLayerInfo()
        {
            this.Members = new ObservableCollection<mySymbolInfo>();
        }

        public string myLayerName { get; set; }

        public string myLayerID { get; set; }

        public ObservableCollection<mySymbolInfo> Members { get; set; }


        private bool _IsChecked = true;
        public bool IsChecked
        {
            get { return _IsChecked; }
            set { this.SetProperty(ref _IsChecked, value); }
        }
    }

    // コンテンツ ウィンドウに表示するシンボル画面の DataContext プロパティ
    public class mySymbolInfo
    {
        public BitmapFrame mySymbolImage { get; set; }
        public string mySymbolName { get; set; }

    }


    public class BindableBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual bool SetProperty<T>(ref T field, T value, [CallerMemberName]string propertyName = null)
        {
            if (Equals(field, value)) { return false; }
            field = value;
            var h = this.PropertyChanged;
            if (h != null) { h(this, new PropertyChangedEventArgs(propertyName)); }
            return true;
        }
    }

}
