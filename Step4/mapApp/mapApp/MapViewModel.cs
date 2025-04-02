using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Location;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks;
using Esri.ArcGISRuntime.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace mapApp
{
    /// <summary>
    /// Provides map data to an application
    /// </summary>
    public class MapViewModel : INotifyPropertyChanged
    {
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
            PortalItem mapItem = await PortalItem.CreateAsync(portal, "3cbd757aede647869515251231a27e08");

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

        }

    }
}
