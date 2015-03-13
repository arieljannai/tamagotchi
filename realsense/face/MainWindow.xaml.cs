//--------------------------------------------------------------------------------------
// Copyright 2014-2015 Intel Corporation
// All Rights Reserved
//
// Permission is granted to use, copy, distribute and prepare derivative works of this
// software for any purpose and without fee, provided, that the above copyright notice
// and this statement appear in all copies.  Intel makes no representations about the
// suitability of this software for any purpose.  THIS SOFTWARE IS PROVIDED "AS IS."
// INTEL SPECIFICALLY DISCLAIMS ALL WARRANTIES, EXPRESS OR IMPLIED, AND ALL LIABILITY,
// INCLUDING CONSEQUENTIAL AND OTHER INDIRECT DAMAGES, FOR THE USE OF THIS SOFTWARE,
// INCLUDING LIABILITY FOR INFRINGEMENT OF ANY PROPRIETARY RIGHTS, AND INCLUDING THE
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE.  Intel does not
// assume any responsibility for any errors which may appear in this software nor any
// responsibility to update it.
//--------------------------------------------------------------------------------------

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
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;

namespace BlockHead
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainWindow m_form;
        private Thread processingThread;
        private Thread processingThread1;
        private Semaphore sem = new Semaphore(3, 3);

        private PXCMSenseManager senseManager;
        private PXCMFaceModule face = null;
        private PXCMFaceConfiguration faceConfiguration;
        private PXCMFaceConfiguration.ExpressionsConfiguration expressionConfiguration;
        private Int32 numberTrackedFaces;
        Bitmap colorBitmap;
        private int faceRectangleHeight;
        private int faceRectangleWidth;
        private int faceRectangleX;
        private int faceRectangleY;
        private float faceAverageDepth;
        private float headRoll;
        private float headPitch;
        private float headYaw;
        private const int TotalExpressions = 6;
        private int[] expressionScore = new int[TotalExpressions];
        private bool WindowOpen = false;
        private bool FacesDone = false;
        private enum FaceExpression
        {
            None,
            Kiss,
            Open,
            Smile,
            Tongue,
            eyesClose
        };
        private FaceExpression[] expressionArr = new FaceExpression[TotalExpressions];
        public MainWindow()
        {
            InitializeComponent();
            
            expressionArr = new FaceExpression[]{FaceExpression.None, FaceExpression.Smile, FaceExpression.Tongue, FaceExpression.Open, FaceExpression.eyesClose, FaceExpression.Kiss};
            senseManager = PXCMSenseManager.CreateInstance();
            senseManager.EnableStream(PXCMCapture.StreamType.STREAM_TYPE_COLOR, 640, 480, 30);
            senseManager.EnableFace();
            senseManager.Init();
            ConfigureFaceTracking();
            processingThread = new Thread(new ThreadStart(ProcessingThread));
            processingThread.Start();
            //while (WindowOpen)
            //ProcessingThread1();
        }
        private void start()
        {
            while (WindowOpen && !FacesDone)
                getFaces();
        }
        private void ConfigureFaceTracking()
        {
            face = senseManager.QueryFace();
            faceConfiguration = face.CreateActiveConfiguration();
            faceConfiguration.detection.isEnabled = true;

            expressionConfiguration = faceConfiguration.QueryExpressions();
            expressionConfiguration.Enable();
            expressionConfiguration.EnableAllExpressions();

            faceConfiguration.EnableAllAlerts();
            faceConfiguration.ApplyChanges();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            imgFace.Visibility = Visibility.Hidden;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WindowOpen = false;
            processingThread.Abort();
            faceConfiguration.Dispose();
            senseManager.Dispose();
        }

        private void getFaces()
        {
            if (face != null)
            {
                PXCMSenseManager sManager = PXCMSenseManager.CreateInstance();
                sManager.EnableStream(PXCMCapture.StreamType.STREAM_TYPE_COLOR, 640, 480, 30);
                senseManager.EnableFace();
                sManager.Init();
                
                
                for (int i = 0; i < expressionArr.Length; i++)
                {
                    int count = 0;
  
                    //TODO: change headline 
                    this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate()
                        { faceExpressionTitle.Content = "Please do a " + expressionArr[i].ToString() + " face! "; }));
  
                    waitForExpression(expressionArr[i]);
                    // Update the user interface
                    sem.WaitOne();
                    //PXCMImage.ImageData imData;
                    //PXCMCapture.Sample sample = sManager.QuerySample();
                    //if (sample.color.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_RGB32, out imData) <
                    //pxcmStatus.PXCM_STATUS_NO_ERROR) return;
                    //Bitmap x = imData.ToBitmap(0, sample.color.info.width, sample.color.info.height);
                    var save = SavePictures(colorBitmap, expressionArr[i]);
                    //x.Dispose();
                    //sample.color.ReleaseAccess(imData);
                    sem.Release();
                 }
                this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate()
                { faceExpressionTitle.Content = "thanks we are done ! "; }));
                
                FacesDone = true; 
            }
        }

        private void ProcessingThread()
        {

            processingThread1 = new Thread(new ThreadStart(start));
            var firstWindowcount = 0;
            while (senseManager.AcquireFrame(true) >= pxcmStatus.PXCM_STATUS_NO_ERROR) 
            {
                PXCMCapture.Sample sample = senseManager.QuerySample();
                PXCMImage.ImageData colorData;

                // Get color image data
                sample.color.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_RGB24, out colorData);
                sem.WaitOne();
                colorBitmap = colorData.ToBitmap(0, sample.color.info.width, sample.color.info.height);
                sample.color.ReleaseAccess(colorData);
                sem.Release();
                // Get a face instance
                face = senseManager.QueryFace();

                if (face != null)
                {
                  
                    
                }

                this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate()
                    {
                        // Mirror the color stream Image control
                        imgMain.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
                        ScaleTransform mainTransform = new ScaleTransform();
                        mainTransform.ScaleX = -1;
                        mainTransform.ScaleY = 1;
                        imgMain.RenderTransform = mainTransform;

                        // Display the color stream
                        sem.WaitOne();
                        imgMain.Source = ConvertBitmap.BitmapToBitmapSource(colorBitmap);
                        sem.Release();
                    }));
                // Release the frame
                
                senseManager.ReleaseFrame();
                WindowOpen = true;
                if (firstWindowcount == 3)
                    processingThread1.Start();
                firstWindowcount++;
                    

            }
            colorBitmap.Dispose();
        }

        private void waitForExpression(FaceExpression expression)
        {
            int topScore = 0;
            bool waitEnd = false;
            while (!waitEnd)
            { 
                // Get face tracking processed data
                PXCMFaceData faceData = face.CreateOutput();
                faceData.Update();
                numberTrackedFaces = faceData.QueryNumberOfDetectedFaces();

                // Retrieve the face location data instance
                PXCMFaceData.Face faceDataFace = faceData.QueryFaceByIndex(0);

                if (faceDataFace != null)
                {
                    // Retrieve face location data
                    PXCMFaceData.DetectionData faceDetectionData = faceDataFace.QueryDetection();
                    if (faceDetectionData != null)
                    {
                        PXCMRectI32 faceRectangle;
                        faceDetectionData.QueryFaceAverageDepth(out faceAverageDepth);
                        faceDetectionData.QueryBoundingRect(out faceRectangle);
                        faceRectangleHeight = faceRectangle.h;
                        faceRectangleWidth = faceRectangle.w;
                        faceRectangleX = faceRectangle.x;
                        faceRectangleY = faceRectangle.y;
                    }

                    // Retrieve pose estimation data
                    PXCMFaceData.PoseData facePoseData = faceDataFace.QueryPose();
                    if (facePoseData != null)
                    {
                        PXCMFaceData.PoseEulerAngles headAngles;
                        facePoseData.QueryPoseAngles(out headAngles);
                        headRoll = headAngles.roll;
                        headPitch = headAngles.pitch;
                        headYaw = headAngles.yaw;
                    }

                    // Retrieve expression data
                    PXCMFaceData.ExpressionsData expressionData = faceDataFace.QueryExpressions();

                    // I think  its to see in a given frame whats the reall expression
                    if (expressionData != null)
                    {
                        PXCMFaceData.ExpressionsData.FaceExpressionResult score;

                        expressionData.QueryExpression(PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_KISS, out score);
                        expressionScore[Convert.ToInt32(FaceExpression.Kiss)] = score.intensity;

                        expressionData.QueryExpression(PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_MOUTH_OPEN, out score);
                        expressionScore[Convert.ToInt32(FaceExpression.Open)] = score.intensity;

                        expressionData.QueryExpression(PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_SMILE, out score);
                        expressionScore[Convert.ToInt32(FaceExpression.Smile)] = score.intensity;

                        expressionData.QueryExpression(PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_TONGUE_OUT, out score);
                        expressionScore[Convert.ToInt32(FaceExpression.Tongue)] = score.intensity;

                        expressionData.QueryExpression(PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_EYES_CLOSED_LEFT, out score);
                        expressionScore[Convert.ToInt32(FaceExpression.eyesClose)] = score.intensity;

                        expressionData.QueryExpression(PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_EYES_CLOSED_RIGHT, out score);
                        expressionScore[Convert.ToInt32(FaceExpression.eyesClose)] = score.intensity;

                        // Determine the highest scoring expression
                        for (int i = 1; i < TotalExpressions; i++)
                        {
                            if (expression == FaceExpression.None)
                            {
                                waitEnd = true; break;
                            }
                            if (expressionScore[i] > topScore && expression == (FaceExpression)i)
                            {waitEnd = true; break;}
                            //if (expressionScore[i] > topScore) { expression = (FaceExpression)i; }
                        }

                    }
                }
                faceData.Dispose();
            }
        }

        private bool SavePictures(Bitmap bitmap, FaceExpression expression)
        {
            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate()
            {
                if (bitmap != null)
                {
                    
                    if (numberTrackedFaces > 0)
                    {
                        switch (expression)
                        {
                            case FaceExpression.None:
                                bitmap.Save(@"C:\Users\shirhen\Downloads\BlockHead_v2\pics\" + FaceExpression.None.ToString() + ".jpeg", ImageFormat.Jpeg);
                                //imgFace.Source = new BitmapImage(new Uri("pack://application:,,,/FaceBasic.png"));
                                break;
                            case FaceExpression.Kiss:
                                // save 
                                bitmap.Save(@"C:\Users\shirhen\Downloads\BlockHead_v2\pics\" + FaceExpression.Kiss.ToString() + ".jpeg", ImageFormat.Jpeg);
                                //imgFace.Source = new BitmapImage(new Uri("pack://application:,,,/FaceKissing.png"));
                                break;
                            case FaceExpression.Open:
                                bitmap.Save(@"C:\Users\shirhen\Downloads\BlockHead_v2\pics\" + FaceExpression.Open.ToString() + ".jpeg", ImageFormat.Jpeg);
                                //imgFace.Source = new BitmapImage(new Uri("pack://application:,,,/FaceMouthOpen.png"));
                                break;
                            case FaceExpression.Smile:
                                bitmap.Save(@"C:\Users\shirhen\Downloads\BlockHead_v2\pics\" + FaceExpression.Smile.ToString() + ".jpeg", ImageFormat.Jpeg);
                                //imgFace.Source = new BitmapImage(new Uri("pack://application:,,,/FaceSmiling.png"));
                                break;
                            case FaceExpression.Tongue:
                                bitmap.Save(@"C:\Users\shirhen\Downloads\BlockHead_v2\pics\" + FaceExpression.Tongue.ToString() + ".jpeg", ImageFormat.Jpeg);
                                //imgFace.Source = new BitmapImage(new Uri("pack://application:,,,/FaceTongueOut.png"));
                                break;
                            case FaceExpression.eyesClose:
                                bitmap.Save(@"C:\Users\shirhen\Downloads\BlockHead_v2\pics\" + FaceExpression.eyesClose.ToString() + ".jpeg", ImageFormat.Jpeg);
                                //imgface.source = new bitmapimage(new uri("pack://application:,,,/facetongueout.png"));
                                break;
                            default:
                                bitmap.Save(@"C:\Users\shirhen\Downloads\BlockHead_v2\pics\basic.jpeg", ImageFormat.Jpeg);
                                //imgFace.Source = new BitmapImage(new Uri("pack://application:,,,/FaceBasic.png"));
                                break;
                        }
                        
                        //// Make the BlockHead visible
                        //imgFace.Visibility = Visibility.Visible;

                        //// Scale the BlockHead
                        //imgFace.Width = faceRectangleWidth;
                        //imgFace.Height = faceRectangleHeight;

                        //// Rotate the BlockHead based on Roll orientation data
                        //RotateTransform headRotateTransform = new RotateTransform(headRoll);
                        //headRotateTransform.CenterX = imgFace.Width / 2;
                        //headRotateTransform.CenterY = imgFace.Height / 2;
                        //imgFace.RenderTransform = headRotateTransform;

                        //// Display the BlockHead on the canvas
                        //Canvas.SetRight(imgFace, faceRectangleX - 15);
                        //Canvas.SetTop(imgFace, faceRectangleY);
                    }
                    else
                    {
                        imgFace.Visibility = Visibility.Hidden;
                    }

                    
                }
            }));
            return true;
        }
    }
}
