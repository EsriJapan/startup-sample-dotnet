using Esri.ArcGISRuntime.Controls;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Layers;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks.Geocoding;
using Esri.ArcGISRuntime.Tasks.Geoprocessing;
using Esri.ArcGISRuntime.Tasks.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MyMapApp
{
    /// <summary>
    /// Step 8: 到達圏解析
    /// </summary>
    public partial class MainWindow : Window
    {
        //ArcGIS Online ジオコーディングサービスの URL
        private const string WORLD_GEOCODE_SERVICE_URL = "http://geocode.arcgis.com/arcgis/rest/services/World/GeocodeServer";

        //ArcGIS Online 到達圏解析ジオプロセシング サービスの URL
        private const string SERVICE_AREA_GP_URL = "http://logistics.arcgis.com/arcgis/rest/services/World/ServiceAreas/GPServer/GenerateServiceAreas";

        private bool isMapReady;                                    //マップが操作可能であるかどうかを示す変数
        private GraphicsLayer geocodeResultGraphicsLayer;           //住所検索結果表示用のグラフィックスレイヤ
        private OnlineLocatorTask onlineLocatorTask;                //住所検索用のジオコーディング タスク
        private FeatureLayer buildingLayer;                         //物件フィーチャレイヤ
        private bool isHitTesting;                                  //フィーチャレイヤへのヒットテスト中であるかどうかを示す変数
        private Geoprocessor serviceAreaGp;                         //到達圏解析用のジオプロセシング タスク
        private GraphicsLayer serviceAreaResultLayer;               //到達圏解析結果表示用のグラフィックスレイヤ
        private Graphic serviceAreaGraphic;                         //到達圏グラフィック

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

            //到達圏解析用のジオプロセシング タスクを初期化
            serviceAreaGp = new Geoprocessor(new Uri(SERVICE_AREA_GP_URL));
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

            //到達圏解析結果表示用のグラフィックスレイヤを取得
            serviceAreaResultLayer = mainMapView.Map.Layers["ServiceAreaResultLayer"] as GraphicsLayer;

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

        #region 到達圏解析
        /// <summary>
        /// 到達圏解析の解析ボタンクリック時の処理
        /// </summary>
        private void analyzeButton_Click(object sender, RoutedEventArgs e)
        {
            //マップが準備できていなければ処理を行わない
            if (!isMapReady) return;

            //前回の到達圏解析の結果をクリア
            ClearAnalysisResult();

            //解析ボタンを非表示
            analyzePanel.Visibility = System.Windows.Visibility.Collapsed;

            //解析手順のメッセージを表示
            analyzeTextBox.Visibility = System.Windows.Visibility.Visible;

            //マップビュータップ時のイベントハンドラを設定
            mainMapView.MapViewTapped += mainMapView_MapViewTapped;

            //カーソルを十字に変更
            mainMapView.Cursor = Cursors.Cross;
        }

        /// <summary>
        /// マップビュータップ時の処理
        /// </summary>
        private async void mainMapView_MapViewTapped(object sender, Esri.ArcGISRuntime.Controls.MapViewInputEventArgs e)
        {
            //解析手順のメッセージを非表示
            analyzeTextBox.Visibility = System.Windows.Visibility.Collapsed;

            //プログレスバーを表示
            analyzeProgressBar.Visibility = System.Windows.Visibility.Visible;

            //マップビュータップ時のイベントハンドラを解除
            mainMapView.MapViewTapped -= mainMapView_MapViewTapped;

            //カーソルを矢印に変更
            mainMapView.Cursor = Cursors.Arrow;

            //クリックした位置からグラフィックを作成
            Graphic clickPoint = new Graphic(e.Location)
            {
                Symbol = layoutRoot.Resources["greenMarkerSymbol"] as SimpleMarkerSymbol,
                ZIndex = 2
            };

            //到達圏解析結果表示用のグラフィックスレイヤにクリック位置のグラフィックを追加
            serviceAreaResultLayer.Graphics.Add(clickPoint);

            try
            {
                //到達圏解析用パラメーターの作成
                GPInputParameter parameter = new GPInputParameter();
                parameter.GPParameters.Add(new GPFeatureRecordSetLayer("facilities", e.Location));  //解析の中心点
                parameter.GPParameters.Add(new GPString("break_values", "10"));                     //到達圏の範囲（10分）
                parameter.GPParameters.Add(new GPString("env:outSR", "102100"));                    //結果の空間参照（Web メルカトル）
                parameter.GPParameters.Add(new GPString("travel_mode", "Walking"));                 //"徒歩"で到達できる範囲を解析

                //到達圏の解析を開始
                GPJobInfo result = await serviceAreaGp.SubmitJobAsync(parameter);

                //到達圏の解析結果が"成功"、"失敗"、"時間切れ"、"キャンセル"のいずれかになるまで
                //2秒ごとに ArcGIS Online にステータスを確認
                while (result.JobStatus != GPJobStatus.Succeeded
                       && result.JobStatus != GPJobStatus.Failed
                       && result.JobStatus != GPJobStatus.TimedOut
                       && result.JobStatus != GPJobStatus.Cancelled)
                {
                    result = await serviceAreaGp.CheckJobStatusAsync(result.JobID);

                    await Task.Delay(2000);
                }


                //到達圏解析の結果が成功した場合は結果を表示
                if (result.JobStatus == GPJobStatus.Succeeded)
                {
                    //到達圏解析の結果を取得
                    GPParameter resultData = await serviceAreaGp.GetResultDataAsync(result.JobID, "Service_Areas");

                    //到達圏解析結果レイヤのグラフィックを結果グラフィックとして取得
                    GPFeatureRecordSetLayer gpLayer = resultData as GPFeatureRecordSetLayer;
                    serviceAreaGraphic = gpLayer.FeatureSet.Features[0] as Graphic;

                    //グラフィックにシンボルを設定
                    serviceAreaGraphic.Symbol = layoutRoot.Resources["greenFillSymbol"] as SimpleFillSymbol;

                    //結果グラフィックが解析の中心点のグラフィックより下に表示されるように表示順序を設定
                    serviceAreaGraphic.ZIndex = 1;

                    //到達圏解析結果表示用のグラフィックスレイヤにグラフィックを追加
                    serviceAreaResultLayer.Graphics.Add(serviceAreaGraphic);
                }
            }
            //エラーが発生した場合の処理
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("到達圏解析：{0}", ex.Message));

                //到達圏解析の結果をクリア
                ClearAnalysisResult();
            }
            finally
            {
                //プログレスバーを非表示
                analyzeProgressBar.Visibility = System.Windows.Visibility.Collapsed;

                //到達圏解析ボタンを表示
                analyzePanel.Visibility = System.Windows.Visibility.Visible;
            }
        }

        /// <summary>
        /// 到達圏解析結果クリアボタンクリック時の処理
        /// </summary>
        private void clearResultButton_Click(object sender, RoutedEventArgs e)
        {
            //到達圏解析結果のクリア
            ClearAnalysisResult();
        }

        /// <summary>
        /// 到達圏解析結果のクリア
        /// </summary>
        private void ClearAnalysisResult()
        {
            //到達圏グラフィックをクリア
            serviceAreaGraphic = null;

            //到達圏解析結果表示用グラフィックスレイヤのグラフィックをクリア
            serviceAreaResultLayer.Graphics.Clear();

            //物件レイヤのフィーチャの選択をすべてクリア
            buildingLayer.ClearSelection();
        }
        #endregion
    }
}
