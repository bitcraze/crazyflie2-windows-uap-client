using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace CrazyflieClient
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private IFlightController flightController;
        private CrazyflieController crazyflieController;

        private bool isCrazyfliePaired;
        
        
        public MainPage()
        {
            this.InitializeComponent();

            // TODO: select the input controller - eventually this should
            // be configurable at runtime through a settings page, or be 
            // automatically detected.
            //
            // For touchscreen control, use the FlightController class (default)
            flightController = new FlightController(leftStick, rightStick);
            // For spatial gesture control (HoloLens), use the GestureController class
            //flightController = new GestureController();

            crazyflieController = new CrazyflieController(flightController);    
        }

        private async void onClick(object sender, RoutedEventArgs e)
        {
            if(connectionButton.Content.ToString() == "Connect")
            {
                isCrazyfliePaired = await crazyflieController.IsCrazyfliePaired();
                if(!isCrazyfliePaired)
                {
                    infoText.Text = "Error: Crazyflie not found. Please pair in settings->devices->bluetooth";
                }
                else
                {
                    infoText.Text = "";
                    connectionButton.Content = "Disconnect";
                    crazyflieController.StartCommanderLink(); 
                }
            }
            else if(connectionButton.Content.ToString() == "Disconnect")
            {
                infoText.Text = "";
                connectionButton.Content = "Connect";
                crazyflieController.StopCommanderLink();
            }
        }
    }
}
