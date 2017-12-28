using System.Numerics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Input.Spatial;

using System.Diagnostics;


namespace CrazyflieClient
{
    //
    // Summary:
    //      Implementation of a flight controller for a UAP app.
    //      This implementation uses Spatial Input APIs to consume hand gestures (HoloLens)
    class GestureController : IFlightController
    {
        private SpatialInteractionManager interactionManager;
        private SpatialGestureRecognizer gestureRecognizer;

        private Vector3 lastNavigationOffset;

        private bool armed = false;
  
        public GestureController()
        {
            gestureRecognizer = new SpatialGestureRecognizer(
                SpatialGestureSettings.Tap | 
                SpatialGestureSettings.NavigationX |
                SpatialGestureSettings.NavigationY |
                SpatialGestureSettings.NavigationZ);
            gestureRecognizer.NavigationStarted += OnNavigationStarted;
            gestureRecognizer.NavigationCompleted += OnNavigationCompleted;
            gestureRecognizer.NavigationCanceled += OnNavigationCanceled;
            gestureRecognizer.NavigationUpdated += OnNavigationUpdated;
            gestureRecognizer.Tapped += OnTapped;

            interactionManager = SpatialInteractionManager.GetForCurrentView();
            interactionManager.InteractionDetected += OnInteractionDetected;
        }

        private void OnInteractionDetected(object sender, SpatialInteractionDetectedEventArgs e)
        {
            gestureRecognizer.CaptureInteraction(e.Interaction);
        }

        private void OnNavigationStarted(object sender, SpatialNavigationStartedEventArgs e)
        {
            Debug.WriteLine("Navigation Started!");
        }

        private void OnNavigationCompleted(object sender, SpatialNavigationCompletedEventArgs e)
        {
            lastNavigationOffset.X = 0;
            lastNavigationOffset.Y = 0;
            lastNavigationOffset.Z = 0;
            armed = false;
        }

        private void OnNavigationCanceled(object sender, SpatialNavigationCanceledEventArgs e)
        {
            lastNavigationOffset.X = 0;
            lastNavigationOffset.Y = 0;
            lastNavigationOffset.Z = 0;
            armed = false;
        }

        private void OnNavigationUpdated(object sender, SpatialNavigationUpdatedEventArgs e)
        {
            lastNavigationOffset = e.NormalizedOffset;
            Debug.WriteLine(lastNavigationOffset.ToString()); 
        }

        private void OnTapped(object sender, SpatialTappedEventArgs e)
        {
            Debug.WriteLine("onTapped");
            armed = !armed;
        }

        //
        // IFlightController implementaiton
        //
        public async Task<FlightControlAxes> GetFlightControlAxes()
        {
            FlightControlAxes axes;
            axes.roll = lastNavigationOffset.X;
            axes.pitch = -1 * lastNavigationOffset.Z; // Z is inverted 
            axes.yaw = 0; // No yaw support
            if(lastNavigationOffset.Y >= 0)
            {
                axes.thrust = lastNavigationOffset.Y;
            }
            else
            {
                axes.thrust = 0;
            }

            axes.armed = armed;

            return axes;
        }
    }
}
