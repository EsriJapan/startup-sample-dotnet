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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace mapApp
{

    // ���C���[ ���X�g�ɕ\�����郌�C���[����}��摜�Ȃǂ� DataContext �v���p�e�B
    public class LayerInfo
    {
        public LayerInfo()
        {
            this.SymbolMembers = new ObservableCollection<SymbolInfo>();
        }
        public string? LayerName { get; set; }
        public string? LayerID { get; set; }
        public bool? LayerChecked { get; set; }
        public ObservableCollection<SymbolInfo>? SymbolMembers { get; set; }
    }

    public class SymbolInfo
    {
        public System.Windows.Media.Imaging.BitmapFrame? SymbolImage { get; set; }
        public string? SymbolName { get; set; }
    }

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

        private ObservableCollection<LayerInfo>? _layers;
        public ObservableCollection<LayerInfo>? Layers
        {
            get { return _layers; }
            set
            {
                _layers = value;
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
            // Web �}�b�v�� ID ���g�p���ă}�b�v���擾����
            ArcGISPortal portal = await ArcGISPortal.CreateAsync();
            PortalItem mapItem = await PortalItem.CreateAsync(portal, "3bf36dae4cc545c8956a521fa9ada1eb");

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

            // �x�[�X�}�b�v�̃��C���[����ύX
            localizedBasemap.BaseLayers[0].Name = "�A�e�N���}";
            localizedBasemap.BaseLayers[1].Name = "�n�`�}";


            // ���C���[ ���X�g���쐬����
            CreateToc();


            // �}�b�v�̃X�P�[���͈͂�ݒ肷��
            Map.MinScale = 5000000;
            Map.MaxScale = 5000;

            // �Z���������ʂ̕\���p�̐V���� GraphicsOverlay ���쐬����
            GraphicsOverlay geocodeResultOverlay = new GraphicsOverlay();
            // GraphicsOverlays (GraphicsOverlayCollection) �v���p�e�B��ݒ肷��
            GraphicsOverlayCollection graphicsOverlays = new GraphicsOverlayCollection
            {
               geocodeResultOverlay
            };
            this.GraphicsOverlays = graphicsOverlays;

            // �Z�������p�� LocatorTask ���쐬����
            Uri _serviceUri = new Uri("https://geocode-api.arcgis.com/arcgis/rest/services/World/GeocodeServer");
            _geocoder = await LocatorTask.CreateAsync(_serviceUri);


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

        public async Task<MapPoint> SearchAddress(string searchText)
        {
            MapPoint? addressLocation = null;
            try
            {
                // �����̌������ʂ̃O���t�B�b�N�X����������
                GraphicsOverlay? geocodeResultOverlay = this.GraphicsOverlays?.FirstOrDefault();
                geocodeResultOverlay!.Graphics.Clear();

                // �e�L�X�g�{�b�N�X�̒l���� LocatorTask �������̏ꍇ�́A�����𒆎~����
                if (String.IsNullOrWhiteSpace(searchText) || _geocoder == null)
                {
                    throw new Exception("���������񂪋󂩁A�W�I�R�[�_�[�������ł�");
                }

                // �������ʂ�1�i�}�b�`�����ł������Z���j�����ɐ�������A�W�I�R�[�f�B���O�̃p�����[�^�[���쐬����
                GeocodeParameters geocodeParameters = new GeocodeParameters();
                geocodeParameters.MaxResults = 1;

                //���͕����񂩂�W�I�R�[�f�B���O�����s���}�b�`����Z���i�Z��������ƈʒu�j���擾����
                IReadOnlyList<GeocodeResult> addresses = await _geocoder.GeocodeAsync(searchText, geocodeParameters);

                // �������ʂ��擾�ł��Ȃ��ꍇ�͏����𒆎~����
                if (addresses.Count < 1)
                {
                    throw new Exception("��v���錋�ʂ�����܂���");
                }

                // �Z���̈ʒu�ɕ\������|�C���g �V���{�����쐬����
                SimpleMarkerSymbol pointSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, System.Drawing.Color.Red, 15);

                // �Z��������̃e�L�X�g �V���{�����쐬����
                TextSymbol textSymbol = new TextSymbol(addresses.First().Label, System.Drawing.Color.Yellow, 15, Esri.ArcGISRuntime.Symbology.HorizontalAlignment.Center, Esri.ArcGISRuntime.Symbology.VerticalAlignment.Bottom);

                textSymbol.HaloColor = System.Drawing.Color.Gray;
                textSymbol.HaloWidth = 3;
                textSymbol.OffsetY = 10;

                // �|�C���g�ƃe�L�X�g �V���{������O���t�B�b�N���쐬����
                Graphic pointGraphic = new Graphic(addresses.First().DisplayLocation, pointSymbol);
                Graphic textGraphic = new Graphic(addresses.First().DisplayLocation, textSymbol);

                // GraphicsOverlay �ɃO���t�B�b�N��ǉ�����
                geocodeResultOverlay.Graphics.Add(pointGraphic);
                geocodeResultOverlay.Graphics.Add(textGraphic);

                // �}�b�v���Z���̈ʒu�ɃY�[�����邽�߂̃|�C���g��Ԃ�
                addressLocation = addresses.First().DisplayLocation;
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "�G���[");
            }

            return addressLocation!;
        }


        public async void CreateToc()
        {
            long layerCount = 0;
            // TreeView �Ƀo�C���h����l��ێ����邽�߂̃��C���[�����i�[���� LayerInfo �̃R���N�V�������쐬����
            Layers = new ObservableCollection<LayerInfo>();

            // �}�b�v�Ɋi�[����Ă��郌�C���[�̈ꗗ���擾����
            foreach (var layer in Map.AllLayers)
            {
                // �}�b�v�Ɋi�[����Ă��郌�C���[�Ƀ��j�[�N�� ID ��ݒ肷��i���C���[�̕\��/��\���@�\����������ۂɎg�p����j
                layer.Id = layerCount.ToString();

                // ���C���[�� (Layer �I�u�W�F�N�g����擾) �ƃ��C���[ ID ���Z�b�g���āALayerInfo �I�u�W�F�N�g���쐬����
                LayerInfo layerInfo = new LayerInfo()
                {
                    LayerName = layer.Name,
                    LayerID = layer.Id,
                    LayerChecked = layer.IsVisible
                };

                // ���C���[�ɐݒ肳��Ă���V���{����� (�V���{���摜�Ƃ��̖���) ���擾����
                var legendInfos = await layer.GetLegendInfosAsync();
                if (legendInfos != null)
                {
                    foreach (LegendInfo item in legendInfos)
                    {
                        // �V���{���� BitmapFrame �^�ɕϊ����摜�Ƃ��Ď擾����
                        Symbol symbol = item.Symbol!;
                        RuntimeImage swatch = await symbol!.CreateSwatchAsync(1 * 96);
                        Stream imageData = await swatch.GetEncodedBufferAsync();
                        System.Drawing.Image image = Bitmap.FromStream(imageData);

                        // �������X�g���[���� BitmapFrame ��ۑ����� LayerInfo �I�u�W�F�N�g�ɃV���{���̉摜�Ɩ��̂��Z�b�g����
                        using (Stream stream = new MemoryStream())
                        {
                            image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                            stream.Seek(0, SeekOrigin.Begin);

                            BitmapFrame symbolImage = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                            SymbolInfo symbolInfo = new SymbolInfo()
                            {
                                SymbolImage = symbolImage,
                                SymbolName = item.Name
                            };
                            layerInfo.SymbolMembers!.Insert(0, symbolInfo);
                        }
                    }
                }
                // TreeView �R���e���c�̃R���N�V�����̈�ԏ�� LayerInfo �I�u�W�F�N�g��ǉ�����
                Layers.Insert(0, layerInfo);
                layerCount = layerCount + 1;
            }
        }

        // ���C���[�̕\��/��\���̐ؑւ�
        public void setVisibleLayer(string checkedLayerId)
        {
            foreach (Layer layer in Map.AllLayers)
            {
                if (layer.Id == checkedLayerId)
                {
                    if (layer.IsVisible)
                    {
                        // ���C���[���\������Ă���ꍇ�͔�\���ɂ���
                        layer.IsVisible = false;
                    }
                    else
                    {
                        // ���C���[����\���̏ꍇ�͕\������
                        layer.IsVisible = true;
                    }
                }
            }
        }



    }
}
