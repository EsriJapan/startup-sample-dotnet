using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Location;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks;
using Esri.ArcGISRuntime.Tasks.Geocoding;
using Esri.ArcGISRuntime.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace mapApp
{
    /// <summary>
    /// Provides map data to an application
    /// </summary>
    public class MapViewModel : INotifyPropertyChanged
    {

        private Feature? _selectedFeature;
        private FeatureLayer? _selectedLayer;
        private LocatorTask? _geocoder;


        public MapViewModel()
        {
            _ = SetupMap();
        }

        private Map _map;

        /// <summary>
        /// Gets or sets the map
        /// </summary>
        public Map Map
        {
            get => _map;
            set { _map = value; OnPropertyChanged(); }
        }

        private GraphicsOverlayCollection? _graphicsOverlays;
        public GraphicsOverlayCollection? GraphicsOverlays
        {
            get { return _graphicsOverlays; }
            set
            {
                _graphicsOverlays = value;
                OnPropertyChanged();
            }
        }


        /// <summary>
        /// Raises the <see cref="MapViewModel.PropertyChanged" /> event
        /// </summary>
        /// <param name="propertyName">The name of the property that has changed</param>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler? PropertyChanged;


        private async Task SetupMap()
        {
            // Web マップの ID を使用してマップを取得する
            ArcGISPortal portal = await ArcGISPortal.CreateAsync();
            PortalItem mapItem = await PortalItem.CreateAsync(portal, "3bf36dae4cc545c8956a521fa9ada1eb");

            // MapView の Map プロパティに Web マップを割り当てる
            Map = new Map(mapItem);

            // ベースマップ スタイルのパラメーターを作成する
            var basemapStyleParameters = new BasemapStyleParameters()
            {
                // ベースマップの地名ラベルを OS のロケールの言語で表示する
                LanguageStrategy = BasemapStyleLanguageStrategy.ApplicationLocale
            };

            // ベースマップのタイプ（ArcGISTopographic：地形図）とベースマップ スタイルのパラメーターを指定して、新しい Basemap オブジェクトを作成する
            var localizedBasemap = new Basemap(BasemapStyle.ArcGISTopographic, basemapStyleParameters);
            // Map の Basemap オブジェクトを新しく作成した Basemap オブジェクトに置き換える
            Map.Basemap = localizedBasemap;

            // マップをロードする
            await Map.LoadAsync();

            // マップのスケール範囲を設定する
            Map.MinScale = 5000000;
            Map.MaxScale = 5000;

            // 住所検索結果の表示用の新しい GraphicsOverlay を作成する
            GraphicsOverlay geocodeResultOverlay = new GraphicsOverlay();
            // GraphicsOverlays (GraphicsOverlayCollection) プロパティを設定する
            GraphicsOverlayCollection graphicsOverlays = new GraphicsOverlayCollection
            {
               geocodeResultOverlay
            };
            this.GraphicsOverlays = graphicsOverlays;

            // 住所検索用の LocatorTask を作成する
            Uri _serviceUri = new Uri("https://geocode-api.arcgis.com/arcgis/rest/services/World/GeocodeServer");
            _geocoder = await LocatorTask.CreateAsync(_serviceUri);


        }

        public CalloutDefinition Identify(IReadOnlyList<IdentifyLayerResult> identifyLayerResults)
        {
            CalloutDefinition? calloutDefinition = null;

            try
            {
                // 既に選択されている（ハイライト表示されている）フィーチャの選択を解除する
                if (_selectedFeature != null)
                {
                    _selectedLayer!.UnselectFeature(_selectedFeature);
                }

                if (identifyLayerResults.Count == 0 || identifyLayerResults[0].GeoElements.Count == 0)
                {
                    throw new Exception("フィーチャが見つかりません");
                }

                // IdentifyLayersAsync() で取得したフィーチャを選択する（ハイライト表示する）
                _selectedFeature = (Feature)identifyLayerResults[0].GeoElements[0];
                _selectedLayer = (FeatureLayer)identifyLayerResults[0].LayerContent;
                _selectedLayer.SelectFeature(_selectedFeature);

                // レイヤーの属性情報のフィールド名とエイリアスを取得する
                // Key にフィールド名、Value にフィールドのエイリアスを格納したディクショナリを作成する
                var fields = new Dictionary<string, string>();
                for (int i = 0; i < _selectedLayer.FeatureTable!.Fields.Count; i++)
                {
                    Field? field = _selectedLayer.FeatureTable.Fields[i];
                    fields.Add(field.Name, field.Alias);
                }

                // コールアウトのコンテンツ（タイトル）を設定する
                string layerName = _selectedLayer.Name;

                // 該当フィーチャの全ての属性情報を結合した文字列を作成する
                var attributes = new System.Text.StringBuilder();
                if (_selectedFeature.Attributes.Count > 0)
                {
                    foreach (var attribute in _selectedFeature.Attributes)
                    {
                        // 該当フィールドのエイリアスを取得する（attribute.Key はフィールド名）
                        string fieldName = fields[attribute.Key];
                        // 該当フィールドのフィールド値を取得する
                        object fieldValue = attribute.Value!;
                        attributes.AppendLine(fieldName + ": " + fieldValue);
                    }
                    attributes.AppendLine();
                }

                // コールアウトのコンテンツを定義する CalloutDefinition を作成する
                calloutDefinition = new CalloutDefinition(layerName, attributes.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "エラー");
            }
            return calloutDefinition!;
        }

        public async Task<MapPoint> SearchAddress(string searchText)
        {
            MapPoint? addressLocation = null;
            try
            {
                // 既存の検索結果のグラフィックスを消去する
                GraphicsOverlay? geocodeResultOverlay = this.GraphicsOverlays?.FirstOrDefault();
                geocodeResultOverlay!.Graphics.Clear();

                // テキストボックスの値が空か LocatorTask が無効の場合は、処理を中止する
                if (String.IsNullOrWhiteSpace(searchText) || _geocoder == null)
                {
                    throw new Exception("検索文字列が空か、ジオコーダーが無効です");
                }

                // 検索結果を1つ（マッチ率が最も高い住所）だけに制限する、ジオコーディングのパラメーターを作成する
                GeocodeParameters geocodeParameters = new GeocodeParameters();
                geocodeParameters.MaxResults = 1;

                //入力文字列からジオコーディングを実行しマッチする住所（住所文字列と位置）を取得する
                IReadOnlyList<GeocodeResult> addresses = await _geocoder.GeocodeAsync(searchText, geocodeParameters);

                // 検索結果が取得できない場合は処理を中止する
                if (addresses.Count < 1)
                {
                    throw new Exception("一致する結果がありません");
                }

                // 住所の位置に表示するポイント シンボルを作成する
                SimpleMarkerSymbol pointSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, System.Drawing.Color.Red, 15);

                // 住所文字列のテキスト シンボルを作成する
                TextSymbol textSymbol = new TextSymbol(addresses.First().Label, System.Drawing.Color.Yellow, 15, Esri.ArcGISRuntime.Symbology.HorizontalAlignment.Center, Esri.ArcGISRuntime.Symbology.VerticalAlignment.Bottom);

                textSymbol.HaloColor = System.Drawing.Color.Gray;
                textSymbol.HaloWidth = 3;
                textSymbol.OffsetY = 10;

                // ポイントとテキスト シンボルからグラフィックを作成する
                Graphic pointGraphic = new Graphic(addresses.First().DisplayLocation, pointSymbol);
                Graphic textGraphic = new Graphic(addresses.First().DisplayLocation, textSymbol);

                // GraphicsOverlay にグラフィックを追加する
                geocodeResultOverlay.Graphics.Add(pointGraphic);
                geocodeResultOverlay.Graphics.Add(textGraphic);

                // マップを住所の位置にズームするためのポイントを返す
                addressLocation = addresses.First().DisplayLocation;
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "エラー");
            }

            return addressLocation!;
        }

    }
}
