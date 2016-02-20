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
        private FlightController flightController;
        private CrazyflieController crazyflieController;

        private bool isCrazyfliePaired;
        
        
        public MainPage()
        {
            this.InitializeComponent();

            flightController = new FlightController(leftStick, rightStick);
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
