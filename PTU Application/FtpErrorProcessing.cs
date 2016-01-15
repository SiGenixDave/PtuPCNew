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
 *  Project:    PTU Application
 * 
 *  File name:  FtpErrorProcessing.cs
 * 
 *  Revision History
 *  ----------------
 *
 *  Date        Version Author          Comments
 *  09/30/15    1.0     K.McD           1.  First entry into TortoiseSVN.
 */
#endregion --- Revision History ---

using Bombardier.PTU.Properties;

namespace Bombardier.PTU
{
    /// <summary>
    /// The Error Code values associated with the FTP transfer Windows Command File.
    /// </summary>
    internal enum FTPErrorCode
    {
        /// <summary>
        /// The function call was successful i.e. there were no errors. Value: 0.
        /// </summary>
        Success = 0,

        /// <summary>
        /// NcFTP Error - Could Not Connect to the Remote Host. Value: 1.
        /// </summary>
        NcFTPCouldNotConnectToRemoteHost = 1,

        /// <summary>
        /// NcFTP Error - Could Not Connect to the Remote Host - Timed Out. Value: 2.
        /// </summary>
        NcFTPCouldNotConnectToRemoteHostTimedOut = 2,

        /// <summary>
        /// NcFTP Error - Transfer Failed. Value: 3.
        /// </summary>
        NcFTPTransferFailed = 3,

        /// <summary>
        /// NcFTP Error - Transfer Failed - Timed Out. Value: 4.
        /// </summary>
        NcFTPTransferFailedTimedOut = 4,

        /// <summary>
        /// NcFTP Error - Directory Change Failed. Value: 5.
        /// </summary>
        NcFTPDirectoryChangeFailed = 5,

        /// <summary>
        /// NcFTP Error - Directory Change Failed - Timed Out. Value: 6.
        /// </summary>
        NcFTPDirectoryChangeFailedTimedOut = 6,

        /// <summary>
        /// NcFTP Error - Malformed URL. Value: 7.
        /// </summary>
        NcFTPMalformedURL = 7,

        /// <summary>
        /// NcFTP Error - FTP Usage Error. Value: 8.
        /// </summary>
        NcFTPUsageError = 8,

        /// <summary>
        /// NcFTP Error - Error in Login Configuration File. Value: 9.
        /// </summary>
        NcFTPErrorInLoginConfigurationFile = 9,

        /// <summary>
        /// NcFTP Error - Library Initialization Failed. Value: 10.
        /// </summary>
        NcFTPLibraryInitializationFailed = 10,

        /// <summary>
        /// NcFTP Error - Session Initialization Failed. Value: 11.
        /// </summary>
        NcFTPSessionInitializationFailed = 11,

        /// <summary>
        /// FindStr Error - No Match Found. Value: 21.
        /// </summary>
        FindStrNoMatchFound = 21,

        /// <summary>
        /// FindStr Error - Incorrect Syntax. Value: 22.
        /// </summary>
        FindStrIncorrectSyntax = 22,

        /// <summary>
        /// System Exception. Value: 30.
        /// </summary>
        SystemException = 30,

        /// <summary>
        /// Ping Error. Value: 31.
        /// </summary>
        PingError = 31,

        /// <summary>
        /// Ping Error - Cannot Open Ping Reply. Value: 32.
        /// </summary>
        PingCannotOpenPingReply = 32,

        /// <summary>
        /// Ping Error - Invalid Packet Information. Value: 33.
        /// </summary>
        PingInvalidPacketInformation = 33,

        /// <summary>
        /// Ping Error - No Reply. Value: 34.
        /// </summary>
        PingNoReply = 34,

        /// <summary>
        /// File Not Found - XML File. Value: 35.
        /// </summary>
        FileNotFoundXML = 35,

        /// <summary>
        /// File Not Found - HLP File. Value: 36.
        /// </summary>
        FileNotFoundHLP = 36,

        /// <summary>
        /// File Not Found - XML and HLP File. Value: 37.
        /// </summary>
        FileNotFoundBoth = 37,

        /// <summary>
        /// File Not Found - Search Results. Value: 38.
        /// </summary>
        FileNotFoundSearchResults = 38,

        /// <summary>
        /// NcFTPGET Failed - XML File. Value: 39.
        /// </summary>
        NcFTPGetXMLFailed = 39,

        /// <summary>
        /// FTPGET Failed - HLP File. Value: 40.
        /// </summary>
        NcFTPGetHLPFailed = 40,

        /// <summary>
        /// XCopy Error - No Files Found. Value: 51.
        /// </summary>
        XCopyNoFilesFound = 51,

        /// <summary>
        /// XCopy Error - Ctrl C Used to Terminate XCopy. Value: 52.
        /// </summary>
        XCopyCtrlC = 52,

        /// <summary>
        /// XCopy Error - Initialization Failed. Value: 54.
        /// </summary>
        XCopyInitFailed = 54,

        /// <summary>
        /// XCopy Error - Disk Write Error. Value: 55.
        /// </summary>
        XCopyDiskWriteError = 55,

        /// <summary>
        /// Undefined. Value: 60.
        /// </summary>
        Undefined = 60
    }

    /// <summary>
    /// Class to support processing of the error codes returned from the FTP transfer Windows Command File.
    /// </summary>
    internal static class FtpErrorProcessing
    {
        #region --- Methods ---
        /// <summary>
        /// Gets the error message corresponding to the specified error code associated with the FTP transfer Windows Command File.
        /// </summary>
        /// <param name="errorCode">The error code that was returned from the FTP transfer Windows Command File.</param>
        /// <returns>The error message corresponding to the specified error code.</returns>
        internal static string GetErrorMessage(FTPErrorCode errorCode)
        {
            // The error message corresponding to the error code.
            string errorMessage;

            switch (errorCode)
            {
                case FTPErrorCode.Success:
                    errorMessage = Resources.FTPErrorCodeSuccess;
                    break;
                case FTPErrorCode.NcFTPCouldNotConnectToRemoteHost:
                    errorMessage = Resources.FTPErrorCodeNcFTPCouldNotConnectToRemoteHost;
                    break;
                case FTPErrorCode.NcFTPCouldNotConnectToRemoteHostTimedOut:
                    errorMessage = Resources.FTPErrorCodeNcFTPCouldNotConnectToRemoteHostTimedOut;
                    break;
               case FTPErrorCode.NcFTPTransferFailed:
                    errorMessage = Resources.FTPErrorCodeNcFTPTransferFailed;
                    break;
               case FTPErrorCode.NcFTPTransferFailedTimedOut:
                    errorMessage = Resources.FTPErrorCodeNcFTPTransferFailedTimedOut;
                    break;
               case FTPErrorCode.NcFTPDirectoryChangeFailed:
                    errorMessage = Resources.FTPErrorCodeNcFTPDirectoryChangeFailed;
                    break;
                case FTPErrorCode.NcFTPDirectoryChangeFailedTimedOut:
                    errorMessage = Resources.FTPErrorCodeNcFTPDirectoryChangeFailedTimedOut;
                    break;
                case FTPErrorCode.NcFTPMalformedURL:
                    errorMessage = Resources.FTPErrorCodeNcFTPMalformedURL;
                    break;
                case FTPErrorCode.NcFTPUsageError:
                    errorMessage = Resources.FTPErrorCodeNcFTPUsageError;
                    break;
                case FTPErrorCode.NcFTPLibraryInitializationFailed:
                    errorMessage = Resources.FTPErrorCodeNcFTPLibraryInitializationFailed;
                    break;
                case FTPErrorCode.NcFTPSessionInitializationFailed:
                    errorMessage = Resources.FTPErrorCodeNcFTPSessionInitializationFailed;
                    break;
                case FTPErrorCode.FindStrNoMatchFound:
                    errorMessage = Resources.FTPErrorCodeFindStrNoMatchFound;
                    break;
                case FTPErrorCode.FindStrIncorrectSyntax:
                    errorMessage = Resources.FTPErrorCodeFindStrIncorrectSyntax;
                    break;
                case FTPErrorCode.SystemException:
                    errorMessage = Resources.FTPErrorCodeSystemException;
                    break;
                case FTPErrorCode.PingError:
                    errorMessage = Resources.FTPErrorCodePingError;
                    break;
                case FTPErrorCode.PingCannotOpenPingReply:
                    errorMessage = Resources.FTPErrorCodePingCannotOpenPingReply;
                    break;
                case FTPErrorCode.PingInvalidPacketInformation:
                    errorMessage = Resources.FTPErrorCodePingInvalidPacketInformation;
                    break;
                case FTPErrorCode.PingNoReply:
                    errorMessage = Resources.FTPErrorCodePingNoReply;
                    break;
                case FTPErrorCode.FileNotFoundXML:
                    errorMessage = Resources.FTPErrorCodeFileNotFoundXML;
                    break;
                case FTPErrorCode.FileNotFoundHLP:
                    errorMessage = Resources.FTPErrorCodeFileNotFoundHLP;
                    break;
                case FTPErrorCode.FileNotFoundBoth:
                    errorMessage = Resources.FTPErrorCodeFileNotFoundBoth;
                    break;
                case FTPErrorCode.FileNotFoundSearchResults:
                    errorMessage = Resources.FTPErrorCodeFileNotFoundSearchResults;
                    break;
                case FTPErrorCode.NcFTPGetXMLFailed:
                    errorMessage = Resources.FTPErrorCodeNcFTPGetXMLFailed;
                    break;
                case FTPErrorCode.NcFTPGetHLPFailed:
                    errorMessage = Resources.FTPErrorCodeNcFTPGetHLPFailed;
                    break;
                case FTPErrorCode.XCopyNoFilesFound:
                    errorMessage = Resources.FTPErrorCodeXCopyNoFilesFound;
                    break;
                case FTPErrorCode.XCopyCtrlC:
                    errorMessage = Resources.FTPErrorCodeXCopyCtrlC;
                    break;
                case FTPErrorCode.XCopyInitFailed:
                    errorMessage = Resources.FTPErrorCodeXCopyInitFailed;
                    break;
                case FTPErrorCode.XCopyDiskWriteError:
                    errorMessage = Resources.FTPErrorCodeXCopyDiskWriteError;
                    break;
                case FTPErrorCode.Undefined:
                    errorMessage = Resources.FTPErrorCodeUndefined;
                    break;
                default:
                    errorMessage = Resources.FTPErrorCodeUnrecognized;
                    break;
            }

            return errorMessage;
        }
        #endregion --- Methods ---
    }
}
