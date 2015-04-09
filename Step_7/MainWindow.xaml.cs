using Esri.ArcGISRuntime.Controls;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Layers;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Tasks.Geocoding;
using Esri.ArcGISRuntime.Tasks.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace MyMapApp
{
    /// <summary>
    /// Step 7: フィルタ設定
    /// </summary>
    public partial class MainWindow : Window
    {
        //ArcGIS Online ジオコーディングサービスの URL
        private const string WORLD_GEOCODE_SERVICE_URL = "http://geocode.arcgis.com/arcgis/rest/services/World/GeocodeServer";

        private bool isMapReady;                                    //マップが操作可能であるかどうかを示す変数
        private GraphicsLayer geocodeResultGraphicsLayer;           //住所検索結果表示用のグラフィックスレイヤ
        private OnlineLocatorTask onlineLocatorTask;                //住所検索用のジオコーディング タスク
        private FeatureLayer buildingLayer;                         //物件フィーチャレイヤ
        private bool isHitTesting;                                  //フィーチャレイヤへのヒットテスト中であるかどうかを示す変数

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            //ArcGIS Online への認証方法の指定
            IdentityManager.Current.ChallengeHandler = new ChallengeHandler(PortalSecurity.Challenge);

            //住所検索用のジオコーディング タスクを初期化
            onlineLocatorTask = new OnlineLocatorTask(new Uri(WORLD_GEOCODE_SERVICE_URL));
        }

        /// <summary>
        /// マップビューの読み込みが完了した際の処理
        /// </summary>
        private async void mainMapView_Loaded(object sender, RoutedEventArgs e)
        {
            //マップ内のすべてのレイヤが読み込まれるまで待機
            await mainMapView.LayersLoadedAsync();

            //住所検索結果表示用のグラフィックスレイヤを取得
            geocodeResultGraphicsLayer = mainMapView.Map.Layers["GeocodingResultLayer"] as GraphicsLayer;

            //物件フィーチャレイヤを取得
            buildingLayer = mainMapView.Map.Layers["BuildingLayer"] as FeatureLayer;

            //マップ操作が可能な状態に変更
            isMapReady = true;
        }

        #region ジオコーディング
        /// <summary>
        /// 住所検索ボタンクリック時の処理
        /// </summary>
        private async void findLocationButton_Click(object sender, RoutedEventArgs e)
        {
            //マップが準備できていなければ処理を行わない
            if (!isMapReady) return;

            //住所検索用のパラメータを作成
            OnlineLocatorFindParameters parameters = new OnlineLocatorFindParameters(addressTextBox.Text)
            {
                MaxLocations = 5,
                OutSpatialReference = SpatialReferences.WebMercator,
                OutFields = OutFields.All
            };

            try
            {
                //住所の検索
                IList<LocatorFindResult> resultCandidates = await onlineLocatorTask.FindAsync(parameters, CancellationToken.None);

                //住所検索結果に対する処理（1つ以上候補が返されていれば処理を実行）
                if (resultCandidates != null && resultCandidates.Count > 0)
                {
                    //現在の結果を消去
                    geocodeResultGraphicsLayer.Graphics.Clear();

                    //常に最初の候補を採用
                    LocatorFindResult candidate = resultCandidates.FirstOrDefault();

                    //最初の候補からグラフィックを作成
                    Graphic locatedPoint = new Graphic() { Geometry = candidate.Feature.Geometry };

                    //住所検索結果表示用のグラフィックスレイヤにグラフィックを追加
                    geocodeResultGraphicsLayer.Graphics.Add(locatedPoint);

                    //追加したグラフィックの周辺に地図を拡大
                    await mainMapView.SetViewAsync((MapPoint)locatedPoint.Geometry, 2500);
                }
                //候補が一つも見つからない場合の処理
                else
                {
                    MessageBox.Show("住所検索：該当する場所がみつかりません。");
                }
            }
            //エラーが発生した場合の処理
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("住所検索：{0}", ex.Message));
            }
        }

        /// <summary>
        /// 住所検索クリアボタンクリック時の処理
        /// </summary>
        private void clearLocationButton_Click(object sender, RoutedEventArgs e)
        {
            //住所検索結果表示用グラフィックスレイヤのすべてのグラフィックを消去
            geocodeResultGraphicsLayer.Graphics.Clear();
        }
        #endregion

        #region マップチップ
        private async void mainMapView_MouseMove(object sender, MouseEventArgs e)
        {
            //マップが準備できていなければ処理を行わない
            if (!isMapReady) return;

            //物件レイヤのヒットテスト中は処理を行わない
            if (isHitTesting) return;

            //物件レイヤへのヒットテスト開始
            isHitTesting = true;
            try
            {
                //マップビューに対するマウス カーソル位置を取得
                System.Windows.Point screenPoint = e.GetPosition(mainMapView);

                //マウス カーソルの位置に物件フィーチャが存在するかヒットテストを実行
                long[] objectIds = await buildingLayer.HitTestAsync(mainMapView, screenPoint);

                //マウス カーソルの位置に物件フィーチャが存在する場合はマップチップを表示
                if (objectIds != null && objectIds.Length > 0)
                {
                    //ヒットテストが返したObject ID を持つ物件フィーチャを取得
                    IEnumerable<Feature> features = await buildingLayer.FeatureTable.QueryAsync(objectIds);
                    Feature feature = features.FirstOrDefault();

                    //マップチップのデータコンテキストに物件フィーチャを設定
                    mapTip.DataContext = feature;

                    //マップチップの表示位置をマウス カーソルの位置に設定
                    MapPoint anchorPoint = mainMapView.ScreenToLocation(screenPoint);
                    MapView.SetViewOverlayAnchor(mapTip, anchorPoint);

                    //マップチップを表示
                    mapTip.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    //マップチップを非表示
                    mapTip.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
            //エラーが発生した場合の処理
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("マップチップの非表示：{0}", ex.Message));

                //マップチップを非表示
                mapTip.Visibility = System.Windows.Visibility.Collapsed;
            }
            finally
            {
                //物件レイヤへのヒットテスト終了
                isHitTesting = false;
            }
        }
        #endregion

        #region レイヤフィルタ
        /// <summary>
        /// 築年数フィルタコンボボックス変更時の処理
        /// </summary>
        private void yearComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            //マップが準備できていなければ処理を行わない
            if (!isMapReady) return;

            //選択されたコンボボックスによりWhere句文字列を設定
            string whereString;
            switch (yearComboBox.SelectedIndex)
            {
                case 1:
                    whereString = "Age<5";
                    break;
                case 2:
                    whereString = "Age>=5 AND Age<10";
                    break;
                case 3:
                    whereString = "Age>=10";
                    break;
                default:
                    whereString = string.Empty;
                    break;
            }

            //物件フィーチャレイヤのフィーチャテーブルにWhere句を設定
            ServiceFeatureTable serviceFeatureTable = buildingLayer.FeatureTable as ServiceFeatureTable;
            serviceFeatureTable.Where = whereString;

            //物件フィーチャレイヤを再描画
            serviceFeatureTable.RefreshFeatures(false);
        }
        #endregion
    }
}
