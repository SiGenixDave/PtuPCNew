#region --- Revision History ---
/*
 * 
 *  This document and its contents are the property of Bombardier Inc. or its subsidiaries and contains confidential, proprietary information.
 *  The reproduction, distribution, utilization or the communication of this document, or any part thereof, without express authorization is strictly prohibited.  
 *  Offenders will be held liable for the payment of damages.
 * 
 *  (C) 2010    Bombardier Inc. or its subsidiaries. All rights reserved.
 * 
 *  Solution:   Portable Test Unit
 * 
 *  Project:    Common
 * 
 *  File name:  EventControl.Designer.cs
 * 
 *  Revision History
 *  ----------------
 * 
 *  Date        Version Author          Comments
 *  11/02/10    1.0     K.McD           1.  First entry into TortoiseSVN.
 *  
 *  04/21/15    1.1     K.McD           References
 *                                      1.  The height of the event variable user control must be increased to allow characters such as 'g', 'j', 'p', 'q', 'y' to be 
 *                                          displayed correctly when using the default font.
 *                                      Modifications
 *                                      1.  The Padding, Size and TextAlign properties of the Name, Value and Units Label controls were changes to: ((0),(0),(0)),
 *                                          ((200, 28), (100, 28), (100, 28)) and (MiddleLeft, MiddleRight and MiddleCenter) respectively.
 *                                      2.  The Size property of the control was modified to (400, 28).
 */

/*
 *  09/30/15    1.2     K.McD       References
 *                                  1.  Bug Fix - SNCR - R188 [20-Mar215] Item 32. The R188 project requires that the PTE can be used on a 1024 x 768 laptop.
 *                                      When release 6.14 is displayed at a resolution of 1024 x 768: (a) the ‘Help’ and ‘Exit’ buttons at the bottom of the control panel
 *                                      are partly cut off (b) the ‘View/Event Log’, ‘View/Test Results’ and ‘Data Stream Replay’ screens are only fully visible
 *                                      (without resorting to the horizontal scroll bar) if the R188 control panel is removed; (c) the ‘Open/Saved Event Log’ screen
 *                                      is not fully visible because of the additional ‘real-estate’ occupied by the ‘[Log]’ column of the DataGridView
 *                                      control; and (d) the watch worksets can only use 2 of the 3 available columns if all watch variables in the workset are to be
 *                                      visible without resorting to the horizontal scroll bar.
 *                                  
 *                                  Modifications
 *                                  1.  Modified the 'TextAlign' property of the 'Label' controls making up this user control from Middle to Top to ensure that the
 *                                      text does not become misaligned if there is insufficient room to display the variable name or value.
 *                                      
 *                                  2.  Removed the code that redefined the height of the control to 28.
 *                                  
 *                                  3.  Redefined the Padding property of all 'Label' controls to the default value of (0, 5, 0, 0).
 * 
 */
#endregion --- Revision History ---

namespace Common.UserControls
{
    partial class EventControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.m_ContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.m_MenuItemShowDefinition = new System.Windows.Forms.ToolStripMenuItem();
            this.m_ContextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // m_LabelNameField
            // 
            this.m_LabelNameField.BackColor = System.Drawing.Color.WhiteSmoke;
            this.m_LabelNameField.DoubleClick += new System.EventHandler(this.m_MenuItemShowDefinition_Click);
            // 
            // m_LabelValueField
            // 
            this.m_LabelValueField.BackColor = System.Drawing.Color.WhiteSmoke;
            this.m_LabelValueField.ForeColor = System.Drawing.Color.ForestGreen;
            // 
            // m_LabelUnitsField
            // 
            this.m_LabelUnitsField.BackColor = System.Drawing.Color.WhiteSmoke;
            this.m_LabelUnitsField.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // m_ContextMenu
            // 
            this.m_ContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_MenuItemShowDefinition});
            this.m_ContextMenu.Name = "m_ContextMenu";
            this.m_ContextMenu.Size = new System.Drawing.Size(159, 26);
            // 
            // m_MenuItemShowDefinition
            // 
            this.m_MenuItemShowDefinition.Image = global::Common.Properties.Resources.Help;
            this.m_MenuItemShowDefinition.Name = "m_MenuItemShowDefinition";
            this.m_MenuItemShowDefinition.Size = new System.Drawing.Size(158, 22);
            this.m_MenuItemShowDefinition.Text = "Show &Definition";
            this.m_MenuItemShowDefinition.Click += new System.EventHandler(this.m_MenuItemShowDefinition_Click);
            // 
            // EventControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.BackColorValueFieldNonZero = System.Drawing.Color.WhiteSmoke;
            this.BackColorValueFieldZero = System.Drawing.Color.WhiteSmoke;
            this.ContextMenuStrip = this.m_ContextMenu;
            this.ForeColorValueFieldNonZero = System.Drawing.Color.ForestGreen;
            this.ForeColorValueFieldZero = System.Drawing.Color.ForestGreen;
            this.Name = "EventControl";
            this.Controls.SetChildIndex(this.m_LabelNameField, 0);
            this.Controls.SetChildIndex(this.m_LabelValueField, 0);
            this.Controls.SetChildIndex(this.m_LabelUnitsField, 0);
            this.m_ContextMenu.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ContextMenuStrip m_ContextMenu;
        private System.Windows.Forms.ToolStripMenuItem m_MenuItemShowDefinition;
    }
}
