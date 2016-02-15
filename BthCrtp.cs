#define USING_BLUETOOTH

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.System.Threading;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace CrazyflieClient
{


    // Class is an implementation of CRTP over BLE
    class BthCrtp
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CrtpControlPacket
        {
            public byte header;
            public float roll;
            public float pitch;
            public float yaw;
            public ushort thrust;
        };


        private static Guid crtpServiceGuid = 
            new Guid("00000201-1c7f-4f9e-947b-43b7c00a9a08");

        private static Guid crtpCharacteristicGuid =
            new Guid("00000202-1c7f-4f9e-947b-43b7c00a9a08");


        private static Guid crtpUpCharacteristicGuid =
            new Guid("00000203-1c7f-4f9e-947b-43b7c00a9a08");

        private static Guid crtpDownCharacteristicGuid =
            new Guid("00000204-1c7f-4f9e-947b-43b7c00a9a08");

        private GattDeviceService crtpService;
        private GattCharacteristic crtpChar;
        private GattCharacteristic crtpUpChar;
        private GattCharacteristic crtpDownChar;

        private ThreadPoolTimer BthWriteTimer;

        private byte tid;
     
        private Object m_Lock;
        private float roll;
        private float pitch;
        private float yaw;
        private UInt16 thrust;

        

        private Boolean isThrustLocked;

        public BthCrtp()
        {
            crtpService = null;
            crtpUpChar = null;
            crtpDownChar = null;
            isThrustLocked = true;

            m_Lock = new Object();
            tid = 0;
            roll = 0;
            pitch = 0;
            yaw = 0;
            thrust = 0;
        }

        // Checks enumerated devices for a device with the crazyflie service GUID
        // This function succeeds if the crazyflie is paired.  A return of 'true' 
        // does not mean the device is connected or connectable.
        public async Task<Boolean> IsCrazyfliePaired()
        {
#if USING_BLUETOOTH
            var bthServices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(
                GattDeviceService.GetDeviceSelectorFromUuid(
                crtpServiceGuid), null);

            return bthServices.Count >= 1;
#else
            return true;
#endif
        }

         
        public async Task<Boolean> InitBthConnection()
        {
#if USING_BLUETOOTH
            var bthServices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(
                GattDeviceService.GetDeviceSelectorFromUuid(
                crtpServiceGuid), null);

            // Use the first instance of this guid
            if (bthServices.Count >= 1)
            {
                crtpService = await GattDeviceService.FromIdAsync(bthServices[0].Id);
                if (crtpService != null)
                {
                    var chars = crtpService.GetCharacteristics(crtpCharacteristicGuid);
                    if (chars.Count >= 1)
                    {
                        crtpChar = chars[0];
                    }
                    var upChars = crtpService.GetCharacteristics(crtpUpCharacteristicGuid);
                    if (upChars.Count >= 1)
                    {
                        crtpUpChar = upChars[0];
                    }
                    var downChars = crtpService.GetCharacteristics(crtpDownCharacteristicGuid);
                    if (downChars.Count >= 1)
                    {
                        crtpDownChar = downChars[0];
                    }
                }
            }

            return ((crtpService != null) && (crtpChar != null) && (crtpUpChar != null) && (crtpDownChar != null));
#else
            return true;
#endif
        }

        public void StartBthLink()
        {
            // Start timer which will send commander CRTP
            // packets every 50ms
            //BthWriteTimer = ThreadPoolTimer.CreatePeriodicTimer(
            //    BthWriteCommanderCrtpPacket,
            //    new TimeSpan(0, 0, 0, 0, 50));
        }

        public void StopBthLink()
        {
            //if(BthWriteTimer != null)
            //{
            //    BthWriteTimer.Cancel();
            //}
        }

        public void SetRoll(float percent)
        {
            //lock(m_Lock)
            //{
                roll = percent * 50;
            //}
        }

        public void SetPitch(float percent)
        {
            //lock(m_Lock)
            //{
                pitch = percent * 50;
            //}
        }

        public void SetYaw(float percent)
        {
            //lock(m_Lock)
            //{
                yaw = percent * 200;
            //}
        }

        public void SetThrust(float percent)
        {
            //lock (m_Lock)
            //{
                thrust = (ushort)(percent * 65535);
            //}
        }

        public async Task SetSetpoints(
            float rollPercent,
            float pitchPercent,
            float yawPercent,
            float thrustPercent)
        {
            roll = rollPercent * 50;
            pitch = pitchPercent * 50;
            yaw = yawPercent * 200;
            thrust = (ushort)(thrustPercent * 65535);


            CrtpControlPacket packet;

            packet.header = 0x30;
            packet.roll = roll;
            packet.pitch = pitch;
            packet.yaw = yaw;
            packet.thrust = thrust;

            int size = Marshal.SizeOf(packet);
            byte[] arr = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(packet, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            // Write the packet to the GATT characteristic

#if USING_BLUETOOTH

            GattCommunicationStatus status = await crtpChar.WriteValueAsync(
                        arr.AsBuffer(),
                        GattWriteOption.WriteWithResponse);

            if (GattCommunicationStatus.Unreachable == status)
            {
                return;
            }
            else
            { 
                return;
            }
#endif

        }

        

        // Handler for the periodic timer which constructs and sends a CRTP
        // commander packet over the BLE link
        private async void BthWriteCommanderCrtpPacket(ThreadPoolTimer timer)
        {
            // BLE header format:
            // Bit 7:       start (this will always be 1 for commander packets
            // Bit 5-6:     TID (increments on each transaction)
            // Bit 0:4:     size of payload (not including the BLE header) max 19
            //              For commander packets, this value is always 15

            // Construct header, start with 10001111 (0x8F) and OR in the TID
            //byte BleHeader = (byte)((byte)0x8F | (byte)((tid & (byte)(0x03)) << 5));
            //writer.WriteByte(BleHeader);

            // CRTP header format:
            // Bit 4-7:     Port
            // Bit 2-3:     Link
            // Bit 0-1:     Channel
            // For commander packets, the header is fixed at 0x30
            //byte crtpHeader = 0x30;
            //writer.WriteByte(crtpHeader);


            // If we are thrust locked (no packets have been sent yet)
            // send all zeros first to unlock the commander on the f/w
            //if (isThrustLocked)
            //{
            //    writer.WriteSingle(0); // Roll
            //    writer.WriteSingle(0); // Pitch
            //    writer.WriteSingle(0); // Yaw
            //    writer.WriteUInt16(0); // Thrust
            //}
            //else
            //{
            //lock (m_Lock)
            //{
            //if (thrust == 0)
            //{
            //    thrust = 10000;
            //}
            //else
            //{
            //    thrust = 0;
            //}

            //writer.WriteBytes(roll);

            //writer.WriteByte(0);
            //writer.WriteByte(0);
            //writer.WriteByte(0);
            //writer.WriteByte(0);


            //writer.WriteByte(0);
            //writer.WriteByte(0);
            //writer.WriteByte(0);
            //writer.WriteByte(0);


            //writer.WriteByte(0);
            //writer.WriteByte(0);
            //writer.WriteByte(0);
            //writer.WriteByte(0);

            //if (thrust == 10000)
            //{

            //    writer.WriteByte(16);
            //    writer.WriteByte(39);
            //}
            //else
            //{
            //    writer.WriteByte(0);
            //    writer.WriteByte(0);
            //}

            //writer.WriteSingle(roll);
            //writer.WriteSingle(pitch);
            //writer.WriteSingle(yaw);
            //writer.WriteUInt16(thrust);
            // }

            CrtpControlPacket packet;

            //lock(m_Lock)
            //{
                packet.header = 0x30;
                packet.roll = roll;
                packet.pitch = pitch;
                packet.yaw = yaw;
                packet.thrust = thrust;

//            thrust = (ushort)((thrust + 2500) % 30000);
            //}


            int size = Marshal.SizeOf(packet);
            byte[] arr = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(packet, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);

            //Debug.WriteLine(arr[14]);
            //arr[0];
            //writer.WriteBytes("Hello" + arr[0] + );
            

            // Write the packet to the GATT characteristic

#if USING_BLUETOOTH

        GattCommunicationStatus status = await crtpChar.WriteValueAsync(
                    arr.AsBuffer(),
                    GattWriteOption.WriteWithResponse);

            if (GattCommunicationStatus.Unreachable == status)
            {
                return;
            }
            else
            {
                // Write was successful, release thrust lock 
                // and increment the TID  
                //isThrustLocked = false;
                //tid = (byte)((tid + 1));
                //tid += 1;


                return;
            }

#else
#endif
        }
    }
}
