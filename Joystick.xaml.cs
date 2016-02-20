using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace CrazyflieClient
{
    public sealed partial class Joystick : UserControl
    {
        // Transform used for rendering position of the control stick
        private TranslateTransform controlStickTransform;

        // Variables used for tracking the total transform of a manipulation session
        // This is necessary since the actual visual transform is bounded by the size 
        // of the box.  We need to keep tracking manipulation beyond that box to 
        // detect when the manipulation re-enteres the box.
        private double TotalTransformX = 0;
        private double TotalTransformY = 0;

        private bool fullRangeY = false;

        // Summary:
        //      Value that sets whether this controller is configured for full-range Y.
        //      A full range Y controller's Y axis is bounded by (0,1) instead of (-1,1)
        //      and the default/zero point is at the bottom of the control box.
        public bool FullRangeY
        {
            get { return fullRangeY; }
            set
            {
                // If the property is changing, we have to update
                // the offset information for the UI
                if(value != fullRangeY)
                {
                    if(value)
                    {
                        TotalTransformY += controlBox.Height / 2;
                    }
                    else
                    {
                        TotalTransformY -= controlBox.Height / 2;
                    }
                    // TODO: consider not doing this during an active
                    // manipulation?  Lower pri since this won't actually
                    // occur for the use cases in this app.
                    updateStickTransform();
                }

                fullRangeY = value;
            }
        }

        public Joystick()
        {
            this.InitializeComponent();

            // Set up the gesture handling
            controlStickTransform = new TranslateTransform();
            controlStick.RenderTransform = controlStickTransform;
            controlStick.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            controlStick.ManipulationDelta += onControlStickMoved;
            controlStick.ManipulationCompleted += onControlStickReleased;
        }

        // Summary:
        //      Obtains a Point which represents the position of the 
        //      joystick as a percentage relative to its full range of motion
        //      The ranges of the joystick are defined as follows:
        //          X = -1: joystick is held all the way to the left
        //          X =  0: joystick is centered on the X axis (default position)
        //          X =  1: joystick is held all the way to the right
        //          Y = -1: joystick is held all the way at the bottom
        //                  This only applies when the joystick is not configured for full-range-Y
        //          Y =  0: joystick is in its centered/default positon
        //                  For full-range-Y mode, this is the very bottom of the controller box
        //                  For non full-range-Y mode, this is the center of the controller box
        //          Y =  1: joystick is held all the way at the top
        //
        // This method must not be called from a UI thread for performance.
        public async Task<Point> GetJoystickPosition()
        {
            Point pointRelativeToBox;
            Point joystickSetpoints = new Point();
            double controlStickWidth = 0;
            double controlStickHeight = 0;
            double controlBoxWidth = 0;
            double controlBoxHeight = 0;

            // Run a task on the UI thread to obtain the necessary information from the UI
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                pointRelativeToBox = controlStick.TransformToVisual(controlBox).TransformPoint(new Point(0, 0));
                controlStickWidth = controlStick.Width;
                controlStickHeight = controlStick.Height;
                controlBoxWidth = controlBox.Width;
                controlBoxHeight = controlBox.Height;
            });

            // The relative point corresponds to the top-left corner of the stick -- must normalize 
            // to obtain the midpoint of the control stick
            pointRelativeToBox.X += controlStickWidth / 2;
            pointRelativeToBox.Y += controlStickHeight / 2;

            // The X-range of the stick is -100 to 100 where "0" is when the stick is at 1/2 the box width
            // Subtracting half the width of the control box gives the relative position.  
            // Divide by 1/2 the box width to get the percentage
            joystickSetpoints.X = (pointRelativeToBox.X - (controlBoxWidth / 2)) / (controlBoxWidth / 2);

            // For the Y-axis, the behavior depends on whether this joystick is set for full range
            if (FullRangeY)
            {
                // The Y-range of the stick is 0 to 100 where "0" is when the stick is at the bottom
                // Subtracting the height of the control box gives the relative position
                // Divide by the box height to get the percentage
                // Multiply by -1 to invert the axis (UI views downward motion as positive 
                // but the joystick treats that as negative motion
                joystickSetpoints.Y = (pointRelativeToBox.Y - controlBoxHeight) / controlBoxHeight * -1;
            }
            else
            {
                // The Y-range of the stick is -100 to 100 where "0" is when the stick is at 1/2 the box height
                // Subtracting half the midpoint of the control stick gives the relative position.
                // Divide by 1/2 the box height to get the percentage
                // Multiply by -1 to invert the axis (UI views downward motion as positive 
                // but the joystick treats that as negative motion
                joystickSetpoints.Y = (pointRelativeToBox.Y - (controlBoxHeight / 2)) / (controlBoxHeight / 2) * -1;
            }

            // The manipulation event logic takes care of bounding the stick position to be within the box so
            // we don't need to worry about bounding values to between -1 and 1.
            return joystickSetpoints;
        }

        //
        // Summary:
        //     Updates the visual transform of the control stick
        //     based on the current deltas and other parameters
        private void updateStickTransform()
        {
            // Update the X direction -- bound motion by the width of the box
            if (Math.Abs(TotalTransformX) < (controlBox.Width / 2))
            {
                controlStickTransform.X = TotalTransformX;
            }
            else
            {
                controlStickTransform.X = Math.Sign(TotalTransformX) * (controlBox.Width / 2);
            }

            // Update the Y direction -- bound motion by the height of the box
            if (Math.Abs(TotalTransformY) < (controlBox.Height / 2))
            {
                controlStickTransform.Y = TotalTransformY;
            }
            else
            {
                controlStickTransform.Y = Math.Sign(TotalTransformY) * (controlBox.Height / 2);
            }
        }

        //
        // Summary:
        //      Handles event generated when the control stick is manipulated        
        private void onControlStickMoved(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            TotalTransformX += e.Delta.Translation.X;
            TotalTransformY += e.Delta.Translation.Y;
            updateStickTransform();
        }

        //
        // Summary:
        //      Handles event generated when control stick is released
        //      Sets the control stick to its default position
        private void onControlStickReleased(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            // X default is always zero (centered)
            TotalTransformX = 0;

            // Y default is either centered or at the bottom
            // depending on whether this joystick is configured for a full-range-Y
            TotalTransformY = fullRangeY ? controlBox.Height / 2 : 0;
            updateStickTransform();
        }
    }
}
