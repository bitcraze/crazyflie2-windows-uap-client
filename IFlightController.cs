using System.Threading.Tasks;

namespace CrazyflieClient
{
    //
    // Summary:
    //      Structure describing the current setpoints
    //      of the flight control object.  This structure
    //      describes four axes: roll, pitch, yaw, thrust.
    //      Each value is a percentage multiplier corresponding
    //      to how far the axis has been set.  
    struct FlightControlAxes
    {
        // Value of the roll axis as a floating point
        // percentage in the range of (-1,1)
        // where -1 is full left and 1 is full right
        public double roll;

        // Value of the pitch axis as a floating point
        // percentage in the range of (-1,1)
        // where -1 is full backward and 1 is full forward
        public double pitch;
        
        // Value of the yaw axis as a floating point 
        // percentage in the range of (-1,1)
        // where -1 is full counter-clockwise and 1 is full clockwise
        public double yaw;

        // Value of the thrust axis as a floating point 
        // percentage in the range of (0,1) 
        // where 0 is no thrust and 1 is full thrust
        public double thrust;
    }

    interface IFlightController
    {
        //
        // Summary:
        //      Returns current flight control axes information
        //      All four values are returned as a synchronized 
        //      snapshot in time.
        Task<FlightControlAxes> GetFlightControlAxes();
    }
}
