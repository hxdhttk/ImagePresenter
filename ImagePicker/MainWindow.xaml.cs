using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using MessageBox = System.Windows.MessageBox;
using ThreadState = System.Threading.ThreadState;
using Image = System.Drawing.Image;

namespace ImagePicker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly string ServerSettingsFile = Path.Combine(Directory.GetCurrentDirectory(), "ServerSettings.json");
        private static readonly string ServerPath = Path.Combine(Directory.GetCurrentDirectory(), "ImagePresenter.Server.exe");

        private Thread _startServerThread;
        private Thread StartServerThread
        {
            get { return _startServerThread ?? (_startServerThread = new Thread(StartServer) { Name = $"{nameof(StartServerThread)}" }); }
            set { _startServerThread = value; }
        }

        public static ObservableCollection<WallPaperImage> WallPaperImages { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            Preparation();
        }

        private void Preparation()
        {
            Height = MinHeight;
            Width = MinWidth;

            PathTextBox.Text = string.Empty;
            OkButton.IsEnabled = false;
            StartButton.IsEnabled = false;
            StopButton.IsEnabled = false;

            WallPaperImages = new ObservableCollection<WallPaperImage>();
            ImagesListView.ItemsSource = WallPaperImages;
        }

        private void GenerateWallPaperImages()
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

            ImagesListView.ItemsSource = WallPaperImages;
        }

        private void StartServer()
        {
            var psi = new ProcessStartInfo(ServerPath)
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };
            var p = Process.Start(psi);
            p.WaitForExit();
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
            GenerateWallPaperImages();
        }

        private void OkButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(PathTextBox.Text)) return;

            if (!WallPaperImages.Any())
            {
                var res1 = MessageBox.Show(this, "No image found in current directory.", "Image Picker Message");
                if (res1 == MessageBoxResult.OK || res1 == MessageBoxResult.Cancel)
                {
                    StartButton.IsEnabled = false;
                    return;
                }
            }

            var settings = new FileInfo(ServerSettingsFile);
            if (settings.Exists)
            {
                settings.Attributes = FileAttributes.Normal;
                settings.Delete();
            }

            var serverSettings = new ServerSettings { ImagePath = PathTextBox.Text, Port = 13017 };
            using (var sw = new StreamWriter(settings.FullName))
            {
                var str = JsonConvert.SerializeObject(serverSettings);
                sw.Write(str);
            }

            var res = MessageBox.Show(this, "Server settings generated.", "Image Picker Message");
            if (res == MessageBoxResult.OK || res == MessageBoxResult.Cancel)
            {
                StartButton.IsEnabled = true;
            }
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(ServerSettingsFile)) return;

            var settings = new ServerSettings();
            using (var sr = new StreamReader(ServerSettingsFile))
            {
                var str = sr.ReadToEnd();
                settings = JsonConvert.DeserializeObject<ServerSettings>(str);
            }
            if (settings.ImagePath == null) return;

            PathTextBox.Text = settings.ImagePath;
            GenerateWallPaperImages();

            OkButton.IsEnabled = true;
            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;
        }

        private void StartButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(ServerSettingsFile))
            {
                var res = MessageBox.Show(this, "Server settings not found.", "Image Picker Message");
                if (res == MessageBoxResult.OK || res == MessageBoxResult.Cancel)
                {
                    StartButton.IsEnabled = false;
                    return;
                }
            }

            var settings = new ServerSettings();
            using (var sr = new StreamReader(ServerSettingsFile))
            {
                var str = sr.ReadToEnd();
                settings = JsonConvert.DeserializeObject<ServerSettings>(str);
            }
            if (settings.ImagePath == null || !WallPaperImages.Any())
            {
                var res = MessageBox.Show(this, "Server settings corrupted.", "Image Picker Message");
                if (res == MessageBoxResult.OK || res == MessageBoxResult.Cancel)
                {
                    StartButton.IsEnabled = false;
                    return;
                }
            }

            StartServerThread.Start();

            OkButton.IsEnabled = false;
            StartButton.IsEnabled = false;
            StopButton.IsEnabled = true;
        }

        private void StopButton_OnClick(object sender, RoutedEventArgs e)
        {
            while (StartServerThread.ThreadState != ThreadState.WaitSleepJoin)
            {
                Thread.Sleep(2000);
            }

            StartServerThread.Abort();
            StartServerThread = new Thread(StartServer) { Name = $"{nameof(StartServerThread)}" };
            var p = Process.GetProcessesByName("ImagePresenter.Server").SingleOrDefault();
            p?.Kill();

            OkButton.IsEnabled = true;
            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;
        }
    }

    public class WallPaperImage
    {
        private const int ThumbnailHeight = 90;
        private const int ThumbnailWidth = 160;

        public string Path { get; set; }

        public ImageSource Thumbnail
        {
            get
            {
                Image.GetThumbnailImageAbort dummyCallback = () => false;
                var originalBitmap = new Bitmap(Path);
                var thumbnail = originalBitmap.Width <= originalBitmap.Height ?
                    originalBitmap.GetThumbnailImage(originalBitmap.Width * ThumbnailHeight / originalBitmap.Height, ThumbnailHeight, dummyCallback, IntPtr.Zero) :
                    originalBitmap.GetThumbnailImage(ThumbnailWidth, originalBitmap.Height * ThumbnailWidth / originalBitmap.Width, dummyCallback, IntPtr.Zero);
                return ChangeBitmapToImageSource((Bitmap)thumbnail);
            }
        }

        public string FileName
        {
            get
            {
                var fi = new FileInfo(Path);
                return fi.Name;
            }
        }

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);

        public static ImageSource ChangeBitmapToImageSource(Bitmap bitmap)
        {
            var hBitmap = bitmap.GetHbitmap();

            var wpfBitmap = Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            if (!DeleteObject(hBitmap))
            {
                throw new Win32Exception();
            }

            return wpfBitmap;
        }
    }

    public class ServerSettings
    {
        public int Port { get; set; }

        public string ImagePath { get; set; }
    }
}
