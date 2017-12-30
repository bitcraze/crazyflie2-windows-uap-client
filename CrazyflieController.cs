using System.Threading;
using System.Threading.Tasks;

namespace CrazyflieClient
{
    // 
    // Summary:
    //      This class implements the control of
    //      a crazyflie 2.0 quadcopter via BLE
    //
    //      Uses an object which implements the IFlightController
    //      interface for determining flight commander setpoints
    class CrazyflieController
    {
        private IFlightController flightController;
        private CancellationTokenSource cancellationSource;
        private BthCrtp bthCrtp;

        private const int MaxThrust = 65536;

        //
        // Summary:
        //      Maximum pitch/roll value to send to the commander, in degrees
        private double maxPitchRollRate = 30;
        public double MaxPitchRollRate
        {
            get { return maxPitchRollRate; }
            set { maxPitchRollRate = value; }
        }

        //
        // Summary:
        //      Maximum yaw value to send to the commander, in degrees
        private double maxYawRate = 200; // degrees per second
        public double MaxYawRate
        {
            get { return maxYawRate; }
            set { maxYawRate = value; }
        }

        //
        // Summary:
        //      Maximum roll value to send to the commander, in percent (0 to 1)
        private double maxThrustPercent = 0.8;
        public double MaxThrustPercent
        {
            get { return maxThrustPercent; }
            set { maxThrustPercent = value; }
        }

        public CrazyflieController(IFlightController flightController)
        {
            this.flightController = flightController;
            bthCrtp = new BthCrtp();
        }

        public async Task<bool> IsCrazyfliePaired()
        {
            return await bthCrtp.IsCrazyfliePaired();
        }

        //
        // Summary:
        //      This function starts the communication to the crazyflie commander
        //      to set flight setpoints based on the state of the flight controller
        public async void StartCommanderLink()
        {
            // Set up the cancellation token 
            if (cancellationSource != null)
            {
                cancellationSource.Dispose();
            }

            await bthCrtp.InitCrtpService();

            cancellationSource = new CancellationTokenSource();
            Task t = Task.Factory.StartNew(() => CommanderSetpointThread(cancellationSource.Token), TaskCreationOptions.LongRunning);
        }

        // 
        // Summary:
        //      This function stops the communication to the crazyflie commander
        public void StopCommanderLink()
        {
            if (cancellationSource != null)
            {
                // TODO: consider writing zeros here to shut motors off
                // Not critical, as the commander code in the CF FW will time out
                cancellationSource.Cancel();
            }
        }

        //
        // Summary:
        //     Thread function for writing commander packets via BTH
        //     Writes as fast as it can in a tight loop
        private async void CommanderSetpointThread(CancellationToken cancellationToken)
        {
            // Write commander packets as fast as possible in a loop until cancelled 
            while(!cancellationToken.IsCancellationRequested)
            {
                FlightControlAxes axes = await flightController.GetFlightControlAxes();
                //await bthCrtp.WriteCommanderPacket(
                //    (float)(axes.roll * maxPitchRollRate),
                //    (float)(axes.pitch * maxPitchRollRate),
                //    (float)(axes.yaw * maxYawRate),
                //    (ushort)(axes.thrust * maxThrustPercent * MaxThrust));

                await bthCrtp.WriteCppmCommanderPacket(
                    (ushort)((axes.roll * 500) + 1500),
                    (ushort)((axes.pitch * 500) + 1500),
                    (ushort)((axes.yaw * 500) + 1500),
                    (ushort)((axes.thrust * 1000 * maxThrustPercent) + 1000),
                    (ushort)(axes.isSelfLevelEnabled ? 2000 : 1000),
                    (ushort)(axes.isArmed ? 2000 : 1000));
            }
        }
    }
}
