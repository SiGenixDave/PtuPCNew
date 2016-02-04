#region --- Revision History ---
/*
 * 
 *  This document and its contents are the property of Bombardier Inc. or its subsidiaries and contains confidential, proprietary information.
 *  The reproduction, distribution, utilization or the communication of this document, or any part thereof, without express authorization is strictly prohibited.  
 *  Offenders will be held liable for the payment of damages.
 * 
 *  (C) 2010    Bombardier Inc. or its subsidiaries. All rights reserved.
 * 
 *  Solution:   PTU
 * 
 *  Project:    PTU Application
 * 
 *  File name:  FormSelectTargetLogic.cs
 * 
 *  Revision History
 *  ----------------
 * 
 *  Date        Version Author          Comments
 *  05/09/10    1.0     K.McD           1.  First entry into TortoiseSVN.
 * 
 *  08/25/10    1.1     K.McD           1.  Inherited ApplicationWindow property renamed to MainWindow.
 * 
 *  10/08/10    1.2     K.McD           1.  Bug fix SNCR001.28. Included a try catch block within the Shown event handler in case the GetTargets() method throws an
 *                                          exception.
 * 
 *  10/15/10    1.3     K.McD           1.  Modified to use the CommunicationApplication class.
 *  
 *  09/12/11	1.4		Sean.D			1.	Added code to check whether access should be made by serial port, Ethernet, or both, and to attempt the Ethernet access if
 *                                          present.
 *
 *  09/21/11	1.4.1	Sean.D			1.	Changed code in GetTargets to print appropriate status messages while attempting to connect to URIs.
 *
 *  11/04/15    1.5     K.McD           References
 *                                      1.  Provide feedback to the user while the URI scan is in progress. Include progress bar and list current target URI.
 *                                      
 *                                      Modifications
 *                                      1.  Applied 'Organize Using - Remove and Sort'.
 *                                      2.  Added m_CancelSelected flag to indicate that the Cancel button was selected.
 *                                      3.  Modified the Cancel button event handler to assert m_CancelSelected and to only call the Close() method if
 *                                          the URI scan is complete.
 *                                      4.  Significant modifications to the GetTargets() method:
 *                                          1.  Included a number of calls to the statusInformation.Update() method to ensure that the status information is updated.
 *                                          2.  Added support for the URI scan ProgressBar control and the associated legend.
 *                                          3.  Removed the statusInformation property updates in the catch statements as they are overwritten too quickly to be seen.
 *                                          4.  Ensured that the OK button is disabled during the scan.
 *                                          5.  Added to Application.DoEvents() call at the beginning of each scan cycle.
 *                                          6.  Included check within each cycle of the scan to determine if the Cancel button has been selected by the user and, if
 *                                              so, to terminate the scan.
 */
#endregion --- Revision History ---

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

using Bombardier.PTU.Communication;
using Bombardier.PTU.Properties;
using Common;
using Common.Communication;
using Common.Configuration;
using Common.Forms;
using Microsoft.Win32;

namespace Bombardier.PTU.Forms
{
    /// <summary>
    /// Form to allow the user to select the logic controller with which the PTU is to communicate. 
    /// </summary>
    public partial class FormSelectTarget : FormPTUDialog
    {
        #region --- Constants ---
        /// <summary>
        /// The total number of characters associated with the serial communication device, for padding purposes. Value: 5.
        /// </summary>
        public const int ComDeviceTotalCharacters = 5;

        /// <summary>
        /// The LocalMachine registry key that defines serial communication device names. Value: "HARDWARE\DEVICEMAP\SERIALCOMM". 
        /// </summary>
        public const string RegistryKeySerialCommunication = @"HARDWARE\DEVICEMAP\SERIALCOMM";

        #region - [SerialCommRegistry Keys that are to be excluded from the search] - 
        /// <summary>
        /// A serial communication registry key found on the PhilipsX58 laptop that is to be excluded from the search.
        /// </summary>
        public const string SerialCommRegistryKeyWinachsf0 = "Winachsf0";
        #endregion - [SerialCommRegistry Keys that are to be excluded from the search] -
        
        /// <summary>
        /// The mnemonic used to specify that the COM port is a virtual COM port. Value: "VCP".
        /// </summary>
        private const string VirtualComPort = "VCP";
        #endregion --- Constants ---

        #region --- Member Variables ---
        /// <summary>
        /// Flag to indicate whether a target logic controller has been selected; true indicates that a target has been seected, otherwise false.
        /// </summary>
        private bool m_TargetSelected;

        /// <summary>
        /// The target configuration information associated with the selected target.
        /// </summary>
        private TargetConfiguration_t m_TargetConfiguration;

        /// <summary>
        /// The communication setting associated with the selected target.
        /// </summary>
        private CommunicationSetting_t m_CommunicationSetting;

        /// <summary>
        /// The list of target configuration information associated with the target logic controllers that were found.
        /// </summary>
        List<TargetConfiguration_t> m_TargetConfigurationList;

        /// <summary>
        /// The list of communication settings associated with the target logic controllers that were found. 
        /// </summary>
        List<CommunicationSetting_t> m_CommunicationSettingList;

        /// <summary>
        /// A flag to indicate that the Cancel button been selected.
        /// </summary>
        private bool m_CancelSelected = false;
        #endregion --- Member Variables ---

        #region --- Constructors ---
        /// <summary>
        /// Initialize a new instance of the form.
        /// </summary>
        public FormSelectTarget()
        {
            InitializeComponent();
        }
        #endregion --- Constructors ---

        #region --- Cleanup ---
        /// <summary>
        /// Clean up the resources used by the form.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Cleanup(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    // Cleanup managed objects by calling their Dispose() methods.
                    if (components != null)
                    {
                        components.Dispose();
                    }
                }

                // Whether called by consumer code or the garbage collector free all unmanaged resources and set the value of managed data members to null.

                #region --- Windows Form Designer Variables ---
                // Detach the event handler delegates.

                // Set the Windows Form Designer Variables to null.

                #endregion --- Windows Form Designer Variables ---
            }
            catch (Exception)
            {
                // Don't do anything, just ensure that an exception isn't thrown.
            }
            finally
            {
                base.Cleanup(disposing);
            }
        }
        #endregion --- Cleanup ---

        #region --- Delegated Methods ---
        #region - [FORM] -
        /// <summary>
        /// Event handler for the <c>Shown</c> event.
        /// </summary>
        /// <param name="sender">Reference to the object that raised the event.</param>
        /// <param name="e">Parameter passed from the object that raised the event.</param>
        private void FormSelectTargetLogic_Shown(object sender, EventArgs e)
        {
            this.Update();

            Debug.Assert(MainWindow != null);

            // Scan each of the available serial communication ports for target hardware.
            DialogResult dialogResult = DialogResult.Cancel;
            do
            {
                try
                {
                    if (GetTargets(m_ListBoxAvailableLogicControllers, m_TxtStatusInformation, out m_TargetConfigurationList, out m_CommunicationSettingList) == true)
                    {
                        break;
                    }
                    else
                    {
                        dialogResult = MessageBox.Show(Resources.MBTLogicsNotFound, Resources.MBCaptionInformation, MessageBoxButtons.RetryCancel,
                                                       MessageBoxIcon.Information);
                        Update();
                        if (dialogResult == DialogResult.Cancel)
                        {
                            Close();
                        }
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show(Resources.MBTSerialDeviceNotFound, Resources.MBCaptionError, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Close();
                }
            }
            while (dialogResult == DialogResult.Retry);
        }
        #endregion - [FORM] -

        /// <summary>
        /// Event handler for the cancel button <c>Click</c> event. Closes the form.
        /// </summary>
        /// <param name="sender">Reference to the object that raised the event.</param>
        /// <param name="e">Parameter passed from the object that raised the event.</param>
        private void m_Cancel_Click(object sender, EventArgs e)
        {
            // Assert the flag that indicates that the Cancel button has been selected so that the scan can be terminated, if need be.
            m_CancelSelected = true;

            // Only close the form if the scan is complete.
            if (m_BtnOK.Enabled == true)
            {
            Close();
        }
        }

        /// <summary>
        /// Event handler for the OK button <c>Click</c> event. Updates the properties with the selected target logic and communication settings.
        /// </summary>
        /// <param name="sender">Reference to the object that raised the event.</param>
        /// <param name="e">Parameter passed from the object that raised the event.</param>
        private void m_BtnOK_Click(object sender, EventArgs e)
        {

            if (m_ListBoxAvailableLogicControllers.SelectedItems.Count == 0)
            {
                // No target was selected.
                MessageBox.Show(Resources.MBTSelectTarget, Resources.MBCaptionInformation, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int selectedLogic = m_ListBoxAvailableLogicControllers.SelectedIndex;
            m_TargetSelected = true;

            m_TargetConfiguration = m_TargetConfigurationList[selectedLogic];
            m_CommunicationSetting = m_CommunicationSettingList[selectedLogic];
            Close();
        }

        #endregion --- Delegated Methods ---

        #region --- Methods ---
        /// <summary>
        /// Scans each of the communication ports listed in the registry to determine if it is connected to target hardware and generates a list
        /// of the target configuration information and the communication settings for each target that is located. 
        /// </summary>
        /// <param name="targetConfigurationList">The list containing the target configuration information for any targets connected to the PTU.</param>
        /// <param name="communicationSettingList">The communication settings associated with each target that was found.</param>
        /// <param name="listBoxTargetsFound">The <c>ListBox</c> control on which the target names are to be displayed.</param>
        /// <param name="statusInformation">The control on which the status information is to be displayed.</param>
        /// <returns>A flag to indicate whether one or more targets were found; true, if  targets were found, otherwise, false.</returns>
        public bool GetTargets(ListBox listBoxTargetsFound, Control statusInformation,  out List<TargetConfiguration_t> targetConfigurationList,
                               out List<CommunicationSetting_t> communicationSettingList)
        {
            // Instantiate to output parameters.
            communicationSettingList = new List<CommunicationSetting_t>();
            targetConfigurationList = new List<TargetConfiguration_t>();

            CommunicationApplication communicationInterface;

            // Flag to indicate whether target hardware was found; true, if one or more targets were found, otherwise, false.
            bool targetFound = false;

			if (Parameter.CommunicationType == Parameter.CommunicationTypeEnum.Both || Parameter.CommunicationType == Parameter.CommunicationTypeEnum.RS232)
			{

				// -----------------------------------------------------------------
				// Scan each serial communication (COM) port listed in the Registry.
				// -----------------------------------------------------------------

				// Get the list of available serial COM ports from the registry.
				RegistryKey root = Registry.LocalMachine;
				CommunicationSetting_t communicationSetting = new CommunicationSetting_t();

				// Set the protocol to serial communication.
				communicationSetting.Protocol = Protocol.RS232;

				// Initialize the serial communication parameters.
				communicationSetting.SerialCommunicationParameters.SetToDefault();

				TargetConfiguration_t targetConfiguration;
				using (RegistryKey serialCommunication = root.OpenSubKey(RegistryKeySerialCommunication))
				{
					// Scan each port in the Registry.
					foreach (string valueName in serialCommunication.GetValueNames())
					{
						// Filter out those '\HKEY_LOCAL_MACHINE\HARDWARE\DEVICEMAP\SERIALCOMM' keys that are not to be included in the search.
						switch (valueName)
						{
							// Skip those registry keys defined here.
							case SerialCommRegistryKeyWinachsf0:
								continue;
							default:
								// Process the key.
								break;
						}

						string value = serialCommunication.GetValue(valueName).ToString();

						communicationSetting.Port.Name = value;
						communicationSetting.Port.FullSpecification = value.PadRight(ComDeviceTotalCharacters) + " - " + valueName;
						communicationSetting.Port.Type = (communicationSetting.Port.FullSpecification.Contains(VirtualComPort)) ? PortType.VCP : PortType.COM;

						// Determine the port identifier, this is a 16 bit Unicode string representation of the serial port number e.g. for physical and virtual
                        // COM ports this takes the form: 1, 2, 3 ... etc.
						switch (communicationSetting.Port.Type)
						{
							case PortType.COM:
							case PortType.VCP:
								communicationSetting.PortIdentifier = communicationSetting.Port.Name.Remove(0, communicationSetting.Port.Type.ToString().Length);
								break;
							default:
								throw new ArgumentException("FormSelectTargetLogic.LocateTargetHardware()", "communicationSetting.Port.Type");
						}

						statusInformation.Text = Resources.TextSearchingForTargetOn + CommonConstants.Space + communicationSetting.Port.FullSpecification;
						statusInformation.Update();

						// Instantiate the appropriate type of communication interface.
						communicationInterface = new CommunicationApplication();
						try
						{
							if (communicationInterface.ScanPort(communicationSetting, out targetConfiguration) == true)
							{
								targetConfigurationList.Add(targetConfiguration);
                                listBoxTargetsFound.Items.Add(targetConfiguration.SubSystemName + " - (COM" + communicationSetting.PortIdentifier + ")" );
								listBoxTargetsFound.Update();
								statusInformation.Text = Resources.TextTargetFoundOn + CommonConstants.Space + communicationSetting.Port.FullSpecification;
                                statusInformation.Update();
								communicationSettingList.Add(communicationSetting);
								targetFound = true;
							}
						}
						catch (Exception)
						{
							statusInformation.Text = Resources.TextNoTargetFoundOn + CommonConstants.Space + communicationSetting.Port.FullSpecification;
							statusInformation.Update();
							continue;
						}
					}
				}
			}

            statusInformation.Text = string.Empty;
            statusInformation.Update();

			if (Parameter.CommunicationType == Parameter.CommunicationTypeEnum.Both || Parameter.CommunicationType == Parameter.CommunicationTypeEnum.TCPIP)
			{

				// -----------------------------------------------------------------
				// Scan each URI listed in the Data Dictionary.
				// -----------------------------------------------------------------

				CommunicationSetting_t communicationSetting = new CommunicationSetting_t();

				// Set the protocol to IP communication.
				communicationSetting.Protocol = Protocol.TCPIP;

				TargetConfiguration_t targetConfiguration;

                // Initialize the ProgressBar control.
                m_ProgressBarScan.Enabled = true;
                m_ProgressBarScan.Visible = true;
                m_LegendScanProgress.Visible = true;
                m_ProgressBarScan.Maximum = Parameter.URIList.Count;
                m_ProgressBarScan.Value = 0;
                m_BtnOK.Enabled = false;
                m_CancelSelected = false;

				// Scan each port in the Registry.
				foreach (string uRI in Parameter.URIList)
				{
                    // Ensure that the form remains responsive during the asynchronous operation.
                    Application.DoEvents();

                    // Check whether the Cancel button has been selected.
                    if (m_CancelSelected == true)
				{
                        // Yes - Terminate the scan.
                        break;
                    }

                    // Update the progress bar.
                    m_ProgressBarScan.Value++;

					communicationSetting.PortIdentifier = uRI;

                    statusInformation.Text = Resources.TextSearchingForTargetOn + CommonConstants.Space + Resources.TextURI + CommonConstants.Colon +
                                             communicationSetting.PortIdentifier;
                    statusInformation.Update();

					// Instantiate the appropriate type of communication interface.
					communicationInterface = new CommunicationApplication();
					try
					{
						if (communicationInterface.ScanPort(communicationSetting, out targetConfiguration) == true)
						{
							targetConfigurationList.Add(targetConfiguration);
							listBoxTargetsFound.Items.Add(targetConfiguration.SubSystemName + CommonConstants.BindingMessage +
                                                          Resources.TextURI + CommonConstants.Colon + communicationSetting.PortIdentifier);
							listBoxTargetsFound.Update();
                            statusInformation.Text = Resources.TextTargetFoundOn + CommonConstants.Space + Resources.TextURI + CommonConstants.Colon +
                                                     communicationSetting.PortIdentifier;
                            statusInformation.Update();
							communicationSettingList.Add(communicationSetting);
							targetFound = true;
						}
					}
					catch (Exception)
					{
						continue;
					}
				}

                m_BtnOK.Enabled = true;
                m_ProgressBarScan.Enabled = false;
                m_ProgressBarScan.Visible = false;
                m_LegendScanProgress.Visible = false;
			}

            if (targetFound == true)
            {
                statusInformation.Text = targetConfigurationList.Count.ToString() + CommonConstants.Space + Resources.TextTargetsFound;
                statusInformation.Update();

                // Highlight the first logic controller that was found.
                listBoxTargetsFound.SetSelected(0, true);
            }
            else
            {
                statusInformation.Text = Resources.TextNoTargetsFound;
                statusInformation.Update();
            }

            return targetFound;
        }
        #endregion --- Methods ---

        #region --- Properties ---
        /// <summary>
        /// Gets the target configuration information associated with the selected target.
        /// </summary>
        public TargetConfiguration_t TargetConfiguration
        {
            get { return m_TargetConfiguration; }
        }

        /// <summary>
        /// Gets the communication setting associated with the selected target.
        /// </summary>
        public CommunicationSetting_t CommunicationSetting
        {
            get { return m_CommunicationSetting; }
        }

        /// <summary>
        /// Gets the flag that indicates whether a target logic controller has been selected; true indicates that a target has been selected, otherwise false.
        /// </summary>
        public bool TargetSelected
        {
            get { return m_TargetSelected; }
        }
        #endregion --- Properties ---
    }
}