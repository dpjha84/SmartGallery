using ImageClassification;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ImageClassification.ImageDataStructures;
using CustomModelFaceClassificatin;
using System.Threading.Tasks;
using System.Diagnostics;

namespace SmartGallery.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        IEnumerable<ImageNetDataProbability> predictions;
        ObservableCollection<ImageDetails> images = new ObservableCollection<ImageDetails>();
        Stopwatch sw = new Stopwatch();
        ImageData[] result2;
        ObservableCollection<ImageNetDataProbability> predictedImages = new ObservableCollection<ImageNetDataProbability>();
        string imagesFolder = "C:\\Users\\divya.jha\\Pictures";

        public MainWindow()
        {
            InitializeComponent();
            imagesFolder = txtImageFolder.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            lblProgress.Content = "Processing images...";
            Process();
        }
        private void Process()
        {
            Task.Run(() =>
            {
                Dispatcher.Invoke(new Action(() => lblProgress.Content = "Processing images..."));
                predictions = Predictor.Predict(imagesFolder);

                result2 = new FaceClassification().GetImagesPredictions(@"C:\Hack\images\Humans");
                Load();
                Dispatcher.Invoke(new Action(() => lblProgress.Content = ""));
            });
        }

        private void Load()
        {
            images = new ObservableCollection<ImageDetails>();
            //Dispatcher.Invoke(new Action(() => images.Clear()));
            foreach (var file in predictions)
            {
                ImageDetails id = new ImageDetails()
                {
                    Path = file.ImagePath,
                    FileName = System.IO.Path.GetFileName(file.ImagePath),
                    Extension = System.IO.Path.GetExtension(file.ImagePath)
                };
 
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.UriSource = new Uri(file.ImagePath, UriKind.Absolute);
                img.EndInit();
                id.Width = img.PixelWidth;
                id.Height = img.PixelHeight;
                id.PredictedLabel = file.PredictedLabel;
 
                FileInfo fi = new FileInfo(file.ImagePath);
                id.Size = fi.Length;
                images.Add(id);
            }
            foreach (var item in result2)
            {
                images.Add(new ImageDetails { Path = item.ImagePath, PredictedLabel = item.Label });
            }
            Dispatcher.Invoke(new Action(() => lb.ItemsSource = images));
            //lb.ItemsSource = images;
            //lb.Items.Refresh();
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            lb.ItemsSource = images.Where(x => x.PredictedLabel.Contains(txtSearch.Text,StringComparison.InvariantCultureIgnoreCase));
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
            {
                var b = sender as Border;
                if(b != null)
                    new ImageWindow(((ImageDetails)b.DataContext).Path).ShowDialog();
                else
                {
                    var img = sender as Image;
                    new ImageWindow(((ImageDetails)img.DataContext).Path).ShowDialog();
                }
            }
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            if (dialog.ShowDialog(this).GetValueOrDefault() && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                imagesFolder = txtImageFolder.Text = dialog.SelectedPath;
                //lblProgress.Content = "Processing images...";
                //lblProgress.Content = "Processing images...";
                //Process();
                //lblProgress.Content = "";
            }
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            Process();
        }
    }
}
