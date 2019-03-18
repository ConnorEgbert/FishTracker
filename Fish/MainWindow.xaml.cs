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
using Window = System.Windows.Window;
using Point = OpenCvSharp.Point;

namespace Fish
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow: System.Windows.Window
    {
        VideoCapture vid;
        double ltime = 0;
        double rtime = 0;
        bool? lastpos;
        double frametime;
        bool processing = false;

        public MainWindow()
        {
            InitializeComponent();

            this.Closed += new EventHandler(MainWindow_Closed);
        }

        private void processVideo()
        {
            
            if (inputfile.Text == "" || processing)
            {
                return;
            }

            processing = true;

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
                processing = false;
                return;
            }

            try
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outputfile.Text));
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "Error");
                processing = false;
            }

            bar.Minimum = 0;
            bar.Maximum = vid.FrameCount;
            frametime = 1 / vid.Fps;
            
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

            Console.WriteLine("{0},{1}", Math.Round(ltime, 2).ToString(), Math.Round(rtime, 2).ToString());

            if (!File.Exists(outputfile.Text))
            {
                // Create a file to write to.
                StreamWriter header = File.CreateText(outputfile.Text);
                try
                {
                    header.Write("Filename,left time,right time\n");
                }
                finally
                {
                    header.Close();
                }
            }

            // Create a file to write to.
            StreamWriter sw = File.AppendText(outputfile.Text);
            try
            {
                sw.Write("{0},{1},{2}\n", System.IO.Path.GetFileName(inputfile.Text), Math.Round(ltime, 2).ToString(), Math.Round(rtime, 2).ToString());
            }
            finally
            {
                sw.Close();
            }

            MessageBox.Show("Time spent on left: " + Math.Round(ltime, 2).ToString() + "\nTime spent on right: " + Math.Round(rtime, 2).ToString() + "\nOutput can also be found in " + outputfile.Text, "Time output");

            vid.Release();
            processing = false;

        }
        private Mat processFrame(Mat srcImage) {

            var binaryImage = new Mat(srcImage.Size(), srcImage.Type());

            Cv2.CvtColor(srcImage, binaryImage, code: ColorConversionCodes.BGR2HSV);

            // Hardcoding this hurts, but that's how things are right now.
            Cv2.InRange(binaryImage, new Scalar(0, 75, 75), new Scalar(65, 255, 255), binaryImage);
            //Cv2.Threshold(binaryImage, binaryImage, thresh: 30, maxval: 50, type: ThresholdTypes.Mask);

            Cv2.GaussianBlur(binaryImage, binaryImage, new OpenCvSharp.Size(1, 1), 10);   //Blur Effect
            Cv2.Dilate(binaryImage, binaryImage, 10);        // Dilate Filter Effect
            Cv2.Erode(binaryImage, binaryImage, 10);         // Erode Filter Effect

            if (disp_blobs.IsChecked ?? false)
            {
                Cv2.ImShow("Blob vision", binaryImage);
            }

            var detectorParams = new SimpleBlobDetector.Params
            {
                FilterByArea = false,
                FilterByCircularity = false,
                FilterByConvexity = false,
                FilterByInertia = false,
                FilterByColor = false,
            };
            var simpleBlobDetector = SimpleBlobDetector.Create(detectorParams);
            var keyPoints = simpleBlobDetector.Detect(binaryImage);

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

                if (largest.Pt.X > srcImage.Width * .5)
                {
                    lastpos = false;
                    rtime += frametime;
                }
                else
                {
                    lastpos = true;
                    ltime += frametime;
                }
            } 
            else
            {
                if (lastpos.HasValue)
                {
                    // Left is true right is false
                    if (lastpos.Value)
                    {
                        ltime += frametime;
                    }
                    else
                    {
                        rtime += frametime;
                    }
                }
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

            Cv2.Line(imageWithKeyPoints, 
                new Point(imageWithKeyPoints.Width * .5, imageWithKeyPoints.Height), 
                new Point(imageWithKeyPoints.Width * .5, 0),
                new Scalar(255, 255, 55));
            Cv2.PutText(imageWithKeyPoints, Math.Round(rtime, 2).ToString(), new Point(imageWithKeyPoints.Width * .8, imageWithKeyPoints.Height * .1), HersheyFonts.HersheySimplex, 1, Scalar.FromRgb(55, 255, 255));
            Cv2.PutText(imageWithKeyPoints, Math.Round(ltime, 2).ToString(), new Point(imageWithKeyPoints.Width * .1, imageWithKeyPoints.Height * .1), HersheyFonts.HersheySimplex, 1, Scalar.FromRgb(55, 255, 255));

            //Cv2.ImShow("Key Points", imageWithKeyPoints);
            if (disp_frame_state.IsChecked ?? false)
            {
                disp.Source = OpenCvSharp.Extensions.BitmapSourceConverter.ToBitmapSource(imageWithKeyPoints);
            }
            
            srcImage.Dispose();
            binaryImage.Dispose();
            return imageWithKeyPoints;
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
                outputfile.Text = System.IO.Path.GetDirectoryName(filename) + "\\timelist.csv";
            }
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            processVideo();
        }

        void MainWindow_Closed(object sender, EventArgs e)
        {
            try
            {
                vid.Release();
            }
            catch {
                ;
            }
            Cv2.DestroyAllWindows();
        }
    }
}
