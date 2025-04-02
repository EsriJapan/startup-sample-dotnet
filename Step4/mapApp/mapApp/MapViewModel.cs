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
            // Web �}�b�v�� ID ���g�p���ă}�b�v���擾����
            ArcGISPortal portal = await ArcGISPortal.CreateAsync();
            PortalItem mapItem = await PortalItem.CreateAsync(portal, "3cbd757aede647869515251231a27e08");

            // MapView �� Map �v���p�e�B�� Web �}�b�v�����蓖�Ă�
            Map = new Map(mapItem);

            // �x�[�X�}�b�v �X�^�C���̃p�����[�^�[���쐬����
            var basemapStyleParameters = new BasemapStyleParameters()
            {
                // �x�[�X�}�b�v�̒n�����x���� OS �̃��P�[���̌���ŕ\������
                LanguageStrategy = BasemapStyleLanguageStrategy.ApplicationLocale
            };

            // �x�[�X�}�b�v�̃^�C�v�iArcGISTopographic�F�n�`�}�j�ƃx�[�X�}�b�v �X�^�C���̃p�����[�^�[���w�肵�āA�V���� Basemap �I�u�W�F�N�g���쐬����
            var localizedBasemap = new Basemap(BasemapStyle.ArcGISTopographic, basemapStyleParameters);
            // Map �� Basemap �I�u�W�F�N�g��V�����쐬���� Basemap �I�u�W�F�N�g�ɒu��������
            Map.Basemap = localizedBasemap;

            // �}�b�v�����[�h����
            await Map.LoadAsync();

            // �}�b�v�̃X�P�[���͈͂�ݒ肷��
            Map.MinScale = 5000000;
            Map.MaxScale = 5000;

        }

    }
}
