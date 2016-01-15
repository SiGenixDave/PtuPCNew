    @echo off
:: 
::       1         2         3         4         5         6         7         8
::  56789|123456789|123456789|123456789|123456789|123456789|123456789|123456789|
::  // ------------------------------------------------------------------------
::  //  
::  //  This document and its contents are the property of Bombardier Inc. or
::  //  its subsidiaries and contains confidential, proprietary information.
::  //  The reproduction, distribution, utilization or the communication of
::  //  this document, or any part thereof, without express authorization is
::  //  strictly prohibited. Offenders will be held liable for the payment of
::  //  damages.
::  // 
::  //  (C) 2010-2015 Bombardier Inc. or its subsidiaries. All rights reserved.
::  //
::  //  Project:    Portable Test Unit
::  // 
::  //  File name:  FtpTransfer.cmd
::  //
::  //  Summary:
::  //      This program is used to transfer all PTU configuration and help files 
::  //      that match the specified search criteria from the specified VCU host 
::  //      FTP servers to the appropriate local folders on the PTU laptop.
::  //
::  //      The program starts by pinging the specified IP address and, 
::  //      if a connection is established, uses the NcFTPLS and NcFTPGET
::  //      executables to download the PTU configuration and diagnostic help
::  //      files from directory '/tffs0' of the VCU FTP server. If no connection
::  //      is established on the main VCU FTP server the program then attempts
::  //      to download the files from the backup FTP server.
::  //          
::  //  Revision History
::  //  ----------------
::  //  Date        Version Author     
::  //  07/31/13    1.0     Sam Dlinn
::  //  Comments
::  //  1.  Original release from PIPPC to demonstrate principle.
::  //
::  //  08/27/15    1.1     K.McDonald
::  //  Comments
::  //  1.  Major update to interface with the PTU. Now uses NcFTPGET and
::  //      NcFTPLS instead of the Windows command FTP client as
::  //      there were problems establishing the Data channel when using
::  //      the Windows FTP client.
::  //
::  //	09/30/15    2.0     K.McDonald
::  //	2.  Improved error handling and debug information.
::  //
::  // ------------------------------------------------------------------------

::  // -----------------------------------------------------------
::  //  Prepare the Command Processor
::  // -----------------------------------------------------------
    set m_Debug=0

    setlocal EnableExtensions
    setlocal EnableDelayedExpansion

::  // -----------------------------------------------------------
::  //  Command Line Parameters
::  // -----------------------------------------------------------

        set searchFilter=%1
        set iPAddress1=%2
        set iPAddress2=%3

::  // -----------------------------------------------------------
::  //  Error Codes
::  // -----------------------------------------------------------

::  // --- [NcFTP Error Codes] ---
::  //  0   Success.
::  //  1   Could not connect to remote host.
::  //  2   Could not connect to remote host - timed out.
::  //  3   Transfer failed.
::  //  4   Transfer failed - timed out.
::  //  5   Directory change failed.
::  //  6   Directory change failed - timed out.
::  //  7   Malformed URL.
::  //  8   Usage error.
::  //  9   Error in login configuration file.
::  //  10  Library initialization failed.
::  //  11  Session initialization failed.

::  // --- [FindStr Error Codes] ---
::  //  0 (False) a match is found in at least one line of at least one file.
::  //  1 (True) if a match is not found in any line of any file, (or if the
::  //    file is not found at all).
::  //  2 Wrong syntax

::  //  --- [XCopy Error Codes] ---
::  //  0   Files were copied without error.
::  //  1   No files were found to copy
::  //  2   The user pressed CTRL +C to terminate xcopy.
::  //  3   Initialization error occurred. There is not enough
::  //      memory or disk space, or you entered an invalid drive
::  //      name or invalid syntax on the command line.
::  //  4   Disk write error occurred.


::  //  --- [FtpTransfer Error Codes] ---
    set /A Success=0
    set /A NcFTPCouldNotConnectToRemoteHost=1
    set /A NcFTPCouldNotConnectToRemoteHostTimedOut=2
    set /A NcFTPTransferFailed=3
    set /A NcFTPTransferFailedTimedOut=4
    set /A NcFTPDirectoryChangeFailed=5
    set /A NcFTPDirectoryChangeFailedTimedOut=6
    set /A NcFTPMalformedURL=7
    set /A NcFTPUsageError=8
    set /A NcFTPErrorInLoginConfigurationFile=9
    set /A NcFTPLibraryInitializationFailed=10
    set /A NcFTPSessionInitializationFailed=11

    set /A FindStrNoMatchFound=21
    set /A FindStrIncorrectSyntax=22

    set /A SystemException=30
    set /A PingError=31
    set /A PingCannotOpenPingReply=32
    set /A PingInvalidPacketInformation=33
    set /A PingNoReply=34
    set /A FileNotFoundXML=35
    set /A FileNotFoundHLP=36
    set /A FileNotFoundBoth=37
    set /A FileNotFoundSearchResults=38
    set /A NcFTPGetXMLFailed=39
    set /A NcFTPGetHLPFailed=40

    set /A XCopyNoFilesFound=51
    set /A XCopyCtrlC=52
    set /A XCopyInitFailed=54
    set /A XCopyDiskWriteError=55

    set /A Undefined=60

::  // --- [Error Code Offsets] ---
    set /A XCopyErrorOffset=50
    set /A FindStrErrorOffset=20

::  // --- [Member Variables] ---

::  // --- Note: %~dp0 is only available within a batch file and expands to the
::  // --- drive letter and path in which that batch file is located (which
::  // --- cannot change).
    set m_Parent=%~dp0
    set "m_pathXML=%m_Parent%Configuration Files\"
    set "m_PathHLP=%m_Parent%Diagnostic Help Files\"
    set m_Me=FtpTransfer.cmd
    set /A m_Return=%Undefined%

::  //  FtpTransfer.cmd Banner
    echo ________________________________________________________________________
    echo.
    echo Filename: FtpTransfer.cmd. Rev. 2.0
    echo.
    echo Summary:
    echo     Batch file to download the specified PTU configuration file and
    echo     diagnostic help file from the VCU using File Transfer Protocol (FTP).
    echo.
    echo ________________________________________________________________________
    echo.
    echo %m_Me% - In Progress ... &echo.

    if "%m_Debug%" == "1" echo --- [Main searchFilter: %searchFilter%]
    if "%m_Debug%" == "1" echo --- [Main iPAddress1: %iPAddress1%]
    if "%m_Debug%" == "1" echo --- [Main iPSddress2: %iPAddress2%]

    set /A return=%Success%

::  // Attempt to download the files from the primary target. If the download
::  // is successful, terminate the program; otherwise, attempt to download the
::  // files from the secondary target.
    call :ConnectAndDownload %iPAddress1% %searchFilter%
    if ErrorLevel 1 (
::      // Try to download the configuration from the secondary VCU.
        echo.
        call ::ConnectAndDownload %iPAddress2% %searchFilter%
        if ErrorLevel 1 (
            set /A return=!ErrorLevel!
        ) else (
            set /A return=%Success%
        )
    ) else (
        set /A return=%Success%
    )

    :EndMain
    echo.
    if !return! == %Success% (
        echo %m_Me% - Complete.
    ) else (
        echo %m_Me% Terminated with Error Code: !return! &call :ErrorHandling !return!
    )

    endlocal & set /A m_Return=%return%
    if "%m_Debug%" == "1" echo --- [Main.Return: %m_Return%] &echo.
    exit %m_Return%

::  // ------------------------------------------------------------------------------
::  //  ErrorLevel = ConnectAndDownload(iPaddress, searchFilter)
::  //  
::  //  Summary: Connect to the specified target vcu and, if successfull, attempt to download
::  //           any files that meet the specified search criterion from the 'ttsf0' directory
::  //           of the VCU to the appropriate local PTU directory.  
::  //
::  //  Returns:
::  //              SUCCESS     If the operation is successful.
::  //              PingNoReply If no reply was received from the specified target VCU.
::  //              
::  //              CheckForConnection() Error Codes
::  //              Download() Error Codes                   
::  //               
::  //  Parameters:
::  //      %1 iPAddress       The address of the target VCU.
::  //      %2 searchFilter    The search filter that is to be applied.
::  //              
::  // ------------------------------------------------------------------------------
    :ConnectAndDownload
::  [
        if "%m_Debug%" == "1" echo --- [ConnectAndDownload(%1 %2)]

        setlocal

        set iPAddress=%1
        set searchFilter=%2

        set /A return=%Success%

::      // Ping the target address only once to simplify processing and pipe the results to a file.
        echo Pinging !iPAddress! with 32 bytes of data.
        @ping -n 1 %iPAddress% > PingReply.txt
        if "%m_Debug%" == "1" echo --- [ConnectAndDownload @ping -n 1 %iPAddress% Return: !ErrorLevel!]
        if ErrorLevel 1 (
::          // No packet was returned from the Ping request.
            set /A return=%PingNoReply%
            echo Target  !iPAddress!:  Not Found.
        ) else (
::          // Check the output file and confirm that only 1 packet was received from the target.
            call :CheckForConnection PingReply.txt
            if ErrorLevel 1 (
                echo Target  !iPAddress!:  Not Found.
                set /A return=!ErrorLevel!
            ) else (
                echo Target  !iPAddress!:  Found.

::              // Attempt to download the configuration and help files from the target.  
                call :Download !iPAddress! %searchFilter%
                if ErrorLevel 1 (
                    set /A return=!ErrorLevel!
                ) else (
::                  // Successful download, terminate the loop.
                    set /A return=%Success%
                )    
            )
        )

        del PingReply.txt
        if "%m_Debug%" == "1" echo --- [ConnectAndDownload del PingReply.txt Return: !ErrorLevel!]

        :ConnectAndDownload
        endlocal & set /A m_Return=%return%
        if "%m_Debug%" == "1" echo --- [ConnectAndDownload.Return: %m_Return%] &call :ErrorHandling %m_Return%
        exit /B %m_Return%
::  ]

::  // ------------------------------------------------------------------------------
::  //  ErrorLevel = CheckForConnection(pingReplyFilename)
::  //  
::  //  Summary:    Read the file containing the results of the ping request and 
::  //              determine the number of packets received. If the number of packets
::  //              received is 1 then the connection to the target is deemed succesful.
::  //
::  //  Returns:
::  //              SUCCESS                     If the number of packets received is 1.
::  //              NoReply                     If the number of packets received is 0. 
::  //              CannotOpenPingReply         If the file containing the reply to the ping
::  //                                          cannot be opened.
::  //              InvalidPacketInformation    If the number of packets received is anything
::  //              
::  //               
::  //  Parameters
::  //              pingReplyFilename - The filename of the file that contains the reply to
::  //              the ping request.
::  // ------------------------------------------------------------------------------
    :CheckForConnection
::  [
        if "%m_Debug%" == "1" echo --- [CheckForConnection(%1)]

        setlocal

        set pingReplyFilename=%1

        set /A return=%Success%

::      // --------------------------------------------------------------------------
::      //  The contents of %pingReplyFilename% takes the form:
::      //
::      //  Pinging 10.0.1.5 with 32 bytes of data:
::      //  Reply from 10.0.1.5: bytes=32 time=6ms TTL=128
::      //  Ping statistics for 192.168.0.11:
::      //  Packets: Sent = 1, Received = 1, Lost = 0 (0% loss),
::      //  Approximate round trip times in milli-seconds:
::      //  Minimum = 6ms, Maximum = 6ms, Average = 6ms
::      // --------------------------------------------------------------------------

::      // Process the Reply Text.
::      // Find the line of text that has the packet information.
        findstr Packets %pingReplyFilename% > _packetsReceived.txt
        if "%m_Debug%" == "1" echo --- [CheckForConnection findstr Packets %pingReplyFilename% Return: !ErrorLevel!]
        if ErrorLevel 1 (
            set /A return=%CannotOpenPingReply%
            del _packetsReceived.txt
            if "%m_Debug%" == "1" echo --- [CheckForConnection del _packetsReceived.txt Return: !ErrorLevel!]
            goto EndCheckForConnection
        )

::      // Find the number of received packets. Note: The number of
::      // received packets is specified in the 7th token of the single
::      // line of text saved in '_packetsReceived.txt'.
        for /F "tokens=7" %%a in (_packetsReceived.txt) do set packetsReceived=%%a
        if ErrorLevel 1 (
            set /A return=%CannotOpenPingReply%
            del _packetsReceived.txt
            if "%m_Debug%" == "1" echo --- [CheckForConnection del _packetsReceived.txt Return: !ErrorLevel!]
            goto EndCheckForConnection
        )

::      // Isolate the number of received packets.
        if "%m_Debug%" == "1" echo --- [CheckForConnection packetsReceived: !packetsReceived!]
::      // Check whether a reply was received from the host.
        set /A pingResult=!packetsReceived:~0,-1!

        if "!pingResult!"=="1" (
::          // Report successful communication.
            set /A return=%Success%
        ) else (
::          // Check whether no reply was received from the host or that the packet information was not available.
            if "!pingResult!"=="0" (
::              // Report that no packet was received from the host.
                set /A return=%NoReply%
            ) else (
::              // Report that the packet information was invalid.
                set /A return=%InvalidPacketInformation%
            )
        )

::      // Delete any temporary text files. 
        del _packetsReceived.txt
        if "%m_Debug%" == "1" echo --- [CheckForConnection del PacketsReceived.txt Return: !ErrorLevel!]

        :EndCheckForConnection
        endlocal & set /A m_Return=%return%
        if "%m_Debug%" == "1" echo --- [CheckForConnection.Return: %m_Return%]
        exit /B %m_Return%
::  ]

::  // ------------------------------------------------------------------------
::  //  ErrorLevel = Download(iPaddress, searchFilter)
::  //
::  //  Summary:    Download all files matching the specified search criterion
::  //              from the target VCU FTP server to the appropriate local PTU directory.
::  //
::  //  Returns:
::  //      SUCCESS                 If all files meeting the specified search criterion
::  //                              are successfully downloaded to the PTU.
::  //      NcFTPGetXMLFailed       If the XML file download failed.  
::  //      NcFTPGetHLPFailed       If the HLP file download failed.
::  //
::  //      NcFTP Error Codes
::  //      FTPSearch() Error Codes
::  //            
::  //  Parameters:
::  //      %1 iPAddress       The address of the target VCU.
::  //      %2 searchFilter    The search filter that is to be applied.
::  // ------------------------------------------------------------------------
    :Download
::  [
        if "%m_Debug%" == "1" echo --- [Download(%1, %2)]

        setlocal

        set iPAddress=%1
        set searchFilter=%2

        set /A return=%Success%
        set /A ncFTPGetXMLState=%Undefined%
        set /A ncFTPGetHLPState=%Undefined%

        echo. &echo File Transfer from !iPAddress! in Progress.
        echo Please do not disconnect the Ethernet cable until the transfer is complete.
        echo.

::      // Output a list of those files that match the specified search criterion to the file SearchResults.txt.
        call :FTPSearch %iPAddress% %searchFilter% SearchResults.txt
        if ErrorLevel 1 (
            set /A return=!ErrorLevel!
            del SearchResults.txt
            if "%m_Debug%" == "1" echo --- [Download del SearchResults.txt Return: !ErrorLevel!]
            goto EndDownload
        )

        for /F "tokens=1" %%a in (SearchResults.txt) do (
            set filename=%%a
            set extension=!filename:~11!
            if "%m_Debug%" == "1" echo --- [Download filename: !filename!]
            if "%m_Debug%" == "1" echo --- [Download extension: !extension!]

            if /I "!extension!" == "xml" (
::              // Transfer the file using ASCII mode.
                call :FTPGet %iPAddress% A !filename!
                if ErrorLevel 1 (
                    set /A return=%NcFTPGetXMLFailed%
                    del SearchResults.txt
                    if "%m_Debug%" == "1" echo --- [Download del SearchResults.txt Return: !ErrorLevel!]
                    goto EndDownload
                )
                call :UpdateConfigurationFiles !filename!
                if ErrorLevel 1 (
                    set /A return=!ErrorLevel!
                    del SearchResults.txt
                    if "%m_Debug%" == "1" echo --- [Download del SearchResults.txt Return: !ErrorLevel!]
                    goto EndDownload
                )
                set /A ncFTPGetXMLState=%Success%
            ) else (
                if /I "!extension!" == "hlp" (
::                  // Transfer the file using BINARY mode.
                    call :FTPGet %iPAddress% B !filename!
                    if ErrorLevel 1 (
                        set /A return=%NcFTPGetHLPFailed%
                        del SearchResults.txt
                        if "%m_Debug%" == "1" echo --- [Download del SearchResults.txt Return: !ErrorLevel!]
                        goto EndDownload
                    )
                    set /A ncFTPGetHLPState=%Success%
                ) else (
                    echo A file with an invalid extension was found, this will be ignored.
                )
            )
        )

::      // Delete the temporary directory listing.
        del SearchResults.txt
        if "%m_Debug%" == "1" echo --- [Download del SearchResults.txt Return: !ErrorLevel!]

::      // Check that both the XML and HLP file were downloaded and update the return variable accordingly.  
        if !ncFTPGetXMLState! EQU %Success% (
            if !ncFTPGetHLPState! EQU %Success% (
::              // Both the XML and HLP files were downloaded successfully.
                set /A return=%Success%
            ) else (
                set /A return=%FileNotFoundHLP%
            )
        ) else (
            if !ncFTPGetHLPState! EQU %Success% (
                set /A return=%FileNotFoundXML%
            ) else (
                set /A return=%FileNotFoundBoth%
            )
        )

        :EndDownload
        if %return% NEQ %Success% echo File Transfer Terminated with Error Code: %return%. &call :ErrorHandling %return%
        endlocal & set /A m_Return=%return%
        if "%m_Debug%" == "1" echo --- [Download.Return: %m_Return%] &call :ErrorHandling %m_Return%
        exit /B %m_Return%
::  ]

::  // ------------------------------------------------------------------------
::  //  ErrorLevel = FTPSearch(iPAddress, searchFilter, searchResults)
::  //
::  //  Summary:
::  //      Writes a list of the files that are stored on the 'tffs0' directory of
::  //      the VCU FTP server that match the specified search criterion 
::  //      to the specified text file.
::  //    
::  //  Returns:
::  //      SUCCESS                     If a match is found in at least one line of
::  //                                  at least one file.
::  //      FindStrIncorrectSyntax      If the FindStr syntax is incorrect.
::  //      FindStrNoMatchFound         If no files matching the specified filter were found.
::  //
::  //      NcFTP Error Codes.
::  //        
::  //  Parameters:
::  //      %1 iPAddress        The TCP/IP address of the host VCU.
::  //      %2 searchFilter     The search filter that is to be applied to the
::  //                          directory listing.
::  //      %3 searchResults    The filename, inc. extension of the file where the
::  //                          search results are to be written.
::  // ------------------------------------------------------------------------
    :FTPSearch
::  [
        if "%m_Debug%" == "1" echo --- [FTPSearch(%1, %2, %3)]

        setlocal

        set iPAddress=%1
        set searchFilter=%2
        set searchResults=%3

        set /A return=%Success%

::      // Log on anonymously and get the directory listing of '/tffs0/'.
        ncftpls ftp://%iPAddress%/tffs0/ > DirectoryList.txt
        if "%m_Debug%" == "1" echo --- [FTPSearch ncftpls ftp://%iPAddress%/tffs0/ Return: !ErrorLevel!]
        if ErrorLevel 1 (
            set /A return=!ErrorLevel!
            del DirectoryList.txt
            if "%m_Debug%" == "1" echo --- [FTPSearch del DirectoryList.txt Return: !ErrorLevel!]
            goto EndFTPSearch
        )

::      // Filter out the files that match the search criterion and output them to the
::      // specified file.
        findstr /I %searchFilter% DirectoryList.txt > %searchResults%
        if "%m_Debug%" == "1" echo --- [FTPSearch findstr %searchFilter% DirectoryList.txt Return: !ErrorLevel!]
        if ErrorLevel 1 (
            if ErrorLevel 2 (
                set /A return=%FindStrIncorrectSyntax%
            ) else (
                set /A return=%FindStrNoMatchFound%
            )
        ) else (
            if "%m_Debug%" == "1" echo --- The following files matching the specified search string were found:
            if "%m_Debug%" == "1" type %searchResults% &echo.
        )

        del DirectoryList.txt
        if "%m_Debug%" == "1" echo --- [FTPSearch del DirectoryList.txt Return: !ErrorLevel!]

        :EndFTPSearch
        endlocal & set /A m_Return=%return%
        if "%m_Debug%" == "1" echo --- [FTPSearch.Return: %m_Return%] &call :ErrorHandling %m_Return%
        exit /B %m_Return%
::  ]

::  // ------------------------------------------------------------------------
::  //  ErrorLevel = CheckIfExist(searchResults, filename)
::  //
::  //  Summary:
::  //      Reads through the the specified directory list to check whether
::  //      the specified file exists.
::  //    
::  //  Returns:
::  //      SUCCESS                     If the file was found.
::  //      FileNotFoundSearchResults   If the file containing the search results
::  //                                  cannot be found.
::  //      FindStrNoMatchFound         If the specified file was not found.
::  //      FindStrIncorrectSyntax      If the FindStr syntax is incorrect.
::  //        
::  //  Parameters:
::  //      %1 searchResults    The filename, inc. extension of the file where the
::  //                          search results are written.
::  //
::  //      %2 filename         The filename of the required file.
::  // ------------------------------------------------------------------------
    :CheckIfExists
::  [
        if "%m_Debug%" == "1" echo --- [CheckIfExists(%1, %2)]

        setlocal

        set searchResults=%1
        set filename=%2

        set /A return=%Success%

::      // Check that the file containing the search results exists.
        if not exist %searchResults% (
            set /A return=%FileNotFoundSearchResults%
            goto EndCheckIfExists
        )

        findstr /I %filename% %searchResults%
        if "%m_Debug%" == "1" echo --- [CheckIfExists findstr %filename% %searchResults% Return: !ErrorLevel!]
        if ErrorLevel 1 (
            if ErrorLevel 2 (
                set /A return=%FindStrIncorrectSyntax%
            ) else (
                set /A return=%FindStrNoMatchFound%
            )
        )

    :EndCheckIfExists
    endlocal & set /A m_Return=%return%
    if "%m_Debug%" == "1" echo --- [CheckIfExists.Return: %m_Return%] &call :ErrorHandling %m_Return%
    exit /B %m_Return%
::  ]



::  // ------------------------------------------------------------------------
::  //  ErrorLevel = FTPGet(iPAddress, transferMode, sourcefilename)
::  //
::  //  Summary:
::  //      Transfers the specified source file from the 'tffs0' directory of the
::  //      target VCU to the appropriate local PTU directory.
::  //
::  //  Member Variables
::  //      m_PathXML           The local PTU path where the configuration files are stored.
::  //      m_PathHLP           The local PTU path where the help files are stored.
::  //    
::  //  Returns:
::  //      SUCCESS             If the transfer was successful.
::  //
::  //      NcFTP Error Codes
::  //        
::  //  Parameters:
::  //      %1 iPAddress        The TCP/IP address of the host VCU.
::  //      %2 transferMode     The transfer mode - [A|B] (Ascii|Binary). The mode
::  //                          character MUST be specified.
::  //      %3 sourcefilename   The source filename.
::  // ------------------------------------------------------------------------
    :FTPGet
::  [
        if "%m_Debug%" == "1" echo --- [FTPGet(%1, %2, %3)]

        setlocal

        set iPAddress=%1
        set transferMode=%2
        set sourceFilename=%3

        set /A return=%Success%

::      // --- Check transfer mode and apply appropriate switch.                                 
        if /I "%transferMode%" == "A" (
            echo Transferring: %sourceFilename% ...
            ncftpget -C -V -a %iPAddress% /tffs0/%sourceFilename% "%m_PathXML%%sourceFilename%"
            if "%m_Debug%" == "1" echo --- [FTPGet ncftpget -C -V -a %iPAddress% /tffs0/%sourceFilename% "%m_PathXML%%sourceFilename%" Return: !ErrorLevel!]
            if ErrorLevel 1 (
                set /A return=!ErrorLevel!
            )
        ) else (
            echo Transferring: %sourceFilename% ...
            ncftpget -C -V %iPAddress% /tffs0/%sourceFilename% "%m_PathHLP%%sourceFilename%"
            if "%m_Debug%" == "1" echo --- [FTPGet ncftpget -C -V %iPAddress% /tffs0/%sourceFilename% "%m_PathHLP%%sourceFilename%" Return: !ErrorLevel!]
            if ErrorLevel 1 (
                set /A return=!ErrorLevel!
            )
        )

        endlocal & set /A m_Return=%return%
        if "%m_Debug%" == "1" echo --- [FTPGet.Return: %m_Return%] &call :ErrorHandling %m_Return%
        exit /B %m_Return%
::  ]

::  // ------------------------------------------------------------------------
::  //  ErrorLevel = UpdateConfigurationFiles(configurationFilename)
::  //
::  //  Summary:    Update the PTU configuration files, 'Configuration.xml' and
::  //              '<project-identifier>.Configuration.xml' to the specified XML file.
::  //              It is assumed that all files are located in the PTU local '\Configuration Files'
::  //              sub-directory.
::  //
::  //  Member Variables
::  //      m_PathXML       The PTU path where the configuration files are stored.
::  //      m_PathHLP       The PTU path where the help files are stored.
::  //    
::  //  Returns:
::  //      SUCCESS         If the update was successful.
::  //
::  //      XCopy Error Codes + XCopyErrorOffset
::  //        
::  //  Parameters:
::  //      configurationFilename   The filename of the XML file that contains the 
::  //                              updated configuration.
::  // ------------------------------------------------------------------------
    :UpdateConfigurationFiles
::  [
        if "%m_Debug%" == "1" echo --- [UpdateConfigurationFiles(%1)]

        setlocal
        set configurationFilename=%1
        set /A return=%Success%

        echo Updating:     CTPA.Configuration.xml
        xcopy "%m_PathXML%%configurationFilename%" "%m_PathXML%CTPA.Configuration.xml" /v /y /q > NUL
        if "%m_Debug%" == "1" echo --- [UpdateConfigurationFiles xcopy "%m_PathXML%%configurationFilename%" "%m_PathXML%CTPA.Configuration.xml" /v /y /q Return: !ErrorLevel!]
        if ErrorLevel 1 (
            set /A return=%XCopyErrorOffset%+!ErrorLevel!
            goto EndUpdateConfigurationFiles
        )

        echo Updating:     Configuration.xml
        xcopy "%m_PathXML%%configurationFilename%" "%m_PathXML%Configuration.xml" /v /y /q > NUL
        if "%m_Debug%" == "1" echo --- [UpdateConfigurationFiles xcopy "%m_PathXML%%configurationFilename%" "%m_PathXML%Configuration.xml" /v /y /q Return: !ErrorLevel!]
        if ErrorLevel 1 (
            set /A return=%XCopyErrorOffset%+!ErrorLevel!
            goto EndUpdateConfigurationFiles
        )

        :EndUpdateConfigurationFiles
        endlocal & set /A m_Return=%return%
        if "%m_Debug%" == "1" echo --- [UpdateConfigurationFiles.Return: %m_Return%] &call :ErrorHandling %m_Return%
        exit /B %m_Return%
::  ]

::  // ------------------------------------------------------------------------
::  //  ErrorLevel = ErrorHandling(errorCode)
::  //
::  //  Summary: Echo the message associated with the specified error code.
::  //    
::  //  Returns:
::  //      SUCCESS
::  //        
::  //  Parameters:
::  //      errorCode   The error code that is to be handled.
::  // ------------------------------------------------------------------------
    :ErrorHandling
::  [
        setlocal
        set errorCode=%1

::      // Don't report anything if the error code is Success.
        if %errorCode% EQU %Success% goto EndErrorHandling

::      NcFTP Error Codes.
        if %errorCode% EQU %NcFTPCouldNotConnectToRemoteHost% echo Error Code: %errorCode% NcFTP Error - Could Not Connect to the Remote Host. &goto EndErrorHandling
        if %errorCode% EQU %NcFTPCouldNotConnectToRemoteHostTimedOut% echo Error Code: %errorCode% NcFTP Error - Could Not Connect to the Remote Host - Timed Out. &goto EndErrorHandling
        if %errorCode% EQU %NcFTPTransferFailed% echo Error Code: %errorCode% NcFTP Error - Transfer Failed. &goto EndErrorHandling
        if %errorCode% EQU %NcFTPTransferFailedTimedOut% echo Error Code: %errorCode% NcFTP Error - Transfer Failed - Timed Out. &goto EndErrorHandling
        if %errorCode% EQU %NcFTPDirectoryChangeFailed% echo Error Code: %errorCode% NcFTP Error - Directory Change Failed. &goto EndErrorHandling
        if %errorCode% EQU %NcFTPDirectoryChangeFailedTimedOut% echo Error Code: %errorCode% NcFTP Error - Directory Change Failed - Timed Out. &goto EndErrorHandling
        if %errorCode% EQU %NcFTPMalformedURL% echo Error Code: %errorCode% NcFTP Error - Malformed URL. &goto EndErrorHandling
        if %errorCode% EQU %NcFTPUsageError% echo Error Code: %errorCode% NcFTP Error - FTP Usage Error. &goto EndErrorHandling
        if %errorCode% EQU %NcFTPErrorInLoginConfigurationFile% echo Error Code: %errorCode% NcFTP Error - Error in Login Configuration File. &goto EndErrorHandling
        if %errorCode% EQU %NcFTPLibraryInitializationFailed% echo Error Code: %errorCode% NcFTP Error - Library Initialization Failed. &goto EndErrorHandling
        if %errorCode% EQU %NcFTPSessionInitializationFailed% echo Error Code: %errorCode% NcFTP Error - Session Initialization Failed. &goto EndErrorHandling

::      // FindStr Error Codes.
        if %errorCode% EQU %FindStrNoMatchFound% echo Error Code: %errorCode% FindStr Error - No Match Found.  &goto EndErrorHandling
        if %errorCode% EQU %FindStrIncorrectSyntax% echo Error Code: %errorCode% FindStr Error - Incorrect Syntax.  &goto EndErrorHandling

::      // FtpTransfer Error Codes.
        if %errorCode% EQU %SystemException% echo Error Code: %errorCode% System Exception.  &goto EndErrorHandling
        if %errorCode% EQU %PingError% echo Error Code: %errorCode% Ping Error.  &goto EndErrorHandling
        if %errorCode% EQU %PingCannotOpenPingReply% echo Error Code: %errorCode% Ping Error - Cannot Open Ping Reply.  &goto EndErrorHandling
        if %errorCode% EQU %PingInvalidPacketInformation% echo Error Code: %errorCode% Ping Error - Invalid Packet Information.  &goto EndErrorHandling
        if %errorCode% EQU %PingNoReply% echo Error Code: %errorCode% Ping Error - No Reply.  &goto EndErrorHandling
        if %errorCode% EQU %FileNotFoundXML% echo Error Code: %errorCode% File Not Found - XML File.  &goto EndErrorHandling
        if %errorCode% EQU %FileNotFoundHLP% echo Error Code: %errorCode% File Not Found - HLP File.  &goto EndErrorHandling
        if %errorCode% EQU %FileNotFoundBoth% echo Error Code: %errorCode% File Not Found - XML and HLP File.  &goto EndErrorHandling
        if %errorCode% EQU %FileNotFoundSearchResults% echo Error Code: %errorCode% File Not Found - Search Results.  &goto EndErrorHandling
        if %errorCode% EQU %NcFTPGetXMLFailed% echo Error Code: %errorCode% NcFTPGET Failed - XML File.  &goto EndErrorHandling
        if %errorCode% EQU %NcFTPGetHLPFailed% echo Error Code: %errorCode% NcFTPGET Failed - HLP File.  &goto EndErrorHandling

::      //  XCopy Error Codes.
        if %errorCode% EQU %XCopyNoFilesFound% echo Error Code: %errorCode% XCopy Error - No Files Found.  &goto EndErrorHandling
        if %errorCode% EQU %XCopyCtrlC% echo Error Code: %errorCode% XCopy Error - Ctrl C Used to Terminate XCopy.  &goto EndErrorHandling
        if %errorCode% EQU %XCopyInitFailed% echo Error Code: %errorCode% XCopy Error - Initialization Failed.  &goto EndErrorHandling
        if %errorCode% EQU %XCopyDiskWriteError% echo Error Code: %errorCode% XCopy Error - Disk Write Error.  &goto EndErrorHandling

::      // Error is Undefined.
        if %errorCode% EQU %Undefined% echo Error Code: %errorCode% Error Undefined.  &goto EndErrorHandling

::      // default.
        echo Error Code: %errorCode% Unrecognised Error Code.

        :EndErrorHandling
        endlocal
        exit /B %Success%
::  ]



