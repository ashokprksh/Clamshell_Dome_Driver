using System;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ASCOM.Utilities;

namespace ASCOM.AshsanDomelatest.Dome
{
    [ComVisible(false)]
    public partial class SetupDialogForm : Form
    {
        public string SelectedDomeComPort { get; set; } = ""; // Initialize to empty string
        public string SelectedTelescopeProgID { get; set; } = ""; // Initialize to empty string

        public SetupDialogForm()
        {
            try
            {
                InitializeComponent();
                PopulateTelescopeList();
                PopulateComPortList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing Setup Dialog: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw; // Re-throw to prevent the form from showing
            }
        }

        private void PopulateTelescopeList()
        {
            try
            {
                textBoxTelescopeProgID.Clear();

                using (Chooser chooser = new Chooser())
                {
                    chooser.DeviceType = "Telescope";
                    string selectedTelescope = chooser.Choose(SelectedTelescopeProgID);

                    if (!string.IsNullOrEmpty(selectedTelescope))
                    {
                        textBoxTelescopeProgID.Text = selectedTelescope;
                    }
                    else if (selectedTelescope == null)
                    {
                        MessageBox.Show("No telescope selected or available.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        textBoxTelescopeProgID.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error populating telescope list: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PopulateComPortList()
        {
            try
            {
                comboBoxDomeComPort.Items.Clear();

                string[] ports = SerialPort.GetPortNames();
                comboBoxDomeComPort.Items.AddRange(ports);

                if (comboBoxDomeComPort.Items.Count > 0)
                {
                    if (!string.IsNullOrEmpty(SelectedDomeComPort) && comboBoxDomeComPort.Items.Contains(SelectedDomeComPort))
                    {
                        comboBoxDomeComPort.SelectedItem = SelectedDomeComPort;
                    }
                    else
                    {
                        comboBoxDomeComPort.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error populating COM port list: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(comboBoxDomeComPort.SelectedItem?.ToString()))
            {
                MessageBox.Show("Please select a COM port for the dome.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SelectedDomeComPort = comboBoxDomeComPort.SelectedItem.ToString();
            SelectedTelescopeProgID = textBoxTelescopeProgID.Text;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void SetupDialogForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (DialogResult == DialogResult.OK)
            {
                Properties.Settings.Default.DomeComPort = SelectedDomeComPort;
                Properties.Settings.Default.TelescopeProgID = SelectedTelescopeProgID;
                Properties.Settings.Default.Save();

                // Add logging to verify settings are saved
                System.Diagnostics.Debug.WriteLine($"Saved COM Port: {SelectedDomeComPort}");
                System.Diagnostics.Debug.WriteLine($"Saved Telescope ProgID: {SelectedTelescopeProgID}");
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}