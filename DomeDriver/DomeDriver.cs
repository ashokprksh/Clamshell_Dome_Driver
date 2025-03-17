// TODO fill in this information for your driver, then remove this line!
//
// ASCOM Dome driver for AshsanDomelatest
//
// Description:	 <To be completed by driver developer>
//
// Implements:	ASCOM Dome interface version: <To be completed by driver developer>
// Author:		(XXX) Your N. Here <your@email.here>
//

using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ASCOM;
using ASCOM.DeviceInterface;
using ASCOM.LocalServer;
using ASCOM.Utilities;
using ASCOM.DriverAccess;
using System.IO.Ports;
using System.Threading;
using System.Diagnostics;
using System.Collections.Concurrent;
using ASCOM.AshsanDomelatest.Dome.Properties;

namespace ASCOM.AshsanDomelatest.Dome
{
    //
    // This code is mostly a presentation layer for the functionality in the DomeHardware class. You should not need to change the contents of this file very much, if at all.
    // Most customisation will be in the DomeHardware class, which is shared by all instances of the driver, and which must handle all aspects of communicating with your device.
    //
    // Your driver's DeviceID is ASCOM.AshsanDomelatest.Dome
    //
    // The COM Guid attribute sets the CLSID for ASCOM.AshsanDomelatest.Dome
    // The COM ClassInterface/None attribute prevents an empty interface called _AshsanDomelatest from being created and used as the [default] interface
    //

    /// <summary>
    /// ASCOM Dome Driver for AshsanDomelatest.
    /// </summary>
    [ComVisible(true)]
    [Guid("81b0a8d5-fe8b-4a3b-a23e-b427d39e6148")]
    [ProgId("ASCOM.AshsanDomelatest.Dome")]
    [ServedClassName("AshanDomelatest")] // Driver description that appears in the Chooser, customise as required
    [ClassInterface(ClassInterfaceType.None)]
    public class Dome : ReferenceCountedObjectBase, IDomeV2, IDisposable
    {
        internal static string DriverProgId = "ASCOM.AshsanDomelatest.Dome";
        internal static string DriverDescription = "AshanDomelatest";
        private bool connectedState = false;
        private TraceLogger tl;
        public short InterfaceVersion => 2;
        private bool _slewing;
        private ITelescopeV3 mount; // Keep this for mount parking (if needed)
        internal ShutterState _shutterStatus; // Use the property for consistency
        internal bool _atPark; // Use the property for consistency
        private DomeHardware domeHardware; // Use DomeHardware class
        private string domeComPort;      // Store the dome's COM port
        private string telescopeProgID; // Store the telescope's ProgID (not COM port)
        private Telescope _telescope;     // The ASCOM Telescope object
        private bool mountConnected = false; // Track mount connection status separately
        private double _slewTimeout = 0;
        private bool _slaved;
        //public event Action<string> ArduinoStatusUpdated; // New event
        private string _domeId ;

        public Dome(string domeId)
        {
            _domeId = domeId;
            try
            {
                tl = new TraceLogger("Ashantest.Driver");
                domeComPort = Properties.Settings.Default.DomeComPort;
                telescopeProgID = Properties.Settings.Default.TelescopeProgID;
                tl.LogMessage("Dome.Dome", $"Loaded COM Port: {domeComPort}");
                tl.LogMessage("Dome.Dome", $"Loaded Telescope ProgID: {telescopeProgID}");
                domeHardware = new DomeHardware(tl);
                domeHardware.LineReceived += DomeHardware_LineReceived;
                tl.LogMessage("Dome.Dome", "domeHardware Initialized");
                if (string.IsNullOrEmpty(domeComPort) || string.IsNullOrEmpty(telescopeProgID))
                {
                    tl.LogMessage("Dome.Dome", "COM Port or Telescope ProgID is empty. Prompting user to run SetupDialog.");
                    MessageBox.Show("Please configure the Dome driver using the Setup Dialog.", "Configuration Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    if (!string.IsNullOrEmpty(telescopeProgID))
                    {
                        ConnectMount();
                    }
                }
            }
            catch (Exception ex)
            {
                tl.LogMessage("Dome.Dome", $"Error during Dome initialization: {ex.Message}");
            }
        }

        public Dome()
        {
            try
            {
                tl = new TraceLogger("Ashantest.Driver");
                domeComPort = Properties.Settings.Default.DomeComPort;
                telescopeProgID = Properties.Settings.Default.TelescopeProgID;
                tl.LogMessage("Dome.Dome", $"Loaded COM Port: {domeComPort}");
                tl.LogMessage("Dome.Dome", $"Loaded Telescope ProgID: {telescopeProgID}");
                domeHardware = new DomeHardware(tl);
                domeHardware.LineReceived += DomeHardware_LineReceived;
                tl.LogMessage("Dome.Dome", "domeHardware Initialized");
                if (string.IsNullOrEmpty(domeComPort) || string.IsNullOrEmpty(telescopeProgID))
                {
                    tl.LogMessage("Dome.Dome", "COM Port or Telescope ProgID is empty. Prompting user to run SetupDialog.");
                    MessageBox.Show("Please configure the Dome driver using the Setup Dialog.", "Configuration Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    if (!string.IsNullOrEmpty(telescopeProgID))
                    {
                        ConnectMount();
                    }
                }
            }
            catch (Exception ex)
            {
                tl.LogMessage("Dome.Dome", $"Error during Dome initialization: {ex.Message}");
            }
        }

        public void Stop()
        {
            tl.LogMessage("Dome.Stop", "Stop command received.");
            try
            {
                domeHardware.SendCommand("STOP");
                tl.LogMessage("Dome.Stop", "Stop command sent to DomeHardware.");
            }
            catch (Exception ex)
            {
                tl.LogMessage("Dome.Stop", $"Error stopping dome: {ex.Message}");
            }
        }

        private void Initialize()
        {
            try
            {
                tl = new TraceLogger("Ashantest.Driver");
                domeComPort = Properties.Settings.Default.DomeComPort;
                telescopeProgID = Properties.Settings.Default.TelescopeProgID;
                tl.LogMessage("Dome.Dome", $"Loaded COM Port: {domeComPort}");
                tl.LogMessage("Dome.Dome", $"Loaded Telescope ProgID: {telescopeProgID}");
                domeHardware = new DomeHardware(tl);
                domeHardware.LineReceived += DomeHardware_LineReceived;
                tl.LogMessage("Dome.Dome", "domeHardware Initialized");
                if (string.IsNullOrEmpty(domeComPort) || string.IsNullOrEmpty(telescopeProgID))
                {
                    tl.LogMessage("Dome.Dome", "COM Port or Telescope ProgID is empty. Prompting user to run SetupDialog.");
                    MessageBox.Show("Please configure the Dome driver using the Setup Dialog.", "Configuration Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    if (!string.IsNullOrEmpty(telescopeProgID))
                    {
                        ConnectMount();
                    }
                }
            }
            catch (Exception ex)
            {
                tl.LogMessage("Dome.Dome", $"Error during Dome initialization: {ex.Message}");
            }
        }

        public DomeHardware DomeHardware
        {
            get { return domeHardware; }
        }

        private bool MountIsEssential()

        {

            // If the mount is required for ANY dome operations (e.g., parking, closing shutter),

            // then return true.  If the dome can function independently of the mount,

            // (e.g., the mount can be parked manually, or your dome doesn't require parking)

            // then return false.

            return true; // Or false, depending on your driver's needs.

        }

        private void ConnectMount()
        {
            if (string.IsNullOrEmpty(telescopeProgID))
            {
                tl.LogMessage("Dome.ConnectMount", "Telescope ProgID is null or empty.");
                mountConnected = false;  // Ensure this is set to false
                mount = null;
                return; // Or throw an exception if you prefer
            }

            try
            {
                tl.LogMessage("Dome.ConnectMount", $"Attempting to connect to mount: {telescopeProgID}");

                if (_telescope == null) // Create only *once*
                {
                    _telescope = new Telescope(telescopeProgID);
                }

                _telescope.Connected = true; // Attempt to connect
                mountConnected = _telescope.Connected; // Update the flag

                if (mountConnected)
                {
                    tl.LogMessage("Dome.ConnectMount", "Successfully connected to mount.");
                    mount = _telescope; // Assign the connected telescope object
                }
                else
                {
                    tl.LogMessage("Dome.ConnectMount", "Mount connection failed.");
                    _telescope.Dispose(); // Dispose on failure
                    _telescope = null;
                    mount = null;
                    mountConnected = false; // Important: set this to false!
                    if (MountIsEssential())
                    {
                        throw new ASCOM.DriverException("Failed to connect to the mount.");
                    }
                }
            }
            catch (COMException ex) // Example: Handle COM exceptions specifically
            {
                tl.LogMessage("Dome.ConnectMount", $"COM Exception connecting to mount: {ex.Message}");
                tl.LogMessage("Dome.ConnectMount", $"COM Exception Stack Trace: {ex.StackTrace}");
                _telescope?.Dispose();
                _telescope = null;
                mount = null;
                mountConnected = false;
                if (MountIsEssential())
                {
                    throw new ASCOM.DriverException("Failed to connect to the mount.", ex); // Re-throw with inner exception
                }
            }
            catch (Exception ex) // Catch other exceptions
            {
                tl.LogMessage("Dome.ConnectMount", $"Exception connecting to mount: {ex.Message}");
                tl.LogMessage("Dome.ConnectMount", $"Exception Type: {ex.GetType().Name}"); // Log exception type!
                tl.LogMessage("Dome.ConnectMount", $"Exception Stack Trace: {ex.StackTrace}"); // Log stack trace!
                _telescope?.Dispose();
                _telescope = null;
                mount = null;
                mountConnected = false;
                if (MountIsEssential())
                {
                    throw new ASCOM.DriverException("Failed to connect to the mount.", ex); // Re-throw with inner exception
                }
            }
        }

       


        public void SetupDialog()
        {
            using (SetupDialogForm setupForm = new SetupDialogForm())
            {
                if (setupForm.ShowDialog() == DialogResult.OK)
                {
                    domeComPort = setupForm.SelectedDomeComPort;
                    telescopeProgID = setupForm.SelectedTelescopeProgID;

                    Properties.Settings.Default.DomeComPort = domeComPort;
                    Properties.Settings.Default.TelescopeProgID = telescopeProgID;
                    Properties.Settings.Default.Save();
                    if (domeHardware != null)
                    {
                        tl.LogMessage("Dome.SetupDialog", $"Setting domeHardware COM port to: {domeComPort}");
                        domeHardware.SetComPort(domeComPort);
                        tl.LogMessage("Dome.SetupDialog", $"domeHardware COM port set to: {domeHardware.GetComPort()}");
                    }
                    else
                    {
                        tl.LogMessage("Dome.SetupDialog", "Error: domeHardware is null. Cannot set COM port.");
                        MessageBox.Show("Error: Dome hardware is not initialized.", "Setup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    if (setupForm.SelectedTelescopeProgID != telescopeProgID)
                    {
                        telescopeProgID = setupForm.SelectedTelescopeProgID;
                        if (_telescope != null)
                        {
                            _telescope.Connected = false; // Disconnect old telescope
                            _telescope.Dispose();
                            _telescope = null;
                            mount = null;
                            mountConnected = false; // Set this to false!
                        }

                        if (!string.IsNullOrEmpty(telescopeProgID))
                        {
                            ConnectMount(); // Reconnect to new telescope
                        }
                    }
                }
            }
        }
    
            





        public void SelectTelescope()
        {
            using (Chooser chooser = new Chooser())
            {
                chooser.DeviceType = "Telescope";
                telescopeProgID = chooser.Choose(telescopeProgID);

                if (telescopeProgID != null)
                {
                    tl.LogMessage("Dome.SelectTelescope", $"Selected Telescope ProgID: {telescopeProgID}");
                    if (_telescope != null)
                    {
                        _telescope.Connected = false;
                        _telescope.Dispose();
                        _telescope = null;
                        mount = null;
                        mountConnected = false;
                    }
                    ConnectMount(); // Connect after selecting a new telescope
                }
                else
                {
                    tl.LogMessage("Dome.SelectTelescope", "No telescope selected.");
                }
            }
        }

        public string GetTelescopeProgID()

        {

            return telescopeProgID;

        }

        public bool Connected
        {
            get => connectedState;
            set
            {
                tl.LogMessage("Dome.Connected", $"Setting Connected to {value}");
                tl.LogMessage("Dome.Connected", $"Stack Trace: {System.Environment.StackTrace}");

                if (value == connectedState) return; // No change needed

                if (value) // Connect
                {
                    try
                    {
                        // Set the COM port, even if already connected:
                        if (domeHardware != null && !string.IsNullOrEmpty(domeComPort))
                        {
                            domeHardware.SetComPort(domeComPort); // Corrected: Set COM port here!
                            tl.LogMessage("Dome.Connected", $"DomeHardware COM port set to: {domeComPort}");
                        }
                        else
                        {
                            tl.LogMessage("Dome.Connected", "Error: domeHardware is null or domeComPort is empty.");
                            throw new ASCOM.DriverException("Dome hardware or COM port not configured.");
                        }

                        domeHardware.Connected = true; // Corrected: Use DomeHardware's Connected!
                        connectedState = true;

                        if (!mountConnected && !string.IsNullOrEmpty(telescopeProgID))
                        {
                            ConnectMount();
                        }

                        if (!mountConnected && MountIsEssential())
                        {
                            domeHardware.Connected = false; // Disconnect DomeHardware if mount is essential but not connected
                            connectedState = false;
                            throw new ASCOM.DriverException("Telescope mount is required but not connected.");
                        }
                    }
                    catch (Exception ex)
                    {
                        domeHardware.Connected = false; // Disconnect on error
                        connectedState = false; // Ensure correct state
                        tl.LogMessage("Dome.Connected", $"Error connecting: {ex.Message}");
                        throw; // Re-throw
                    }
                }
                else // Disconnect
                {
                    try
                    {
                        domeHardware.Connected = false; // Corrected: Use DomeHardware's Connected!
                        connectedState = false; // Ensure correct state

                        if (_telescope != null)
                        {
                            _telescope.Connected = false;
                            _telescope.Dispose();
                            _telescope = null;
                            mount = null;
                            mountConnected = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        tl.LogMessage("Dome.Connected", $"Error disconnecting: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        public bool IsTelescopeParked()

        {

            if (string.IsNullOrEmpty(telescopeProgID) || mount == null) return true; // Handle null mount



            try

            {

                return mount.AtPark;

            }

            catch (Exception ex)

            {

                tl.LogMessage("Dome.IsTelescopeParked", $"Error checking telescope park status: {ex.Message}");

                return true; // Or throw an exception if you prefer

            }

        }

        public bool MountParked
        {
            get { return mount != null && mount.AtPark; }
        }

        public void ParkTelescope()

        {

            if (string.IsNullOrEmpty(telescopeProgID) || mount == null) return; // Handle null mount



            try

            {

                mount.Park();

            }

            catch (Exception ex)

            {

                tl.LogMessage("Dome.ParkTelescope", $"Error parking telescope: {ex.Message}");

            }

        }

        public void OpenShutter()
        {
            if (mount == null)
            {
                tl.LogMessage("Dome.OpenShutter", "Mount is null. Throwing exception.");
                throw new ASCOM.DriverException("Mount is not available. Shutter cannot be opened.");
            }

            tl.LogMessage("Dome.OpenShutter", "Opening Shutter - Start"); // Log start

            try
            {
                tl.LogMessage("Dome.OpenShutter", "Sending OPENSHUTTER command to DomeHardware.");
                domeHardware.SendCommand("OPENSHUTTER");
                tl.LogMessage("Dome.OpenShutter", "OPENSHUTTER command sent successfully.");
            }
            catch (ASCOM.DriverException ascomEx)
            {
                tl.LogMessage("Dome.OpenShutter", $"ASCOM Driver Exception: {ascomEx.Message}");
                throw; // Re-throw the ASCOM exception
            }
            catch (Exception ex)
            {
                tl.LogMessage("Dome.OpenShutter", $"General Exception: {ex.Message}, Stack Trace: {ex.StackTrace}");
                throw new ASCOM.DriverException("Error opening shutter: " + ex.Message);
            }

            tl.LogMessage("Dome.OpenShutter", "Opening Shutter - End"); // Log end
        }



        public void CloseShutter()
        {
            tl.LogMessage("Dome.CloseShutter", "Closing Shutter - Start"); // Log start

            if (!mountConnected && MountIsEssential())
            {
                tl.LogMessage("Dome.CloseShutter", "Mount is not connected. Throwing exception.");
                throw new ASCOM.DriverException("Mount is not connected. Shutter cannot be closed.");
            }

            if (mount != null && !mount.AtPark)
            {
                tl.LogMessage("Dome.CloseShutter", "Mount is not parked. Parking mount...");
                if (!ParkMount()) // Park the mount FIRST
                {
                    tl.LogMessage("Dome.CloseShutter", "Mount did not park correctly. Throwing exception.");
                    throw new ASCOM.DriverException("Mount did not park correctly. Shutter cannot be closed.");
                }
                else
                {
                    tl.LogMessage("Dome.CloseShutter", "Mount parked successfully.");
                }
            }
            else
            {
                tl.LogMessage("Dome.CloseShutter", "Mount either null or already parked.");
            }

            // Now that the mount is guaranteed to be parked (or checked), close the shutter.
            try
            {
                tl.LogMessage("Dome.CloseShutter", "Sending CLOSESHUTTER command to DomeHardware.");
                domeHardware.SendCommand("CLOSESHUTTER");
                tl.LogMessage("Dome.CloseShutter", "CLOSESHUTTER command sent successfully.");
            }
            catch (ASCOM.DriverException ascomEx)
            {
                tl.LogMessage("Dome.CloseShutter", $"ASCOM Driver Exception: {ascomEx.Message}");
                throw; // Re-throw the ASCOM exception
            }
            catch (Exception ex)
            {
                tl.LogMessage("Dome.CloseShutter", $"General Exception: {ex.Message}, Stack Trace: {ex.StackTrace}");
                throw new ASCOM.DriverException("Error closing shutter: " + ex.Message);
            }

            tl.LogMessage("Dome.CloseShutter", "Closing Shutter - End"); // Log end
        }



        public ShutterState ShutterStatus // Rename to ShutterStatus
        {
            get
            {
                if (domeHardware != null && domeHardware.Connected)
                {
                    string status = domeHardware.GetDomeStatus();
                    domeHardware.ParseDomeStatus(status, this);
                }
                return _shutterStatus;
            }
            set { _shutterStatus = value; }
        }






        public bool Slewing
        {
            get => _slewing;
            private set
            {
                if (_slewing != value)
                {
                    _slewing = value;
                    tl.LogMessage("Dome.Slewing", $"Slewing set to: {_slewing}");
                }
            }
        }





        public void Park()
        {
            tl.LogMessage("Dome.Park", "Start");
            tl.LogMessage("Dome.Park", "Calling Park");
            try
            {
                if (mount == null || !mountConnected) // Check both mount and connection status
                {
                    throw new ASCOM.DriverException("Mount is not connected. Dome cannot be parked.");
                }

                mount.Park(); // Send the park command to the mount

                int timeoutSeconds = 30; // Set a reasonable timeout (configurable in setup)
                for (int i = 0; i < timeoutSeconds; i++)
                {
                    Thread.Sleep(1000); // Wait 1 second
                    if (mount != null && mount.AtPark) // Check if mount is still valid!
                    {
                        tl.LogMessage("Dome.Park", "Mount successfully parked.");
                        AtPark = true; // Set Dome's AtPark ONLY here, AFTER confirmation!
                        return; // Exit the Park method
                    }
                    tl.LogMessage("Dome.Park", $"Waiting for mount to park (attempt {i + 1}/{timeoutSeconds})");
                }

                tl.LogMessage("Dome.Park", "Mount did not park within the timeout.");
                throw new ASCOM.DriverException("Mount did not park within the timeout period.");
            }
            catch (Exception ex)
            {
                tl.LogMessage("Dome.Park", $"Error parking mount: {ex.Message}");
                tl.LogMessage("Dome.Park", $"Stack Trace: {ex.StackTrace}"); // Log the stack trace!
                throw new ASCOM.DriverException($"Error parking mount: {ex.Message}"); // Re-throw the exception
            }
        }



        public void Unpark() // Dome Unpark

        {

            tl.LogMessage("Dome.Unpark", "Unparking Dome");

            OpenShutter(); // Open shutter to unpark the dome

            //AtPark = ShutterStatus != ShutterState.shutterClosed; // Update AtPark status

        }





        private void DomeHardware_LineReceived(string line)
        {
            domeHardware.ParseDomeStatus(line, this); // Corrected call
            tl.LogMessage("Dome.LineReceived", $"Received: {line}");
        }



        private bool ParkMount() // Improved ParkMount with timeout

        {

            if (mount == null || !mountConnected) return false;



            try

            {

                tl.LogMessage("Dome.ParkMount", "Starting mount parking attempt.");



                mount.Park();



                int timeoutSeconds = 30; // Configurable timeout

                for (int i = 0; i < timeoutSeconds; i++)

                {

                    if (mount.AtPark)

                    {

                        tl.LogMessage("Dome.ParkMount", "Mount successfully parked.");

                        return true;

                    }

                    Thread.Sleep(1000);

                    tl.LogMessage("Dome.ParkMount", $"Checking mount park status (attempt {i + 1}/{timeoutSeconds}).");

                }



                tl.LogMessage("Dome.ParkMount", "Mount did not park within the timeout period.");

                return false;

            }

            catch (Exception ex)

            {

                tl.LogMessage("Dome.ParkMount", $"Error parking mount: {ex.Message}");

                return false;

            }

        }



        public void Dispose()

        {

            if (Connected)

            {

                Connected = false;

            }



            domeHardware?.Dispose();

            _telescope?.Dispose(); // Dispose the telescope object

            mount = null;

            mountConnected = false;

            if (tl != null)

            {

                tl.Enabled = false;

                tl.Dispose();

                tl = null;

            }

        }





        public ArrayList SupportedActions

        {

            get

            {

                tl.LogMessage("SupportedActions Get", "Returning empty arraylist");

                return new ArrayList(); // Or add your supported actions if any

            }

        }



        public string Action(string actionName, string actionParameters)

        {

            tl.LogMessage("Action", "Action method called - not implemented");

            throw new ASCOM.MethodNotImplementedException("Action");

        }



        public void CommandBlind(string Command, bool Raw)

        {

            throw new ASCOM.MethodNotImplementedException("CommandBlind");

        }



        public bool CommandBool(string Command, bool Raw)

        {

            throw new ASCOM.MethodNotImplementedException("CommandBool");

        }



        public string CommandString(string Command, bool Raw)

        {

            throw new ASCOM.MethodNotImplementedException("CommandString");

        }



        public string Description => "Observatory Dome Driver";



        public string DriverInfo => "Driver for controlling the observatory dome";



        public string DriverVersion => "2.0";



        public string Name => "Clamshell Dome";



        public void AbortSlew()
        {
            tl.LogMessage("Dome.AbortSlew", "Aborting Slew");
            Slewing = false; // Or send a stop command to your hardware if needed
        }


        public void SetPark()
        {
            tl.LogMessage("Dome.SetPark", "Parking Dome...");

            try
            {
                CloseShutter(); // Close the shutter (essential)
                tl.LogMessage("Dome.SetPark", $"ShutterStatus after CloseShutter: {ShutterStatus}");

                int timeoutSeconds = 120; // Increased timeout - Make configurable in setup!
                for (int i = 0; i < timeoutSeconds; i++)
                {
                    Thread.Sleep(1000);

                    bool shutterClosed = (ShutterStatus == ShutterState.shutterClosed); //corrected here
                    bool mountParked = (mount != null && mount.AtPark); // Only if mount is part of park criteria

                    tl.LogMessage("Dome.SetPark", $"Attempt {i + 1}: ShutterClosed={shutterClosed}, MountParked={mountParked}");

                    if (shutterClosed && mountParked) // Check ALL criteria
                    {
                        AtPark = true; // Set AtPark ONLY when all conditions are met
                        tl.LogMessage("Dome.SetPark", "Dome parked.");
                        return;
                    }
                }

                tl.LogMessage("Dome.SetPark", "Dome did not reach parked state within timeout.");
                throw new ASCOM.DriverException("Dome did not reach parked state within timeout.");

            }
            catch (Exception ex)
            {
                tl.LogMessage("Dome.SetPark", $"Error parking dome: {ex.Message}");
                tl.LogMessage("Dome.SetPark", $"Stack Trace: {ex.StackTrace}"); // Log the stack trace!
                throw new ASCOM.DriverException("Error parking dome: " + ex.Message);
            }
        }
        public bool AtPark
        {
            get => _atPark;
            private set
            {
                if (_atPark != value)
                {
                    _atPark = value;
                    tl.LogMessage("Dome.AtPark", $"AtPark set to: {_atPark}");
                }
            }
        }

        public void SlewToAltitude(double Altitude)
        {
            throw new ASCOM.PropertyNotImplementedException("SlewToAltitude is not implemented for this dome.");
        }

        public void SlewToAzimuth(double Azimuth)
        {
            throw new ASCOM.PropertyNotImplementedException("SlewToAzimuth is not implemented for this dome.");
        }

        public void SyncToAzimuth(double Azimuth)
        {
            throw new ASCOM.PropertyNotImplementedException("SyncToAzimuth is not implemented for this dome.");
        }


        public double Altitude => 0; // Implement if needed

        public bool AtHome
        {
            get => false; // Or your actual implementation if available
        }



        public double Azimuth
        {
            get => 0; // Or your actual implementation if available
        }

        public bool CanFindHome
        {
            get
            {
                return false;
            }
        }

        public bool CanPark
        {
            get
            {
                return true;
            }
        }

        public bool CanSetAltitude
        {
            get
            {
                return false;
            }
        }
        public bool CanSetAzimuth
        {
            get
            {
                return false;
            }
        }

        public bool CanSetPark
        {
            get
            {
                return true;
            }
        }

        public bool CanSetShutter => true;

        public bool CanOpen => true;

        public bool CanClose => true;

        public bool CanSlave
        {
            get
            {
                return false;
            }
        }

        public bool CanSyncAzimuth
        {
            get
            {
                return false;
            }
        }

        public bool Slaved
        {
            get => _slaved;
            set
            {
                tl.LogMessage("Dome.Slaved", $"Setting Slaved to: {value}");

                if (CanSlave)
                {
                    _slaved = value; 
                    tl.LogMessage("Dome.Slaved", $"CanSlave is true, Slaved set to: {value}");
                }
                else 
                {
                    if (value) 
                    {
                        throw new ASCOM.PropertyNotImplementedException("Slaved", true);
                    }
                    else 
                    {
                        tl.LogMessage("Dome.Slaved", $"CanSlave is false, and Slaved set to false. Doing nothing (no-op).");
                        _slaved = false; 
                    }
                }
            }
        }



        public void FindHome()
        {
            throw new ASCOM.PropertyNotImplementedException("FindHome is not implemented for this dome.");
        }

        public double SlewTimeout
        {
            get => _slewTimeout;
            set => _slewTimeout = value; // Or remove the setter if not supported
        }



    }

}
