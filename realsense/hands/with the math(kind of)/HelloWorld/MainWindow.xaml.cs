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
using System.Drawing;
using System.Windows.Media.Effects;
using System.Windows;
using System.Media;
using System.Threading;
using System.Windows.Media.Imaging;
using System.Windows.Controls;



namespace HelloWorld
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Thread processingThread;
        private PXCMSenseManager senseManager;
        private PXCMHandModule hand;
        private PXCMHandConfiguration handConfig;
        private PXCMHandData handData;
        private PXCMHandData.GestureData gestureData;
        private bool handWaving;
        private bool isHandOnPic;
        private System.Windows.Controls.Image DragImg;
        private Thickness DragLoc;
        private bool isDragging;
        private bool handTrigger;
        private int msgTimer;

        public MainWindow()
        {
            InitializeComponent();
            handWaving = false;
            isHandOnPic = false;
            handTrigger = false;
            isDragging = false;
            msgTimer = 0;

            
            // Instantiate and initialize the SenseManager
            senseManager = PXCMSenseManager.CreateInstance();
            senseManager.EnableStream(PXCMCapture.StreamType.STREAM_TYPE_COLOR, 1280, 720, 30);
            senseManager.EnableHand();
            senseManager.Init();

            // Configure the Hand Module
            hand = senseManager.QueryHand();
            handConfig = hand.CreateActiveConfiguration();
            handConfig.EnableGesture("spreadfingers");
            handConfig.EnableAllAlerts();
            handConfig.ApplyChanges();

            // Start the worker thread
            processingThread = new Thread(new ThreadStart(ProcessingThread));
            processingThread.Start();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            lblMessage.Content = "(spreadfingers Your Hand)";

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            processingThread.Abort();
            if (handData != null) handData.Dispose();
            handConfig.Dispose();
            senseManager.Dispose();
        }

        private void ProcessingThread()
        {
            // Start AcquireFrame/ReleaseFrame loop
            while (senseManager.AcquireFrame(true) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                PXCMCapture.Sample sample = senseManager.QuerySample();
                Bitmap colorBitmap;
                PXCMImage.ImageData colorData;

                // Get color image data
                sample.color.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_RGB24, out colorData);
                colorBitmap = colorData.ToBitmap(0, sample.color.info.width, sample.color.info.height);

                // Retrieve gesture data
                hand = senseManager.QueryHand();

                if (hand != null)
                {
                    // Retrieve the most recent processed data
                    handData = hand.CreateOutput();
                    handData.Update();
                    
                    handWaving = handData.IsGestureFired("spreadfingers", out gestureData);

                    if (handWaving)
                    {
                        this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate()
                        {
                            Console.WriteLine("Top - " + Schlez.Margin.Top);
                            Console.WriteLine("Left - " + Schlez.Margin.Left);
                            Console.WriteLine("Right - " + Schlez.Margin.Right);
                            Console.WriteLine("Butoom - " + Schlez.Margin.Bottom);

                            //isHandOnPic = IsHandOverThePic(handData, new Thickness( Schlez.Margin.Left-(Schlez.Width/2),
                            //                                                        Schlez.Margin.Top-(Schlez.Height/2), 
                            //                                                        Schlez.Margin.Right+(Schlez.Width/2), 
                            //                                                        Schlez.Margin.Bottom + (Schlez.Height/2)));

                            if (!IsHandOverThePic(handData, Schlez, new Thickness(Canvas.GetLeft(Schlez),
                                                                                  Canvas.GetTop(Schlez),
                                                                                  Canvas.GetRight(Schlez),
                                                                                  Canvas.GetBottom(Schlez))) && isDragging)
                            {
                                isHandOnPic = true;
                            }
                            else
                            {
                                if (IsHandOverThePic(handData, Breakfast, new Thickness(Canvas.GetLeft(Breakfast),
                                                                                  Canvas.GetTop(Breakfast),
                                                                                  Canvas.GetRight(Breakfast),
                                                                                  Canvas.GetBottom(Breakfast))) && !isDragging)
                                {
                                    isDragging = true;
                                    DragImg = Breakfast;
                                }
                                else
                                {
                                    if (IsHandOverThePic(handData, FrenchFries, new Thickness(Canvas.GetLeft(FrenchFries),
                                                                                  Canvas.GetTop(FrenchFries),
                                                                                  Canvas.GetRight(FrenchFries),
                                                                                  Canvas.GetBottom(FrenchFries))) && !isDragging)
                                    {
                                        isDragging = true;
                                        DragImg = FrenchFries;
                                    }
                                    else 
                                    {
                                        if (IsHandOverThePic(handData, IceCream, new Thickness(Canvas.GetLeft(IceCream),
                                                                                  Canvas.GetTop(IceCream),
                                                                                  Canvas.GetRight(IceCream),
                                                                                  Canvas.GetBottom(IceCream))) && !isDragging)
                                        {
                                            isDragging = true;
                                            DragImg = IceCream;
                                        }
                                        else
                                        {
                                            isDragging = false;
                                            isHandOnPic = false;
                                        }
                                    }
                                }
                            }               
                        }));
                    }
                }

                // Update the user interface
                UpdateUI(colorBitmap);

                // Release the frame
                if (handData != null) handData.Dispose();
                colorBitmap.Dispose();
                sample.color.ReleaseAccess(colorData);
                senseManager.ReleaseFrame();
            }
        }

        /* Check If current frames hand joints is Over The Picture */
        private bool IsHandOverThePic(PXCMHandData handOutput, System.Windows.Controls.Image img, Thickness picloc, long timeStamp = 0)
        {
            //Iterate hands
            PXCMHandData.JointData[][] nodes = new PXCMHandData.JointData[][] { new PXCMHandData.JointData[0x20], new PXCMHandData.JointData[0x20] };
            int numOfHands = handOutput.QueryNumberOfHands();
            for (int i = 0; i < numOfHands; i++)
            {
                //Get hand by time of appearence
                //PXCMHandAnalysis.HandData handData = new PXCMHandAnalysis.HandData();
                PXCMHandData.IHand handData;
                if (handOutput.QueryHandData(PXCMHandData.AccessOrderType.ACCESS_ORDER_BY_TIME, i, out handData) == pxcmStatus.PXCM_STATUS_NO_ERROR)
                {
                    if (handData != null)
                    {
                        //Iterate Joints
                        for (int j = 0; j < 0x20; j++)
                        {
                            PXCMHandData.JointData jointData;
                            handData.QueryTrackedJoint((PXCMHandData.JointType)j, out jointData);
                            nodes[i][j] = jointData;

                        } // end iterating over joints
                    }
                }
            } // end itrating over hands

            return IsJointOverThePic(nodes, numOfHands, img, picloc);
        }

        /* Check If current frames hand joints is Over The Picture */
        public bool IsJointOverThePic(PXCMHandData.JointData[][] nodes, int numOfHands, System.Windows.Controls.Image img, Thickness picloc)
        {
            bool IsJointOverPic = false;
            

            for (int i = 0; i < numOfHands; i++)
            {
                for (int j = 1; (j < 22 && !IsJointOverPic)  ; j++)
                {
                    if (nodes[i][j] == null) continue;
                    int x = (int)nodes[i][j].positionImage.x;
                    int y = (int)nodes[i][j].positionImage.y;
                    this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate()
                    {
                    int newLeft = (int)((cnv.ActualWidth) - x - img.Width) < 0 ? 0 : (int)((cnv.ActualWidth) - x - img.Width);
                    int newTop = (int)((cnv.ActualHeight) - y - img.Height) < 0 ? 0 : (int)((cnv.ActualHeight) - y - img.Height);
                    DragLoc = new Thickness(newLeft, y, x, newTop);
                  
                    
                     lblCor.Content = x + "," + y;
                     if (((x >= cnv.ActualWidth - newLeft - img.Width) && (x <= newLeft)) && ((y >= cnv.ActualHeight - newTop - img.Height) && (y <= newTop)) && (j == 1))
                    {
                       IsJointOverPic = true;
                       
                    }
                    }));
                }
            }
            
            return IsJointOverPic;
        }

            //Bitmap bitmap = null;
            //Graphics g = Graphics.FromImage(bitmap);

            //lock (this)
            //{
            //    int scaleFactor = 1;

            //    using (Pen boneColor = new Pen(Color.DodgerBlue, 3.0f))
            //    {
            //        for (int i = 0; i < numOfHands; i++)
            //        {
            //            if (nodes[i][0] == null) continue;
            //            int baseX = (int)nodes[i][0].positionImage.x / scaleFactor;
            //            int baseY = (int)nodes[i][0].positionImage.y / scaleFactor;

            //            int wristX = (int)nodes[i][0].positionImage.x / scaleFactor;
            //            int wristY = (int)nodes[i][0].positionImage.y / scaleFactor;

            //            for (int j = 1; j < 22; j++)
            //            {
            //                if (nodes[i][j] == null) continue;
            //                int x = (int)nodes[i][j].positionImage.x / scaleFactor;
            //                int y = (int)nodes[i][j].positionImage.y / scaleFactor;

            //                if (nodes[i][j].confidence <= 0) continue;

            //                if (j == 2 || j == 6 || j == 10 || j == 14 || j == 18)
            //                {

            //                    baseX = wristX;
            //                    baseY = wristY;
            //                }

            //                //g.DrawLine(boneColor, new System.Drawing.Point(baseX, baseY), new System.Drawing.Point(x, y));
            //                baseX = x;
            //                baseY = y;
            //            }

                        //using (
                        //    Pen red = new Pen(Color.Red, 3.0f),
                        //        black = new Pen(Color.Black, 3.0f),
                        //        green = new Pen(Color.Green, 3.0f),
                        //        blue = new Pen(Color.Blue, 3.0f),
                        //        cyan = new Pen(Color.Cyan, 3.0f),
                        //        yellow = new Pen(Color.Yellow, 3.0f),
                        //        orange = new Pen(Color.Orange, 3.0f))
                        //{
                        //    Pen currnetPen = black;

                        //    for (int j = 0; j < PXCMHandData.NUMBER_OF_JOINTS; j++)
                        //    {
                        //        float sz = 4;

                        //        int x = (int)nodes[i][j].positionImage.x / scaleFactor;
                        //        int y = (int)nodes[i][j].positionImage.y / scaleFactor;

                        //        if (nodes[i][j].confidence <= 0) continue;

                        //        //Wrist
                        //        if (j == 0)
                        //        {
                        //            currnetPen = black;
                        //        }

                        //        //Center
                        //        if (j == 1)
                        //        {
                        //            currnetPen = red;
                        //            sz += 4;
                        //        }

                        //        //Thumb
                        //        if (j == 2 || j == 3 || j == 4 || j == 5)
                        //        {
                        //            currnetPen = green;
                        //        }
                        //        //Index Finger
                        //        if (j == 6 || j == 7 || j == 8 || j == 9)
                        //        {
                        //            currnetPen = blue;
                        //        }
                        //        //Finger
                        //        if (j == 10 || j == 11 || j == 12 || j == 13)
                        //        {
                        //            currnetPen = yellow;
                        //        }
                        //        //Ring Finger
                        //        if (j == 14 || j == 15 || j == 16 || j == 17)
                        //        {
                        //            currnetPen = cyan;
                        //        }
                        //        //Pinkey
                        //        if (j == 18 || j == 19 || j == 20 || j == 21)
                        //        {
                        //            currnetPen = orange;
                        //        }


                        //        if (j == 5 || j == 9 || j == 13 || j == 17 || j == 21)
                        //        {
                        //            sz += 4;
                        //            //currnetPen.Width = 1;
                        //        }

                        //        //g.DrawEllipse(currnetPen, x - sz / 2, y - sz / 2, sz, sz);
                        //    }
                        //}
        //            }
        //        }
        //    }
        //    //g.Dispose();
        //}

        private void UpdateUI(Bitmap bitmap)
        {
            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate()
            {
                if (bitmap != null)
                {
                    // Mirror the color stream Image control
                    imgColorStream.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
                    System.Windows.Media.ScaleTransform mainTransform = new System.Windows.Media.ScaleTransform();
                    mainTransform.ScaleX = -1;
                    mainTransform.ScaleY = 1;
                    imgColorStream.RenderTransform = mainTransform;
                    
                    // Display the color stream
                    imgColorStream.Source = ConvertBitmap.BitmapToBitmapSource(bitmap);
                    Schlez.Source = new BitmapImage(new Uri("pack://application:,,,/pic/Schlez.jpg"));
                    Breakfast.Source = new BitmapImage(new Uri("pack://application:,,,/pic/Breakfast.png"));
                    FrenchFries.Source = new BitmapImage(new Uri("pack://application:,,,/pic/FrenchFries.png"));
                    IceCream.Source = new BitmapImage(new Uri("pack://application:,,,/pic/IceCream.png"));
                    heart.Source = new BitmapImage(new Uri("pack://application:,,,/pic/heart.png"));
                    
                    // Update the screen message

                    if ((handWaving) && (isDragging) && (DragImg.Visibility == Visibility.Visible))
                    {
                        //DragImg.Margin = DragLoc;
                        DragImg.PointToScreen(new System.Windows.Point(DragLoc.Top, DragLoc.Right));
                        lblMessage.Content = "You Chose The Food.";
                        handTrigger = true;
                    }

                    if ((handWaving) && (isDragging) && (isHandOnPic) && (DragImg.Visibility == Visibility.Visible))
                    {
                        DragImg.Visibility = Visibility.Hidden;
                        lblMessage.Content = "Schlez Eat The Food.";
                        isDragging = false;
                        DragImg = null;
                        handTrigger = true;
                    }

                    // Reset the screen message after ~50 frames
                    if (handTrigger)
                    {
                        msgTimer++;

                        if (msgTimer >= 20)
                        {
                            if ((handWaving) && (isDragging))
                             {
                                 lblMessage.Content = "Food Is Moving";

                             }
                            else
                             {
                                 lblMessage.Content = "Select Food To Eat The Chlez";
                             }
                            msgTimer = 0;
                            handTrigger = false;
                        }
                    }
                }
            }));
        }
    }
}