using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
using System.Windows.Threading;

namespace ColorExtractor
{
    public partial class MainWindow : Window
    {
        public new ColorExtractorViewModel DataContext => (ColorExtractorViewModel)base.DataContext;

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Browse for an image
        /// </summary>
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.CheckFileExists = true;
            openFileDialog.DefaultExt = "Image (.jpg,.png,.bmp)|*.jpg,*.png,*.bmp";
            if (openFileDialog.ShowDialog() == true)
            {
                DataContext.Path = openFileDialog.FileName;
            }
        }
        /// <summary>
        /// Process the selected image
        /// </summary>
        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            var Path = DataContext.Path;
            
            var Colors = await Task.Run(() =>
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => Enable(false)));

                List<Color> ResultColors = null;
                try
                {
                    var InputImage = new BitmapImage(new Uri(Path));

                    ResultColors = ExtractColors(InputImage);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => Enable(true)));
                }

                return ResultColors;
            });

            if (Colors != null)
            {
                var ResultWidth = (int)Math.Floor(Math.Sqrt((double)Colors.Count));
                var ResultHeight = (int)Math.Ceiling((double)((double)Colors.Count / ResultWidth));
                var ResultPixelFormat = PixelFormats.Bgr32;
                var ResultPixels = Colors.SelectMany(C => new byte[] { C.R, C.G, C.B, C.A }).ToList();
                var ResultStride = ResultWidth * (ResultPixelFormat.BitsPerPixel / 8);

                ResultPixels.AddRange(new byte[((ResultWidth * ResultHeight * (ResultPixelFormat.BitsPerPixel / 8)) - ResultPixels.Count)]);

                var ResultImage = BitmapSource.Create(ResultWidth, ResultHeight, 96, 96, ResultPixelFormat, null, ResultPixels.ToArray(), ResultStride);
                RenderOptions.SetBitmapScalingMode(ResultImage, BitmapScalingMode.NearestNeighbor);

                DataContext.ResultImage = ResultImage;
            }
        }
        /// <summary>
        /// Save the result
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            if (saveFileDialog.ShowDialog() == true)
            {
                using (var fileStream = new FileStream(saveFileDialog.FileName, FileMode.Create))
                {
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(DataContext.ResultImage));
                    encoder.Save(fileStream);
                }
            }
        }
        /// <summary>
        /// Enables/Disables the interface
        /// </summary>
        private void Enable(bool Enable = true)
        {
            PathBox.IsEnabled = Enable;
            BrowseButton.IsEnabled = Enable;
            StartButton.IsEnabled = Enable;
            SaveButton.IsEnabled = Enable;
        }

        /// <summary>
        /// Extracts the colors of the provided SourceImage
        /// </summary>
        /// <param name="SourceImage">Image to extract the colors of</param>
        /// <returns>Colors of the image, ordered by perceived brightness</returns>
        private List<Color> ExtractColors(BitmapImage SourceImage)
        {
            List<Color> Colors = new List<Color>();

            int stride = (int)SourceImage.PixelWidth * (SourceImage.Format.BitsPerPixel / 8);
            byte[] pixels = new byte[(int)SourceImage.PixelHeight * stride];
            SourceImage.CopyPixels(pixels, stride, 0);

            for (int y = 0; y < SourceImage.PixelHeight; y++)
            {
                for (int x = 0; x < SourceImage.PixelWidth; x++)
                {
                    int index = y * stride + 4 * x;
                    byte red = pixels[index];
                    byte green = pixels[index + 1];
                    byte blue = pixels[index + 2];
                    byte alpha = pixels[index + 3];

                    //if (Colors.Contains(Color.FromArgb(alpha, red, green, blue)) == false)
                    Colors.Add(Color.FromArgb(alpha, red, green, blue));
                }
            }

            Colors = Colors.Distinct().OrderBy(C => PerceivedBrightness(C.R, C.G, C.B, C.A)).Reverse().ToList();

            return Colors;
        }
        /// <summary>
        /// Calculates the perceived brightness of the given color
        /// </summary>
        private float PerceivedBrightness(float R, float G, float B, float A) => (float)Math.Sqrt(R * R * .241 + G * G * .691 + B * B * .068) * A;
    }
}
