using OpenCvSharp;
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
using System.Drawing;
using System.IO;
using System.Windows.Threading;
using System.Threading;

namespace Fish
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow: System.Windows.Window
    {
        VideoCapture vid;

        public MainWindow()
        {
            InitializeComponent();

            this.Closed += new EventHandler(MainWindow_Closed);
        }

        private void processVideo()
        {
            if (inputfile.Text == "")
            {
                return;
            }

            try
            {
                vid = new VideoCapture(inputfile.Text);
            }
            catch (Exception e)
            {
                if (e is FileNotFoundException) {
                    MessageBox.Show("Input file not found:\n" + inputfile.Text, "Error");
                }
                else
                {
                    MessageBox.Show(e.ToString(), "Error");
                }
                return;
            }

            bar.Minimum = 0;
            bar.Maximum = vid.FrameCount;
            
            while (true)
            {
                Mat fram = new Mat();
                vid.Read(fram);
                
                if (fram.Empty())
                {
                    break;
                }
                processFrame(fram);
                bar.Value++;
                double progress = 100 * (bar.Value / vid.FrameCount);
                barText.Text = Math.Round(progress, 0, MidpointRounding.AwayFromZero).ToString() + "%";
                Dispatcher.Invoke(new Action(() => { }), DispatcherPriority.ContextIdle, null);
            }

            vid.Release();
        }
        private void processFrame(Mat srcImage) {

            var binaryImage = new Mat(srcImage.Size(), srcImage.Type());
            Mat threshImg = new Mat();

            Cv2.CvtColor(binaryImage, threshImg, code: ColorConversionCodes.BGR2HSV);

            //Cv2.InRange(threshImg, new Scalar(30, 1, 1), new Scalar(50, 255, 255), threshImg);
            //Cv2.Threshold(binaryImage, binaryImage, thresh: 30, maxval: 50, type: ThresholdTypes.Binary);

            //Cv2.GaussianBlur(threshImg, threshImg, , 0);   //Blur Effect
            //Cv2.Dilate(threshImg, threshImg, 0);        // Dilate Filter Effect
            //Cv2.Erode(threshImg, threshImg, 0);         // Erode Filter Effect

            //Cv2.CvtColor(srcImage, binaryImage, ColorConversionCodes.BGRA2GRAY);
            

            Cv2.ImShow("eu", threshImg);

            var detectorParams = new SimpleBlobDetector.Params
            {
                //MinDistBetweenBlobs = 10, // 10 pixels between blobs
                //MinRepeatability = 1,

                //MinThreshold = 0,
                //MaxThreshold = 255,
                //ThresholdStep = 5,

                FilterByArea = false,
                //FilterByArea = true,
                //MinArea = 1000, // 10 pixels squared
                //MaxArea = 50000,

                FilterByCircularity = false,
                //FilterByCircularity = true,
                //MinCircularity = 0.001f,

                FilterByConvexity = false,
                //FilterByConvexity = true,
                //MinConvexity = 0.001f,
                //MaxConvexity = 10,

                FilterByInertia = false,
                //FilterByInertia = true,
                //MinInertiaRatio = 0.001f,

                //FilterByColor = false,
                FilterByColor = true,
                BlobColor = 40
            };
            var simpleBlobDetector = SimpleBlobDetector.Create(detectorParams);
            var keyPoints = simpleBlobDetector.Detect(threshImg);

            KeyPoint largest = new KeyPoint();

            Console.WriteLine("blobs: {0}", keyPoints.Length);
            if (keyPoints.Length != 0) {
                largest = keyPoints[0];

                foreach (var keyPoint in keyPoints)
                {
                    if (keyPoint.Size > largest.Size)
                    {
                        largest = keyPoint;
                    }
                    //Console.WriteLine("X:\t{0},\tY:\t{1},\tSize:\t{2}", Math.Round(keyPoint.Pt.X, 0, MidpointRounding.AwayFromZero), Math.Round(keyPoint.Pt.Y, 0, MidpointRounding.AwayFromZero), Math.Round(keyPoint.Size, 0, MidpointRounding.AwayFromZero));
                }
                Console.WriteLine("Largest key point\n\tX:\t\t{0},\n\tY:\t\t{1},\n\tSize:\t{2}", largest.Pt.X, largest.Pt.Y, largest.Size);
            }
            KeyPoint[] list_converter = new KeyPoint[1];
            list_converter[0] = largest;
            var imageWithKeyPoints = new Mat();
            Cv2.DrawKeypoints(
                    image: srcImage, //binaryImage,
                    keypoints: list_converter,
                    outImage: imageWithKeyPoints,
                    color: Scalar.FromRgb(55, 255, 255),
                    flags: DrawMatchesFlags.DrawRichKeypoints);

            //Cv2.ImShow("Key Points", imageWithKeyPoints);
            if (disp_frame_state.IsChecked ?? false)
            {
                disp.Source = OpenCvSharp.Extensions.BitmapSourceConverter.ToBitmapSource(imageWithKeyPoints);
            }
            
            srcImage.Dispose();
            imageWithKeyPoints.Dispose();
        }

        private void file_select_click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                // Set filter for file extension and default file extension 
                //dlg.DefaultExt = ".mp4";
                Filter = "All files(*.*)|*.*"
            };


            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                inputfile.Text = filename;
                outputfile.Text = System.IO.Path.GetDirectoryName(filename) + "\\processed_" + System.IO.Path.GetFileName(filename);
            }
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            processVideo();
        }

        void MainWindow_Closed(object sender, EventArgs e)
        {
            vid.Release();
        }
    }
}
