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
 *  File name:  FileHandling.cs
 * 
 *  Revision History
 *  ----------------
 * 
 *  Date        Version Author          Comments
 *  03/31/10    1.0     K.McD           1.  First entry into TortoiseSVN.
 * 
 *  11/16/10    1.1     K.McD           1.  Added to AutoScale_t structure.
 *                                      2.  Modified the WatchFile_t structure.
 * 
 *  11/17/10    1.2     K.McD           1.  Removed the AutoScale_t structure.
 * 
 *  12/01/10    1.3     K.McD           1.  Added the EventLogFile_t structire.
 * 
 *  01/06/11    1.4     K.McD           1.  Added the Filename variable to the WatchFile_t and EventLogFile_t structures.
 * 
 *  01/31/11    1.5     K.McD           1.  Added the AppendEventRecordList() method to the EventLogFile_t structure.
 *                                      2.  Additional signature included in the EventLogFile_t structure.
 * 
 *  02/03/11    1.6     K.McD           1.  Removed unnecessary signatures associated with the LoadDataSet(), SerializeDataToFile() and LoadDataFromFile() methods.
 *                                      2.  Renamed the SerializeDataToFile() method to Serialize() and the LoadDataFromFile() method to Load().
 *                                      3.  Modified a number of XML tags and comments.
 *                                      4.  Included an additional signature for the AppendEventRecordList() method of the EventLogFile_t structure which will 
 *                                          append the records in the specified event record list but will ignore duplicate entries.
 * 
 *  03/18/11    1.7     K.McD           1.  Modified the AppendEventRecordList() method used to filter out duplicate records to include a match for the event index 
 *                                          in the Find() method call when trying to locate the correct record in the list of event records. This was added to cater 
 *                                          for the case where multiple events with the same description and date/time stamp are generated by the VCU, but with 
 *                                          different event variable information e.g. in the case of the 'Inverter Fault' event. 
 * 
 *  03/28/11    1.8     K.McD           1.  Renamed a number of local variables.
 *  
 *  10/01/11    1.9     K.McD           1.  Added the FullFilename property to the WatchFile_t and EventLogFile_t structures.
 *                                      2.  Modified the Serialize<T>() method to use the 'FileInfo.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite)' 
 *                                          method and to include a try/catch block to catch UnauthorizedAccess exceptions.
 *                                          
 *  10/10/11    1.10    K.McD           1.  Bug fix. Modified the Serialize<T>() method to delete the file if it already exists. If the file is not deleted there can be 
 *                                          problems when saving the data in XML mode. If the size of the data is smaller than the data in the existing file then the 
 *                                          new data appears to be overlayed into the existing file which mean that when the file is read the XML code may contains 
 *                                          spurious remnants of the existing XML code at the end of the file which may throw an exception when the code is read using 
 *                                          the Deserialize() method.
 *                                          
 *  04/16/15    1.11    K.McD           References
 *              
 *                                      1.  Upgrade the PTU software to extend the support for the R188 project as defined in purchase order
 *                                          4800010525-CU2/19.03.2015.
 *                                      
 *                                          1.  Changes to address the items outlined in the minutes of the meeting between BTPC, Kawasaki Rail Car and NYTC on
 *                                              12th April 2013 - MOC-0171:
 *                                              
 *                                              1.  MOC-0171-06. All references to 'Fault Logs', including menu options and directory names to be replaced by
 *                                                  'Data Streams' for all projects.
 *
 *                                          2.  NK-U-6505 Section 5.1.3. Log File Naming. The filename of all event logs, fault logs, real time data etc will be modified
 *                                              to meet the KRC specification. This modification will be specific to the KRC project; for all other projects, the
 *                                              current naming convention will still apply.
 *                                      
 *                                      Modifications
 *                                      1.  Renamed the SimulatedDataStream and DataStream elements of the LogType enumerator.
 *                                      
 *  05/13/15    1.12    K.McD           References
 *                                      1.  SNCR - R188 PTU [20 Mar 20015] Item 15. While trying to modify the plot layout of a data stream that had been created as part
 *                                          of the CTA PTU installation, the exception �ER-150506 � Attempt to modify default plot layout� was thrown. On further
 *                                          investigation, it was discovered that files that are created as part of the PTU installation are read only for the current
 *                                          user, even if they have administrative privileges. On changing the plot layout the exception was thrown on attempting to
 *                                          write the new layout to disk.
 *  
 *                                      Modifications
 *                                      1.  Introduced a try/catch block in the Serialize method to catch any exception that is thrown as a result of the call to 
 *                                          fileInfo.Delete() and report it to the user. Following the implementation of the bug fix: the uer is informed of the
 *                                          problem; and the plot layout is modified for the duration of the current session, but is not saved to disk.
 */
#endregion --- Revision History ---

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.IO;
using System.Data;
using System.Diagnostics;

using Common.Configuration;
using Common.Communication;
using Common.Properties;

namespace Common
{
    #region --- Enumerated Types ---
    /// <summary>
    /// The type of log associated with the data.
    /// </summary>
    public enum LogType
    {
        /// <summary>
        /// Recorded watch values.
        /// </summary>
        Watch,

        /// <summary>
        /// Event log.
        /// </summary>
        Event,

        /// <summary>
        /// Data Stream.
        /// </summary>
        DataStream,

        /// <summary>
        /// Simulated Data Stream.
        /// </summary>
        SimulatedDataStream,

        /// <summary>
        /// Undefined.
        /// </summary>
        Undefined
    }

    /// <summary>
    /// The source of the log e.g.: disk file; target hardware etc.
    /// </summary>
    public enum DataSource
    {
        /// <summary>
        /// Disk file.
        /// </summary>
        Disk,

        /// <summary>
        /// Target hardware.
        /// </summary>
        TargetHardware,

        /// <summary>
        /// The data originates from the header information associated with the last data file that was retrieved.
        /// </summary>
        HeaderLastRetrieved,

        /// <summary>
        /// The data source is currently un-defined.
        /// </summary>
        None
    }
    #endregion --- Enumerated Types ---

    #region --- Structures ---
    /// <summary>
    /// A structure to store the watch variable values that are to be serialized to and de-serialized from disk.
    /// </summary>
    [Serializable]
    public struct WatchFile_t
    {
        #region - [Member Variables] -
        /// <summary>
        /// The filename of the file containing the serialized watch data.
        /// </summary>
        /// <remarks>
        /// This is updated when the data is read from the file.
        /// </remarks>
        public string Filename;

        /// <summary>
        /// The fully qualified filename of the file containing the serialized watch data.
        /// </summary>
        /// <remarks>
        /// This is updated when the data is read from the file.
        /// </remarks>
        public string FullFilename;

        /// <summary>
        /// The file header information.
        /// </summary>
        public Header_t Header;

        /// <summary>
        /// The data stream containing the watch variable values.
        /// </summary>
        public DataStream_t DataStream;
        #endregion - [Member Variables] -

        #region - [Constructors] -
        /// <summary>
        /// Initialize a new instance of the structure. Copies the specified parameters to the structure and creates the auto-scale limits from the specified watch data 
        /// frames.
        /// </summary>
        /// <param name="header">The file header information.</param>
        /// <param name="dataStream">The data stream.</param>
        public WatchFile_t(Header_t header, DataStream_t dataStream)
        {
            Header = header;
            DataStream = dataStream;
            Filename = string.Empty;
            FullFilename = string.Empty;
        }
        #endregion - [Constructors] -
    }

    /// <summary>
    /// A structure to store the event records that are to be serialized to and de-serialized from disk.
    /// </summary>
    [Serializable]
    public struct EventLogFile_t
    {
        #region - [Member Variables] -
        /// <summary>
        /// The filename of the file containing serialized event records.
        /// </summary>
        /// <remarks>
        /// This is updated when the data is read from the file.
        /// </remarks>
        public string Filename;

        /// <summary>
        /// The fully qualified filename of the file containing the serialized event records.
        /// </summary>
        /// <remarks>
        /// This is updated when the data is read from the file.
        /// </remarks>
        public string FullFilename;

        /// <summary>
        /// The file header information.
        /// </summary>
        public Header_t Header;

        /// <summary>
        /// A list of the event records.
        /// </summary>
        public List<EventRecord> EventRecordList;
        #endregion - [Member Variables] -

        #region - [Constructors] -
        /// <summary>
        /// Initializes a new instance of the structure. 
        /// </summary>
        /// <param name="header">The file header information.</param>
        public EventLogFile_t(Header_t header)
        {
            Header = header;
            EventRecordList = new List<EventRecord>();
            Filename = string.Empty;
            FullFilename = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the structure. 
        /// </summary>
        /// <param name="header">The file header information.</param>
        /// <param name="eventRecordList">The list of event records that are to be written to disk file in XML format.</param>
        public EventLogFile_t(Header_t header, List<EventRecord> eventRecordList)
        {
            Header = header;

            // Copy the list of event records.
            EventRecord[] eventRecords = eventRecordList.ToArray();
            EventRecordList = new List<EventRecord>();
            EventRecordList.AddRange(eventRecords);
            Filename = string.Empty;
            FullFilename = string.Empty;
        }
        #endregion - [Constructors] -

        #region --- Methods ---
        /// <summary>
        /// Append the specified list of event records to the existing event record list. Use the signature that specifies the duplicationsFound flag if 
        /// duplicate entries are to be ignored.
        /// </summary>
        /// <param name="eventRecordList">The list of event records that are to be appended to the event record list.</param>
        public void AppendEventRecordList(List<EventRecord> eventRecordList)
        {
            Debug.Assert(eventRecordList != null, "FileHandling.AppendEventRecordList() - [eventRecordList != null]");

            EventRecord[] eventRecords = eventRecordList.ToArray();
            if (EventRecordList != null)
            {
                EventRecordList.AddRange(eventRecords);
            }
        }

        /// <summary>
        /// Append the specified list of event records to the existing event record list. Use the signature that specifies the duplicationsFound flag if 
        /// duplicate entries are to be ignored.
        /// </summary>
        /// <param name="eventRecordList">The list of event records that are to be appended to the event record list.</param>
        /// <param name="duplicationsFound">A flag to indicate whether the specified event record list contained duplicated events. True, if duplate entries were 
        /// found; otherwise, false.</param>
        public void AppendEventRecordList(List<EventRecord> eventRecordList, out bool duplicationsFound)
        {
            Debug.Assert(EventRecordList != null, "FileHandling.AppendEventRecordList() - [EventRecordList != null]");
            Debug.Assert(eventRecordList != null, "FileHandling.AppendEventRecordList() - [eventRecordList != null]");

            duplicationsFound = false;

            // Add the events contained within the selected file, however, exclude duplications.
            string description, carIdentifier;
            DateTime dateTime;
            int eventIndex, logIdentifier;

            EventRecord foundEventRecord;
            for (short recordIndex = 0; recordIndex < eventRecordList.Count; recordIndex++)
            {
                // Check whether the current event already exists.
                description = eventRecordList[recordIndex].Description;
                carIdentifier = eventRecordList[recordIndex].CarIdentifier;
                dateTime = eventRecordList[recordIndex].DateTime;
                eventIndex = eventRecordList[recordIndex].EventIndex;
                logIdentifier = eventRecordList[recordIndex].LogIdentifier;

                foundEventRecord = EventRecordList.Find(delegate(EventRecord eventRecord)
                {
                    if ((eventRecord.Description == description) &&
                        (eventRecord.CarIdentifier == carIdentifier) &&
                        (eventRecord.LogIdentifier == logIdentifier) &&
                        (eventRecord.DateTime == dateTime) &&
                        (eventRecord.EventIndex == eventIndex))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                });

                // If the current event doesn't already exist, add it.
                if (foundEventRecord == null)
                {
                    EventRecordList.Add(eventRecordList[recordIndex]);
                }
                else
                {
                    duplicationsFound = true;
                }
            }
        }
        #endregion --- Methods ---
    }
    #endregion --- Structures ---

    /// <summary>
    /// A collection of static methods used to support reading and writing of data from/to disk files.
    /// </summary>
    public static class FileHandling
    {
        #region --- Enumerated Data Types ---
        /// <summary>
        /// Defines the type of serialization format to use when serializing an object to disk.
        /// </summary>
        public enum FormatType
        {
            /// <summary>
            /// Binary format.
            /// </summary>
            Binary,

            /// <summary>
            /// SOAP format.
            /// </summary>
            SOAP,

            /// <summary>
            /// Xml Format.
            /// </summary>
            Xml
        }
        #endregion --- Enumerated Data Types ---

        #region --- Methods ---
        /// <summary>
        /// De-serializes the data contained within the specified file into the specified generic type.
        /// </summary>
        /// <param name="fullFilename">The fully qualified filename of the file containing the serialized data.</param>
        /// <param name="formatType">The format that was used to serialize the object: Binary; SOAP; Xml.</param>
        /// <typeparam name="T">The object type being de-serialized.</typeparam>
        /// <returns>The de-serialized object.</returns>
        public static T Load<T>(string fullFilename, FormatType formatType)
        {
            // Provides functionality for formatting serialized objects.
            IFormatter formatter;

            // Provides support for serializing objects using XML format.
            XmlSerializer xmlSerializer;

            // Exposes a stream around a file, supporting both synchronous and asynchronous read and write operations.
            FileStream fileStream;

            // The de-serialized object.
            T data = default(T);
            try
            {
                fileStream = new FileStream(fullFilename, FileMode.Open, FileAccess.Read, FileShare.None);
                if (fileStream != null)
                {
                    Cursor.Current = Cursors.WaitCursor;
                    try
                    {
                        // De-serialize the data using the appropriate serialization object.
                        switch (formatType)
                        {
                            case FormatType.Binary:
                                formatter = new BinaryFormatter();
                                data = (T)formatter.Deserialize(fileStream);
                                break;
                            case FormatType.SOAP:
                                formatter = new BinaryFormatter();
                                data = (T)formatter.Deserialize(fileStream);
                                break;
                            case FormatType.Xml:
                                xmlSerializer = new XmlSerializer(typeof(T));
                                data = (T)xmlSerializer.Deserialize(fileStream);
                                break;
                        }
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show(exception.Message, Resources.MBCaptionError, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return data;
                    }
                    finally
                    {
                        fileStream.Close();
                        Cursor.Current = Cursors.Default;
                    }
                    return data;
                }
            }
            catch (FileNotFoundException fileNotFoundException)
            {
                MessageBox.Show(fileNotFoundException.Message, Resources.MBCaptionWarning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return data;
            }
            return data;
        }

        /// <summary>
        /// Serializes the specified generic type to the specified filename using the specified format. 
        /// </summary>
        /// <param name="fullFilename">The fully qualified file name of the file to which the data is to be serialized.</param>
        /// <param name="data">The data to be serialized.</param>
        /// <typeparam name="T">The object type to be serialized.</typeparam>
        /// <param name="formatType">The format to be used to serialize the generic type: Binary; SOAP; Xml.</param>
        public static void Serialize<T>(string fullFilename, T data, FormatType formatType)
        {
            // Provides functionality for formatting serialized objects.
            IFormatter formatter;

            // Provides support for serializing objects using XML format.
            XmlSerializer xmlSerializer;

            // Exposes a stream around a file, supporting both synchronous and asynchronous read and write operations.
            FileStream fileStream;

            // Ensure that the specified filename is valid.
            FileInfo fileInfo = new FileInfo(fullFilename);
            DirectoryInfo directoryInfo = new DirectoryInfo(fileInfo.Directory.ToString());
            if (directoryInfo.Exists)
            {
                Cursor.Current = Cursors.WaitCursor;
                
                // If the file already exists, delete it. If we don't do this there can be problems when saving the data in XML mode. If the new data is smaller 
                // than the data in the existing file then the new data appears to be overlayed into the existing file which mean that when the file is read the 
                // XML code may contains spurious remnants of the existing XML code at the end of the file which may throw an exception.
                try
                {
                    if (fileInfo.Exists)
                    {
                        fileInfo.Delete();
                    }
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message + CommonConstants.NewPara + Resources.MBTLayoutChangesNotSaved, Resources.MBCaptionError,
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                fileStream = fileInfo.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

                try
                {
                    // Serialize the data using the appropriate serialization type.
                    switch (formatType)
                    {
                        case FormatType.Binary:
                            formatter = new BinaryFormatter();
                            formatter.Serialize(fileStream, data);
                            break;
                        case FormatType.SOAP:
                            formatter = new BinaryFormatter();
                            formatter.Serialize(fileStream, data);
                            break;
                        case FormatType.Xml:
                            xmlSerializer = new XmlSerializer(typeof(T));
                            xmlSerializer.Serialize(fileStream, data);
                            break;
                    }
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message, Resources.MBCaptionError, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    fileStream.Close();
                    Cursor.Current = Cursors.Default;
                }
            }
        }

        /// <summary>
        /// Loads the contents of an <c>XML</c> file into the specified <c>DataSet</c> derived class.
        /// </summary>
        /// <param name="fullFilename">The fully qualified filename of the XML file.</param>
        /// <param name="dataSet">The data dictionary.</param>
        /// <typeparam name="T">The <c>DataSet</c> derived class associated with the XML file.</typeparam>
        public static void LoadDataSet<T>(string fullFilename, ref T dataSet) where T : DataSet
        {
            // Exposes a stream around a file, supporting both synchronous and asynchronous read and write operations.
            FileStream fileStream;

            try
            {
                // Load the DataSet read from the XML file.
                fileStream = new FileStream(fullFilename, FileMode.Open, FileAccess.Read, FileShare.None);
                Cursor.Current = Cursors.WaitCursor;
                try
                {
                    dataSet.ReadXml(fileStream);
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message, Resources.MBCaptionError, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    dataSet = default(T);
                }
                finally
                {
                    fileStream.Close();
                    Cursor.Current = Cursors.Default;
                }
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show(Resources.MBTDefaultDataDictionaryNotFound, Resources.MBCaptionInformation, MessageBoxButtons.OK, MessageBoxIcon.Information);
                dataSet = default(T);
            }
        }
        #endregion --- Methods ---
    }
}
