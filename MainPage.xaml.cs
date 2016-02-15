using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace CrazyflieClient
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private BthCrtp TestBthCrtp;

        private TranslateTransform controlStickRightTranslation;
        private TranslateTransform controlStickLeftTranslation;

        private double controlStickLeftSize;
        private double controlBoxLeftSize;

        private double controlStickRightSize;
        private double controlBoxRightSize;

        private Point controlStickLeftCoords;
        private Point controlStickRightCoords;

        private CancellationTokenSource cancellationSource;

        public MainPage()
        {
            this.InitializeComponent();

            TestBthCrtp = new BthCrtp();

            controlStickRightTranslation = new TranslateTransform();
            controlStickRight.RenderTransform = this.controlStickRightTranslation;
            controlStickRight.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            controlStickRight.ManipulationDelta += onControlStickRightMoved;
            controlStickRight.ManipulationCompleted += onControlStickRightReleased;

            controlStickLeftTranslation = new TranslateTransform();
            controlStickLeft.RenderTransform = this.controlStickLeftTranslation;
            controlStickLeft.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            controlStickLeft.ManipulationDelta += onControlStickLeftMoved;
            controlStickLeft.ManipulationCompleted += onControlStickLeftReleased;

            //todo: don't hardcode?
            controlStickLeftSize = controlStickLeft.Width;
            controlBoxLeftSize = controlBoxLeft.Width;
            controlStickRightSize = controlStickRight.Width;
            controlBoxRightSize = controlBoxRight.Width;

            // Set the left stick (thrust) to the bottom of the box)
            // TODO: this will be configurable
            controlStickLeftTranslation.Y = controlBoxLeft.Height / 2;
        }

        private void onControlStickRightMoved(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            controlStickRightTranslation.X += e.Delta.Translation.X;
            controlStickRightTranslation.Y += e.Delta.Translation.Y;
        }

        private void onControlStickRightReleased(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            controlStickRightTranslation.X = 0;
            controlStickRightTranslation.Y = 0;
        }

        private void onControlStickLeftMoved(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            controlStickLeftTranslation.X += e.Delta.Translation.X;
            controlStickLeftTranslation.Y += e.Delta.Translation.Y;
        }

        private void onControlStickLeftReleased(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            controlStickLeftTranslation.X = 0;
            controlStickLeftTranslation.Y = controlBoxLeft.Height / 2;
        }

        private async void onConnectClick(object sender, RoutedEventArgs e)
        {
            bool result = await TestBthCrtp.InitBthConnection();
            if (!result)
            {
                debugOutput.Text = "Connection Failed!";
            }
            else
            {
                debugOutput.Text = "Connection Succeeded!";
                TestBthCrtp.StartBthLink();

                //Task t = new Task(SetPointSetter(cancellationSource.Token), cancellationSource.Token, TaskCreationOptions.LongRunning);
                //t.Start();
                //cancellationSource.d
                if (cancellationSource != null)
                {
                    cancellationSource.Dispose();
                }
                cancellationSource = new CancellationTokenSource();
                Task t2 = Task.Factory.StartNew(() => SetPointSetter(cancellationSource.Token), TaskCreationOptions.LongRunning);
            }
        }

        private void onDisconnectClick(object sender, RoutedEventArgs e)
        {
            cancellationSource.Cancel();            
        }

        private async void SetPointSetter(CancellationToken cancelToken)
        {
            double leftXPos;
            double leftYPos;
            double rightXPos;
            double rightYPos;


            // TODO:  should implement a disconnect someday...
            while(!cancelToken.IsCancellationRequested)
            {
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    controlStickLeftCoords = controlStickLeft.TransformToVisual(controlBoxLeft).TransformPoint(new Point(0, 0));
                    controlStickRightCoords = controlStickRight.TransformToVisual(controlBoxRight).TransformPoint(new Point(0, 0));
                });

                // controlStickLeft coords represents the top left corner of the XAML element
                // we want the center to be 0,0 so add in half the height of the stick object
                double leftStickXPos = controlStickLeftCoords.X + (controlStickLeftSize / 2);

                // The range of the stick is -100 to 100 where "0" is when the stick is at 1/2 the box width
                // Subtract half the width of the control box -- this gives the position relative to the midpoint
                double leftStickXOffset = leftStickXPos - (controlBoxLeftSize / 2);

                // Finally, compute what percentage of the full range this offset is
                double leftStickXOffsetPercent = leftStickXOffset / (controlBoxLeftSize / 2);

                leftXPos = leftStickXOffsetPercent;

                if (leftXPos > 1)
                {
                    leftXPos = 1;
                }
                if (leftXPos < -1)
                {
                    leftXPos = -1;
                }


                double leftStickYPos = controlStickLeftCoords.Y + (controlStickLeftSize / 2);

                // the range of the stick in the Y dimension is 0 to 100 where 0 is the full height
                // of the box.  Get the offset by subtracting the height from the pos, and inverting.
                double leftstickYOffset = (leftStickYPos - controlBoxLeftSize) * -1;

                double leftStickYOffsetPercent = leftstickYOffset / controlBoxLeftSize;

                leftYPos = leftStickYOffsetPercent;

                if (leftYPos > 1)
                {
                    leftYPos = 1;
                }
                if (leftYPos < 0)
                {
                    leftYPos = 0;
                }

                // controlStickRight coords represents the top left corner of the XAML element
                // we want the center to be 0,0 so add in half the height of the stick object
                double rightStickXPos = controlStickRightCoords.X + (controlStickRightSize / 2);

                // The range of the stick is -100 to 100 where "0" is when the stick is at 1/2 the box width
                // Subtract half the width of the control box -- this gives the position relative to the midpoint
                double rightStickXOffset = rightStickXPos - (controlBoxRightSize / 2);

                // Finally, compute what percentage of the full range this offset is
                double rightStickXOffsetPercent = rightStickXOffset / (controlBoxRightSize / 2);

                rightXPos = rightStickXOffsetPercent;

                if (rightXPos > 1)
                {
                    rightXPos = 1;
                }
                if (rightXPos < -1)
                {
                    rightXPos = -1;
                }

                // controlStickRight coords represents the top left corner of the YAML element
                // we want the center to be 0,0 so add in half the height of the stick object
                double rightStickYPos = controlStickRightCoords.Y + (controlStickRightSize / 2);

                // The range of the stick is -100 to 100 where "0" is when the stick is at 1/2 the box width
                // Subtract half the width of the control box -- this gives the position relative to the midpoint
                // invert the Y axis
                double rightStickYOffset = (rightStickYPos - (controlBoxRightSize / 2));

                // Finally, compute what percentage of the full range this offset is
                double rightStickYOffsetPercent = rightStickYOffset / (controlBoxRightSize / 2);

                rightYPos = rightStickYOffsetPercent;

                if (rightYPos > 1)
                {
                    rightYPos = 1;
                }
                if (rightYPos < -1)
                {
                    rightYPos = -1;
                }

                Debug.WriteLine("R:" + rightXPos + "\tP:" + rightYPos + "\tY:" + leftXPos + "\tT:" + leftYPos);

                await TestBthCrtp.SetSetpoints(Convert.ToSingle(rightXPos), Convert.ToSingle(rightYPos), Convert.ToSingle(leftXPos), Convert.ToSingle(leftYPos));

            }
            return;
        }
    }
}
