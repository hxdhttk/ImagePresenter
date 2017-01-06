using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace ImagePicker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static ObservableCollection<WallPaperImage> WallPaperImages { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            Preparation();
        }

        private void FolderBrowseButton_OnClick(object sender, RoutedEventArgs e)
        {
            var folderBrowserDialog = new FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }
            PathTextBox.Text = folderBrowserDialog.SelectedPath;

            if (!string.IsNullOrEmpty(PathTextBox.Text) && !string.IsNullOrWhiteSpace(PathTextBox.Text))
            {
                OkButton.IsEnabled = true;
            }
        }
        
        private void PathTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!Directory.Exists(PathTextBox.Text)) return;

            WallPaperImages.Clear();
            var di = new DirectoryInfo(PathTextBox.Text);
            var fc = di.EnumerateFiles();
            var imageFiles = from f in fc
                              where f.Extension == ".jpg" ||
                                    f.Extension == ".png" ||
                                    f.Extension == ".bmp"
                              select f;
            if (!imageFiles.Any()) return;

            foreach (var imageFile in imageFiles)
            {
                WallPaperImages.Add(new WallPaperImage { Path = imageFile.FullName });
            }

            ImagesListBox.ItemsSource = WallPaperImages;
        }
        
        private void Preparation()
        {
            Height = MinHeight;
            Width = MinWidth;

            PathTextBox.Text = String.Empty;
            OkButton.IsEnabled = false;
            
            WallPaperImages = new ObservableCollection<WallPaperImage>();
            ImagesListBox.ItemsSource = WallPaperImages;
        }
    }

    public class WallPaperImage
    {
        public string Path { get; set; }

        public string FileName
        {
            get
            {
                var fi = new FileInfo(Path);
                return fi.Name;
            }
        }
    }
}
