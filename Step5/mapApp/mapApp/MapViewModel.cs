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

        public CalloutDefinition Identify(IReadOnlyList<IdentifyLayerResult> identifyLayerResults)
        {
            CalloutDefinition? calloutDefinition = null;

            try
            {
                // ���ɑI������Ă���i�n�C���C�g�\������Ă���j�t�B�[�`���̑I������������
                if (_selectedFeature != null)
                {
                    _selectedLayer!.UnselectFeature(_selectedFeature);
                }

                if (identifyLayerResults.Count == 0 || identifyLayerResults[0].GeoElements.Count == 0)
                {
                    throw new Exception("�t�B�[�`����������܂���");
                }

                // IdentifyLayersAsync() �Ŏ擾�����t�B�[�`����I������i�n�C���C�g�\������j
                _selectedFeature = (Feature)identifyLayerResults[0].GeoElements[0];
                _selectedLayer = (FeatureLayer)identifyLayerResults[0].LayerContent;
                _selectedLayer.SelectFeature(_selectedFeature);

                // ���C���[�̑������̃t�B�[���h���ƃG�C���A�X���擾����
                // Key �Ƀt�B�[���h���AValue �Ƀt�B�[���h�̃G�C���A�X���i�[�����f�B�N�V���i�����쐬����
                var fields = new Dictionary<string, string>();
                for (int i = 0; i < _selectedLayer.FeatureTable!.Fields.Count; i++)
                {
                    Field? field = _selectedLayer.FeatureTable.Fields[i];
                    fields.Add(field.Name, field.Alias);
                }

                // �R�[���A�E�g�̃R���e���c�i�^�C�g���j��ݒ肷��
                string layerName = _selectedLayer.Name;

                // �Y���t�B�[�`���̑S�Ă̑�����������������������쐬����
                var attributes = new System.Text.StringBuilder();
                if (_selectedFeature.Attributes.Count > 0)
                {
                    foreach (var attribute in _selectedFeature.Attributes)
                    {
                        // �Y���t�B�[���h�̃G�C���A�X���擾����iattribute.Key �̓t�B�[���h���j
                        string fieldName = fields[attribute.Key];
                        // �Y���t�B�[���h�̃t�B�[���h�l���擾����
                        object fieldValue = attribute.Value!;
                        attributes.AppendLine(fieldName + ": " + fieldValue);
                    }
                    attributes.AppendLine();
                }

                // �R�[���A�E�g�̃R���e���c���`���� CalloutDefinition ���쐬����
                calloutDefinition = new CalloutDefinition(layerName, attributes.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "�G���[");
            }
            return calloutDefinition!;
        }


    }
}
