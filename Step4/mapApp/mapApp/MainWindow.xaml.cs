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

    }
}
