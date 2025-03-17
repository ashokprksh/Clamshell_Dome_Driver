using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using ASCOM.Utilities;
using System.Collections.Concurrent;
using ASCOM.DeviceInterface;
using System.Text;
using ASCOM.DriverAccess;
using System.IO;
using ASCOM.Astrometry.NOVASCOM;

namespace ASCOM.AshsanDomelatest.Dome
{
    public class DomeHardware : IDisposable
    {
        private SerialPort serialPort;
        private string comPort;
        private int baudRate = 9600;
        private bool connectedState = false;
        private TraceLogger tl;
        private ConcurrentQueue<string> receivedLines = new ConcurrentQueue<string>();
        private Task readTask;
        private CancellationTokenSource cts;
        public event Action<string> LineReceived;
        private StringBuilder _receiveBuffer = new StringBuilder();
        private DateTime lastStatusRequestTime = DateTime.MinValue;
        private DateTime lastFullStatusRequestTime = DateTime.MinValue; // Track last full status request
        private string lastReceivedStatus = null;

        public DomeHardware(TraceLogger logger)
        {
            tl = logger;
            serialPort = new SerialPort();
            comPort = "";
            //serialPort.ErrorReceived += SerialPort_ErrorReceived;
        }

        public void SetComPort(string comPort)
        {
            this.comPort = comPort;
            if (serialPort.IsOpen)
            {
                serialPort.Close();
            }
            serialPort.PortName = comPort;
            serialPort.BaudRate = baudRate;
        }

        public string GetComPort()
        {
            tl.LogMessage("DomeHardware.GetComPort", $"Returning COM port: {comPort}");
            return comPort;
        }

        public bool Connected
        {
            get => connectedState;
            set
            {
                if (value == connectedState) return;

                tl.LogMessage("DomeHardware.Connected", $"Setting Connected to {value}, comPort: {comPort}");

                if (value)
                {
                    tl.LogMessage("DomeHardware.Connected", $"Attempting to connect to COM port: {comPort}");

                    if (string.IsNullOrEmpty(comPort))
                    {
                        tl.LogMessage("DomeHardware.Connected", "COM port is not set.");
                        throw new ASCOM.NotConnectedException("COM port is not set.");
                    }

                    try
                    {
                        serialPort.PortName = comPort;
                        serialPort.BaudRate = baudRate;
                        serialPort.Open();
                        serialPort.DataReceived += SerialPort_DataReceived; // Subscribe here!
                        connectedState = true;
                        StartReading();
                        tl.LogMessage("DomeHardware.Connected", "Serial port opened.");
                    }
                    catch (Exception ex)
                    {
                        tl.LogMessage("DomeHardware.Connected", $"Error opening serial port: {ex.Message}");
                        connectedState = false;
                        throw new ASCOM.NotConnectedException("Failed to open serial port", ex);
                    }
                }
                else
                {
                    try
                    {
                        if (cts != null)
                        {
                            cts.Cancel();
                        }
                        if (readTask != null)
                        {
                            readTask.Wait();
                        }
                        serialPort.Close();
                        serialPort.DataReceived -= SerialPort_DataReceived; // Unsubscribe here!
                        connectedState = false;
                    }
                    catch (Exception ex)
                    {
                        tl.LogMessage("DomeHardware.Connected", $"Error closing serial port: {ex.Message}");
                    }
                    finally
                    {
                        if (cts != null)
                        {
                            cts.Dispose();
                            cts = null;
                        }
                        readTask = null;
                    }
                }
            }
        }



        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (serialPort == null) return;

            try
            {
                while (serialPort.BytesToRead > 0)
                {
                    char receivedChar = (char)serialPort.ReadChar();
                    _receiveBuffer.Append(receivedChar);

                    if (receivedChar == '\n') // Simplified line ending check
                    {
                        string line = _receiveBuffer.ToString().Trim();
                        _receiveBuffer.Clear();

                        tl.LogMessage("DomeHardware.LineReceived", $"Received: {line}");
                        receivedLines.Enqueue(line);
                    }
                }
            }
            catch (IOException ex) // More specific exception handling
            {
                tl.LogMessage("DomeHardware.SerialPort_DataReceived", $"Error: {ex.Message}");
            }
            catch (TimeoutException ex) // More specific exception handling
            {
                tl.LogMessage("DomeHardware.SerialPort_DataReceived", $"Error: {ex.Message}");
            }
        }

        private void StartReading()
        {
            if (cts != null) return;

            cts = new CancellationTokenSource();
            readTask = Task.Run(() =>
            {
                while (serialPort != null && serialPort.IsOpen && !cts.IsCancellationRequested)
                {
                    try
                    {
                        if (receivedLines.TryDequeue(out string line))
                        {
                            LineReceived?.Invoke(line);
                            tl.LogMessage("DomeHardware.StartReading", $"Received: {line}");
                        }
                        // Task.Delay(10, cts.Token).Wait(); // Removed delay
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (IOException ex) // More specific exception handling
                    {
                        tl.LogMessage("DomeHardware.StartReading", $"Error: {ex.Message}");
                        break;
                    }
                    catch (TimeoutException ex) // More specific exception handling
                    {
                        tl.LogMessage("DomeHardware.StartReading", $"Error: {ex.Message}");
                        break;
                    }
                }
            }, cts.Token);
        }

        public void SendCommand(string command)
        {
            tl.LogMessage("DomeHardware.SendCommand", $"SendCommand called, Connected: {Connected}, Time: {DateTime.Now:HH:mm:ss.fff}");

            if (!Connected) throw new ASCOM.NotConnectedException("Not connected to hardware");

            try
            {
                tl.LogMessage("DomeHardware.SendCommand", $"Attempting to send command: {command}, Time: {DateTime.Now:HH:mm:ss.fff}");

                if (!serialPort.IsOpen)
                {
                    tl.LogMessage("DomeHardware.SendCommand", "Serial port is not open.");
                    throw new ASCOM.NotConnectedException("Serial port is not open.");
                }

                serialPort.WriteLine(command);

                tl.LogMessage("DomeHardware.SendCommand", $"Command sent: {command}, Time: {DateTime.Now:HH:mm:ss.fff}");
            }
            catch (Exception ex)
            {
                tl.LogMessage("DomeHardware.SendCommand", $"Error sending command: {ex.Message}, Time: {DateTime.Now:HH:mm:ss.fff}");
                tl.LogMessage("DomeHardware.SendCommand", $"Stack Trace: {ex.StackTrace}");
                throw new ASCOM.DriverException("Error executing command", ex);
            }
        }

        // Add GetDomeStatus and ParseDomeStatus methods
        public string GetDomeStatus()
        {
            try
            {
                if (!Connected)
                {
                    throw new ASCOM.NotConnectedException("Not connected to hardware");
                }

                // Return the last received status message
                if (lastReceivedStatus != null)
                {
                    return lastReceivedStatus;
                }
                else
                {
                    return "STATUS:NoUpdate"; // Or a default message if no status has been received yet
                }
            }
            catch (Exception ex)
            {
                tl.LogMessage("DomeHardware.GetDomeStatus", $"Unexpected error getting status: {ex.Message}");
                return "ERROR:Unexpected";
            }
        }

        // Method to parse the status message
        public void ParseDomeStatus(string statusMessage, Dome dome)
        {
            if (string.IsNullOrEmpty(statusMessage))
            {
                tl.LogMessage("DomeHardware.ParseDomeStatus", "Received an empty status message.");
                return;
            }

            try
            {
                // Handle "STATUS:NoUpdate"
                if (statusMessage == "STATUS:NoUpdate")
                {
                    tl.LogMessage("DomeHardware.ParseDomeStatus", "No new status update.");
                    return; // or handle it differently if needed
                }

                string[] parts = null;

                if (statusMessage.StartsWith("STATUS:"))
                {
                    parts = statusMessage.Split(':');
                }
                else
                {
                    tl.LogMessage("DomeHardware.ParseDomeStatus", $"Unexpected status message format: {statusMessage}");
                    return;
                }

                if (parts != null && parts.Length >= 3)
                {
                    string shutterState = parts[1];
                    string parkState = parts[2];

                    ShutterState newShutterStatus = ShutterState.shutterError;

                    switch (shutterState)
                    {
                        case "Open":
                            newShutterStatus = ShutterState.shutterOpen;
                            break;
                        case "Closed":
                            newShutterStatus = ShutterState.shutterClosed;
                            break;
                        case "Opening":
                            newShutterStatus = ShutterState.shutterOpening;
                            break;
                        case "Closing":
                            newShutterStatus = ShutterState.shutterClosing;
                            break;
                        case "Stopped":
                            newShutterStatus = ShutterState.shutterError;
                            break;
                        default:
                            newShutterStatus = ShutterState.shutterError;
                            tl.LogMessage("DomeHardware.ParseDomeStatus", $"Unknown shutter state received: {shutterState}");
                            break;
                    }

                    if (dome._shutterStatus != newShutterStatus)
                    {
                        string mountParkStatus = dome.MountParked ? "Mount Parked" : "Mount Not Parked";
                        tl.LogMessage("DomeHardware.ParseDomeStatus", $"Shutter State Changed: {dome._shutterStatus} -> {newShutterStatus}, {mountParkStatus}");
                    }

                    dome._shutterStatus = newShutterStatus;

                    dome._atPark = (newShutterStatus == ShutterState.shutterClosed);
                }
                else
                {
                    tl.LogMessage("DomeHardware.ParseDomeStatus", $"Invalid status message format: {statusMessage}");
                }
            }
            catch (Exception ex)
            {
                tl.LogMessage("DomeHardware.ParseDomeStatus", $"Error parsing status: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Connected = false;
            serialPort?.Dispose();
        }
    }
}