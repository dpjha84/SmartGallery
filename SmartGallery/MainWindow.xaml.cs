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
        IEnumerable<ImageClassification.DataModels.ImageData> predictionsAtRefresh;
        ObservableCollection<ImageDetails> images = new ObservableCollection<ImageDetails>();
        Stopwatch sw = new Stopwatch();
        ObservableCollection<ImageNetDataProbability> predictedImages = new ObservableCollection<ImageNetDataProbability>();
        string imagesFolder = "C:\\Users\\divya.jha\\Pictures";

        public MainWindow()
        {
            InitializeComponent();
            imagesFolder = txtImageFolder.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            FirstLoad();
        }

        private void FirstLoad()
        {
            //Refresh();
            Task.Run(() =>
            {
                Dispatcher.Invoke(new Action(() => lblProgress.Content = "Processing images..."));
                predictions = Predictor.Predict(imagesFolder);
                Load();
                Dispatcher.Invoke(new Action(() => lblProgress.Content = ""));
            });
        }

        private void Refresh()
        {
            Task.Run(() =>
            {
                Dispatcher.Invoke(new Action(() => lblProgress.Content = "Refreshing images..."));
                predictionsAtRefresh = ImageClassification.Train.CustomModelPhotoRecognition.GetPredictionsForImagesCustomModelSaved(imagesFolder);

                Load(true);
                Dispatcher.Invoke(new Action(() => lblProgress.Content = ""));
            });
        }

        private void Load(bool refresh = false)
        {
            images = new ObservableCollection<ImageDetails>();
            //Dispatcher.Invoke(new Action(() => images.Clear()));
            if(!refresh)
            {
                foreach (var file in predictions)
                {
                    ImageDetails id = new ImageDetails()
                    {
                        Path = file.ImagePath,
                        FileName = System.IO.Path.GetFileName(file.ImagePath),
                        Extension = System.IO.Path.GetExtension(file.ImagePath)
                    };

                    //MemoryStream ms = new MemoryStream();
                    //BitmapImage img = new BitmapImage();
                    //byte[] bytArray = File.ReadAllBytes(@"test.jpg");
                    //ms.Write(bytArray, 0, bytArray.Length);ms.Position = 0;
                    //img.BeginInit();
                    //img.StreamSource = ms;
                    //img.EndInit();
                    //img.Source = img;
 
                    //BitmapImage img = new BitmapImage();
                    //img.BeginInit();
                    //img.CacheOption = BitmapCacheOption.OnLoad;
                    //img.UriSource = new Uri(file.ImagePath, UriKind.Absolute);
                    //img.EndInit();
                    BitmapImage img = BitmapFromUri(new Uri(file.ImagePath, UriKind.Absolute));

                    id.Width = img.PixelWidth;
                    id.Height = img.PixelHeight;
                    id.PredictedLabel = id.CustomLabel = file.PredictedLabel;
 
                    FileInfo fi = new FileInfo(file.ImagePath);
                    id.Size = fi.Length;
                    images.Add(id);
                }
            }
            else
            {
                foreach (var file in predictionsAtRefresh)
                {
                    ImageDetails id = new ImageDetails()
                    {
                        Path = file.ImagePath,
                        FileName = System.IO.Path.GetFileName(file.ImagePath),
                        Extension = System.IO.Path.GetExtension(file.ImagePath)
                    };

                    //MemoryStream ms = new MemoryStream();
                    //BitmapImage img = new BitmapImage();
                    //byte[] bytArray = File.ReadAllBytes(@"test.jpg");
                    //ms.Write(bytArray, 0, bytArray.Length);ms.Position = 0;
                    //img.BeginInit();
                    //img.StreamSource = ms;
                    //img.EndInit();
                    //img.Source = img;
 
                    //BitmapImage img = new BitmapImage();
                    //img.BeginInit();
                    //img.CacheOption = BitmapCacheOption.OnLoad;
                    //img.UriSource = new Uri(file.ImagePath, UriKind.Absolute);
                    //img.EndInit();
                    BitmapImage img = BitmapFromUri(new Uri(file.ImagePath, UriKind.Absolute));

                    id.Width = img.PixelWidth;
                    id.Height = img.PixelHeight;
                    id.PredictedLabel = id.CustomLabel = file.Label;
 
                    FileInfo fi = new FileInfo(file.ImagePath);
                    id.Size = fi.Length;
                    images.Add(id);
                }
            }
            //foreach (var item in result2)
            //{
            //    images.Add(new ImageDetails { Path = item.ImagePath, PredictedLabel = item.Label });
            //}
            Dispatcher.Invoke(new Action(() => lb.ItemsSource = images));
            //lb.ItemsSource = images;
            //lb.Items.Refresh();
        }

        public static BitmapImage BitmapFromUri(Uri source)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = source;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            return bitmap;
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
            FirstLoad();
        }

        private void btnSaveLabels_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<string, List<string>> labelAndPaths = new Dictionary<string, List<string>>();
            foreach (var item in images)
            {
                item.CustomLabel = item.CustomLabel?? item.PredictedLabel;
                var list = labelAndPaths.ContainsKey(item.CustomLabel) ? labelAndPaths[item.CustomLabel] : new List<string>();
                list.Add(item.Path);
                labelAndPaths[item.CustomLabel] = list;
            }
            ImageClassification.Train.CustomModelPhotoRecognition.TrainAndSaveModel(labelAndPaths);
            Refresh();
        }

        private void txtCustomLabel_TextChanged(object sender, TextChangedEventArgs e)
        {
            var img = images.FirstOrDefault(x => x.Path == ((SmartGallery.UI.ImageDetails)((System.Windows.FrameworkElement)sender).DataContext).Path);
            img.CustomLabel = (sender as TextBox).Text;
            //((ImageDetails)((FrameworkElement)sender).DataContext).Path
        }

        private void btnTrain_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<string, List<string>> labelAndPaths = new Dictionary<string, List<string>>();
            foreach (var item in images)
            {
                item.CustomLabel = item.CustomLabel?? item.PredictedLabel;
                var list = labelAndPaths.ContainsKey(item.CustomLabel) ? labelAndPaths[item.CustomLabel] : new List<string>();
                list.Add(item.Path);
                labelAndPaths[item.CustomLabel] = list;
            }
            ImageClassification.Train.CustomModelPhotoRecognition.TrainAndSaveModel(labelAndPaths);
            Refresh();
        }
    }
}
