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
            var Sort = DataContext.SelectedSort;

            var Colors = await Task.Run(() =>
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => Enable(false)));

                List<Color> ResultColors = null;
                try
                {
                    var InputImage = new BitmapImage(new Uri(Path));

                    ResultColors = ExtractColors(InputImage, Sort);
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
        private List<Color> ExtractColors(BitmapImage SourceImage, Sort Sort)
        {
            List<Color> Colors = new List<Color>();

            if (SourceImage.Format == PixelFormats.Indexed1 || SourceImage.Format == PixelFormats.Indexed2 || SourceImage.Format == PixelFormats.Indexed4 || SourceImage.Format == PixelFormats.Indexed8)
                throw new Exception("Indexed PixelFormats are currently not supported");

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

                    Colors.Add(Color.FromArgb(alpha, red, green, blue)); //Note: Probably a little slow to create a Color
                }
            }

            Colors = Colors.Distinct().ToList();

            //Note: Probably a little slow
            switch (Sort)
            {
                case Sort.HueSaturationLightness:
                    Colors = Colors.OrderBy(C => RGBtoHSL(C.R, C.G, C.B)).Reverse().ToList();
                    break;
                case Sort.HuePerceivedBrightness:
                    Colors = Colors.OrderBy(C => RGBtoHSL(C.R, C.G, C.B).Item1).ThenBy(C => PerceivedBrightness(C.R, C.G, C.B, C.A)).Reverse().ToList();
                    break;
                case Sort.PerceivedBrightness:
                    Colors = Colors.OrderBy(C => PerceivedBrightness(C.R, C.G, C.B, C.A)).Reverse().ToList();
                    break;
                case Sort.Hue:
                    Colors = Colors.OrderBy(C => RGBtoHSL(C.R, C.G, C.B).Item1).Reverse().ToList();
                    break;
            }

            return Colors;
        }

        /// <summary>
        /// Converts RGB to HSL
        /// </summary>
        /// <seealso cref="https://stackoverflow.com/a/11923973/2526818"/>
        private (float,float,float) RGBtoHSL(float R, float G, float B)
        {
            var RFloat = R / 255f;
            var GFloat = G / 255f;
            var BFloat = B / 255f;

            var Min = (float)Math.Min(RFloat, (float)Math.Min(GFloat, BFloat));
            var Max = (float)Math.Max(RFloat, (float)Math.Max(GFloat, BFloat));

            float H = 0, S = 0;
            var L = (Max + Min) / 2;

            if (Max == Min)
                H = S = 0;
            else
            {
                var d = Max - Min;

                S = L > 0.5 ? d / (2 - Max - Min) : d / (Max + Min);

                if (Max == RFloat)
                    H = (GFloat - BFloat) / d + (GFloat < BFloat ? 6 : 0);
                else if (Max == GFloat)
                    H = (BFloat - RFloat) / d + 2;
                else
                    H = (RFloat - GFloat) / d + 4;

                H /= 6;
            }

            return (H * 360, S * 100, L * 100);
        }

        /// <summary>
        /// Calculates the perceived brightness of the given color
        /// </summary>
        /// <seealso cref="http://www.nbdtech.com/Blog/archive/2008/04/27/Calculating-the-Perceived-Brightness-of-a-Color.aspx"/>
        private float PerceivedBrightness(float R, float G, float B, float A) => (float)Math.Sqrt(R * R * .241 + G * G * .691 + B * B * .068) * A;
    }
}
