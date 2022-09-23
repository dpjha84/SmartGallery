using ImageClassification;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
using ImageClassification.ImageDataStructures;
using System.Windows.Controls.Primitives;
using CustomModelFaceClassificatin;

namespace SmartGallery.UI
{
    public class ImageDetails
    {
        /// <summary>
        /// A name for the image, not the file name.
        /// </summary>
        public string Name { get; set; }
 
        /// <summary>
        /// A description for the image.
        /// </summary>
        public string Description { get; set; }
 
        /// <summary>
        /// Full path such as c:\path\to\image.png
        /// </summary>
        public string Path { get; set; }
 
        /// <summary>
        /// The image file name such as image.png
        /// </summary>
        public string FileName { get; set; }
 
        /// <summary>
        /// The file name extension: bmp, gif, jpg, png, tiff, etc...
        /// </summary>
        public string Extension { get; set; }
         
        /// <summary>
        /// The image height
        /// </summary>
        public int Height { get; set; }
 
        /// <summary>
        /// The image width.
        /// </summary>
        public int Width { get; set; }
 
        /// <summary>
        /// The file size of the image.
        /// </summary>
        public long Size { get; set; }

        public string PredictedLabel { get; set; }
         
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        IEnumerable<ImageNetDataProbability> predictions;
        ObservableCollection<ImageDetails> images = new ObservableCollection<ImageDetails>();
        ImageData[] result2;
        //System.Windows.Threading.DispatcherTimer _timer = new System.Windows.Threading.DispatcherTimer();
        public MainWindow()
        {
            InitializeComponent();
        //    _timer.Interval = TimeSpan.FromSeconds(0.2); //wait for the other click for 200ms
        //_timer.Tick += _timer_Tick;
            predictions = Predictor.Predict();

            var predictinLient = new FaceClassification();
            //var result1 = predictinLient.GetImagesForALabel("Elizabeth", @"C:\Users\anjaney.shrivastava\Pictures\Photos\Queen");
            result2 = predictinLient.GetImagesPredictions(@"C:\Hack\images\Humans");
        }
 
        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            //string root = @"C:\Hack\DeepLearning_ImageClassification_Training\ImageClassification.Train\assets\inputs\images\flower_photos_small_set\flower_photos_small_set";//System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string root = @"C:\Hack";
            txtImageFolder.Text = root;
            string[] supportedExtensions = new[] { ".bmp", ".jpeg", ".jpg", ".png", ".tiff" };
            var files = Directory.GetFiles(root, "*.*", SearchOption.AllDirectories).Where(s => supportedExtensions.Contains(System.IO.Path.GetExtension(s).ToLower()));

            ObservableCollection<ImageNetDataProbability> predictedImages = new ObservableCollection<ImageNetDataProbability>();
            
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
 
                // I couldn't find file size in BitmapImage
                FileInfo fi = new FileInfo(file.ImagePath);
                id.Size = fi.Length;
                images.Add(id);
            }
            foreach (var item in result2)
            {
                images.Add(new ImageDetails {Path = item.ImagePath, PredictedLabel = item.Label});
            }
            lb.ItemsSource = images;
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            lb.ItemsSource = images.Where(x => x.PredictedLabel.Contains(txtSearch.Text,StringComparison.InvariantCultureIgnoreCase));
        }

        //bool shown = false;
        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
            {
                //if(shown)
                //        return;
                var b = sender as Border;
                if(b != null)
                    new ImageWindow(((ImageDetails)b.DataContext).Path).ShowDialog();
                //shown = true;
                else
                {
                    //if(shown)
                    //    return;
                    var img = sender as Image;
                    new ImageWindow(((ImageDetails)img.DataContext).Path).ShowDialog();
                    //shown = true;
                }
            }
        }
    }
}
