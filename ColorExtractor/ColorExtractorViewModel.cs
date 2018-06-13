using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ColorExtractor
{
    public class ColorExtractorViewModel : INotifyPropertyChanged
    {
        string path;
        BitmapSource resultImage;

        public string Path { get => path; set => SetField(ref path, value); }
        public BitmapSource ResultImage { get => resultImage; set => SetField(ref resultImage, value); }

        public ColorExtractorViewModel()
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public virtual void OnPropertyChanged(string PropertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        public bool SetField<T>(ref T Field, T Value, [CallerMemberName]string PropertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(Field, Value))
                return false;
            Field = Value;
            OnPropertyChanged(PropertyName);
            return true;
        }
    }
}
