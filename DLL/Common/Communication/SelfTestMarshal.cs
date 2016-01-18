#region --- Revision History ---

/*
 *
 *  This document and its contents are the property of Bombardier Inc. or its subsidiaries and contains confidential, proprietary information.
 *  The reproduction, distribution, utilization or the communication of this document, or any part thereof, without express authorization is strictly prohibited.
 *  Offenders will be held liable for the payment of damages.
 *
 *  (C) 2016    Bombardier Inc. or its subsidiaries. All rights reserved.
 *
 *  Solution:   PTU
 *
 *  Project:    Common
 *
 *  File name:  EventStreamMarshal.cs
 *
 *  Revision History
 *  ----------------
 *
 *  Date        Version Author       Comments
 *  03/01/2015  1.0     D.Smail      First Release.
 *
 */

#endregion --- Revision History ---

using System;
using System.Threading;
using VcuComm;

namespace Common.Communication
{
    /// <summary>
    /// This class contains methods used to generate commands and data requests to the embedded target
    /// and process the responses. All methods are related to handling self test communication.
    /// </summary>
    public class SelfTestMarshal
    {
        #region --- Constants ---

        /// <summary>
        /// The maximum amount of self test data that can be sent from the embedded target to this application
        /// on any given message.
        /// </summary>
        private const UInt16 MAX_BUFFER_SIZE = 4096;

        private const UInt16 STC_MSG_MODE_SPECIAL = 4;

        private const Byte STC_CMD_ABORT_SEQ = 4;

        private const Byte STC_CMD_OPRTR_ACK = 7;

        private const Byte STC_CMD_UPDT_LIST = 3;

        private const Byte STC_CMD_SEL_LIST = 1;

        private const Byte STC_CMD_UPDT_LOOP_CNT = 8;

        private const Byte STC_CMD_EXECUTE_LIST = 2;

        private const Byte STC_CMD_UPDT_MODE = 0;

        private const byte STC_MSG_MODE_INTERACTIVE = 5;

        #endregion --- Constants ---

        #region --- Member Variables ---

        /// <summary>
        /// The type of communication device used to interface with the embedded target (RS-232, TCP, etc.)
        /// </summary>
        private ICommDevice m_CommDevice;

        /// <summary>
        /// Object used to handle the standard embedded target communication protocol
        /// </summary>
        private PtuTargetCommunication m_PtuTargetCommunication = new PtuTargetCommunication();

        /// <summary>
        /// Buffer used to store data responses from the embedded target. Need to add the header
        /// size.
        /// </summary>
        private Byte[] m_RxMessage = new Byte[MAX_BUFFER_SIZE];

        #endregion --- Member Variables ---

        #region --- Constructors ---

        /// <summary>
        /// Constructor that must be used to create an object of this class.
        /// </summary>
        /// <param name="device">the type of communication device (RS-232, TCP, etc.)</param>
        public SelfTestMarshal(ICommDevice device)
        {
            m_CommDevice = device;
        }

        /// <summary>
        /// The default constructor is made private to force the use of the multi-argument constructor
        /// when creating an instance of this class.
        /// </summary>
        private SelfTestMarshal()
        { }

        #endregion --- Constructors ---

        #region --- Methods ---

        #region --- Public Methods ---

        /// <summary>
        /// Get the self test special message.
        /// </summary>
        /// <param name="Result">The result of the call. A value of: (1) 1 represents success; (2) indicates that the error message defined by the 
        /// <paramref name="Reason"/> parameter applies and (3) represents an unknown error.</param>
        /// <param name="Reason">A value of 1 represents success; otherwise, the value is mapped to the <c>ERRID</c> field of the
        /// <c>SELFTESTERRMESS</c> table of the data dictionary in order to determine the error message returned from the VCU.</param>
        /// <returns>Success, if the communication request was successful; otherwise, an error code.</returns>
        public CommunicationError GetSelfTestSpecialMessage(out Int16 Result, out Int16 Reason)
        {
            Result = -1;
            Reason = -1;

            // Initiate transaction with embedded target
            CommunicationError commError = m_PtuTargetCommunication.SendDataRequestToEmbedded(m_CommDevice, ProtocolPTU.PacketType.GET_SELF_TEST_PACKET, m_RxMessage);

            if (commError != CommunicationError.Success)
            {
                return commError;
            }

            // Extract all of the information from the received data
            Byte valid = m_RxMessage[8];
            Byte messageMode = m_RxMessage[9];

            if (m_CommDevice.IsTargetBigEndian())
            {
                valid = Utils.ReverseByteOrder(valid);
                messageMode = Utils.ReverseByteOrder(messageMode);
            }

            if (valid != 1)
            {
                return CommunicationError.UnknownError;
            }

            if (messageMode != STC_MSG_MODE_SPECIAL)
            {
                return CommunicationError.BadResponse;
            }

            Result = BitConverter.ToInt16(m_RxMessage, 12);
            Reason = BitConverter.ToInt16(m_RxMessage, 15);

            if (m_CommDevice.IsTargetBigEndian())
            {
                Result = Utils.ReverseByteOrder(Result);
                Reason = Utils.ReverseByteOrder(Reason);
            }

            return CommunicationError.Success;
        }

        /// <summary>
        /// Start the self test task.
        /// </summary>
        /// <remarks>This request will start the self test process on the VCU. 
        /// process.</remarks>
        /// <param name="Result">The result of the call. A value of: (1) 1 represents success; (2) indicates that the error message defined by the 
        /// <paramref name="Reason"/> parameter applies and (3) represents an unknown error.</param>
        /// <param name="Reason">A value of 1 represents success; otherwise, the value is mapped to the <c>ERRID</c> field of the
        /// <c>SELFTESTERRMESS</c> table of the data dictionary in order to determine the error message returned from the VCU.</param>
        /// <returns>Success, if the communication request was successful; otherwise, an error code.</returns>
        public CommunicationError StartSelfTestTask(out Int16 Result, out Int16 Reason)
        {
            Result = -1;
            Reason = -1;

            // Initiate transaction with embedded target
            CommunicationError commError = m_PtuTargetCommunication.SendCommandToEmbedded(m_CommDevice, ProtocolPTU.PacketType.START_SELF_TEST_TASK);

            if (commError != CommunicationError.Success)
            {
                return commError;
            }

            // original code has a hard-coded while--loop delay
            Thread.Sleep(100);

            commError = GetSelfTestSpecialMessage(out Result, out Reason);

            return commError;
        }

        /// <summary>
        /// Exit the self test task. 
        /// </summary>
        /// <remarks>This request will exit the self-test process on the VCU and turn control over to the propulsion software.</remarks>
        /// <param name="Result">The result of the call. A value of: (1) 1 represents success; (2) indicates that the error message defined by the 
        /// <paramref name="Reason"/> parameter applies and (3) represents an unknown error.</param>
        /// <param name="Reason">A value of 1 represents success; otherwise, the value is mapped to the <c>ERRID</c> field of the
        /// <c>SELFTESTERRMESS</c> table of the data dictionary in order to determine the error message returned from the VCU.</param>
        /// <returns>Success, if the communication request was successful; otherwise, an error code.</returns>
        public CommunicationError ExitSelfTestTask(out Int16 Result, out Int16 Reason)
        {
            Result = -1;
            Reason = -1;

            // Initiate transaction with embedded target
            CommunicationError commError = m_PtuTargetCommunication.SendCommandToEmbedded(m_CommDevice, ProtocolPTU.PacketType.EXIT_SELF_TEST_TASK);

            if (commError != CommunicationError.Success)
            {
                return commError;
            }

            commError = GetSelfTestSpecialMessage(out Result, out Reason);

            return commError;
        }

        /// <summary>
        /// Abort the self test sequence.
        /// </summary>
        /// <remarks>This request will stop the execution of the self-test process on the VCU and return control to the propulsion software.</remarks>
        /// <returns>Success, if the communication request was successful; otherwise, an error code.</returns>
        public CommunicationError AbortSTSequence()
        {
            ProtocolPTU.SelfTestCommand request = new ProtocolPTU.SelfTestCommand(STC_CMD_ABORT_SEQ, 0, 0);

            // Initiate transaction with embedded target
            CommunicationError commError = m_PtuTargetCommunication.SendCommandToEmbedded(m_CommDevice, request);

            return commError;
        }

        /// <summary>
        /// Send an operator acknowledge message.
        /// </summary>
        /// <remarks>This request allows the operator to move to the next step of an interactive test.</remarks>
        /// <returns>Success, if the communication request was successful; otherwise, an error code.</returns>
        public CommunicationError SendOperatorAcknowledge()
        {
            ProtocolPTU.SelfTestCommand request = new ProtocolPTU.SelfTestCommand(STC_CMD_OPRTR_ACK, 0, 0);

            // Initiate transaction with embedded target
            CommunicationError commError = m_PtuTargetCommunication.SendCommandToEmbedded(m_CommDevice, request);

            return commError;
        }

        /// <summary>
        /// Update the list of individually selected self tests that are to be executed. 
        /// </summary>
        /// <remarks>This method will define the list of self-tests that are to be executed once the tester selects the execute command. The self tests
        /// are defined using the self test identifiers defined in the data dictionary.</remarks>
        /// <param name="NumberOfTests">The number of tests in the list.</param>
        /// <param name="TestList">A list of the self test identifiers.</param>
        /// <returns>Success, if the communication request was successful; otherwise, an error code.</returns>
        public CommunicationError UpdateSTTestList(Int16 NumberOfTests, Int16[] TestList)
        {
            ProtocolPTU.SelfTestUpdateListReq request = new ProtocolPTU.SelfTestUpdateListReq(STC_CMD_UPDT_LIST, NumberOfTests, TestList);

            // Initiate transaction with embedded target
            CommunicationError commError = m_PtuTargetCommunication.SendCommandToEmbedded(m_CommDevice, request);

            return commError;
        }

        /// <summary>
        /// Run the predefined self tests associated with the specified test list identifier, these tests are defined in the data dictionary. 
        /// </summary>
        /// <param name="TestID">The test list identifier of the predefined self tests that are to be executed.</param>
        /// <returns>Success, if the communication request was successful; otherwise, an error code.</returns>
        public CommunicationError RunPredefinedSTTests(Int16 TestID)
        {
            ProtocolPTU.SelfTestCommand request = new ProtocolPTU.SelfTestCommand(STC_CMD_SEL_LIST, 0, (UInt16)TestID);

            // Initiate transaction with embedded target
            CommunicationError commError = m_PtuTargetCommunication.SendCommandToEmbedded(m_CommDevice, request);

            return commError;
        }

        /// <summary>
        /// Update the number of times that the selected tests are to be run.
        /// </summary>
        /// <param name="LoopCount">The number of cycles/loops of the defined tests that are to be performed.</param>
        /// <returns>Success, if the communication request was successful; otherwise, an error code.</returns>
        public CommunicationError UpdateSTLoopCount(Int16 LoopCount)
        {
            ProtocolPTU.SelfTestCommand request = new ProtocolPTU.SelfTestCommand(STC_CMD_UPDT_LOOP_CNT, 0, (UInt16)LoopCount);

            // Initiate transaction with embedded target
            CommunicationError commError = m_PtuTargetCommunication.SendCommandToEmbedded(m_CommDevice, request);

            return commError;
        }

        /// <summary>
        /// Execute the self tests that are defined in the current list.
        /// </summary>
        /// <param name="TruckInformation">The truck to which the self tests apply. This does not apply on the CTA project, separate self-tests are set
        /// up for each truck.</param>
        /// <returns>Success, if the communication request was successful; otherwise, an error code.</returns>
        public CommunicationError ExecuteSTTestList(Int16 TruckInformation)
        {
            ProtocolPTU.SelfTestCommand request = new ProtocolPTU.SelfTestCommand(STC_CMD_EXECUTE_LIST, (Byte)TruckInformation, 0);

            // Initiate transaction with embedded target
            CommunicationError commError = m_PtuTargetCommunication.SendCommandToEmbedded(m_CommDevice, request);

            return commError;
        }


        /// <summary>
        /// Update the self test mode.
        /// </summary>
        /// <remarks>This call is used to check whether communication with the VCU has been lost.</remarks>
        /// <param name="NewMode">The required self test mode.</param>
        /// <returns>Success, if the communication request was successful; otherwise, an error code.</returns>
        public CommunicationError UpdateSTMode(Int16 NewMode)
        {
            ProtocolPTU.SelfTestCommand request = new ProtocolPTU.SelfTestCommand(STC_CMD_UPDT_MODE, 0, (UInt16)NewMode);

            // Initiate transaction with embedded target
            CommunicationError commError = m_PtuTargetCommunication.SendCommandToEmbedded(m_CommDevice, request);

            return commError;
        }


        /// <summary>
        /// Get the self test results.
        /// </summary>
        /// <param name="ValidResult">A flag to indicate whether a valid result is available. A value of 1 indicates that a valid result is
        /// available; otherwise, 0.</param>
        /// <param name="MessageMode">The type of message returned from the VCU.</param>
        /// <param name="TestID">The test result identifier; the interpretation of this value is dependent upon the message mode. For detailed
        /// messages, this value represents the self test identifier.</param>
        /// <param name="TestCase">The test case number associated with the message.</param>
        /// <param name="TestResult">Used with the passive and logic self tests to define whether the test passed or failed. A value of 1 indicates
        /// that the test passed; otherwise, the test failed.</param>
        /// <param name="SetInfo">An enumerator to define the truck information associated with the message.</param>
        /// <param name="NumOfVars">The number of variables associated with the message.</param>
        /// <param name="InteractiveResults">A pointer to a byte array that is used to store the value of each self test variable associated with the current
        /// interactive test. This byte array must be mapped to an array of <see cref="InteractiveResults_t"/> structures to obtain the results. Data
        /// is passed this way as the size of the InteractiveResults_t structure in C# is different to the value used in VcuCommunication32. Each
        /// element of the array of the InteractiveResults_t structures in VcuCommunication32 consists of a double (8 bytes) followed by an integer
        /// (4 byte) making a total of 12 bytes.</param>
        /// <returns>Success, if the communication request was successful; otherwise, an error code.</returns>
        public CommunicationError GetSelfTestResult(out Int16 ValidResult, out MessageMode MessageMode, out Int16 TestID,
                                                     out Int16 TestCase, out Int16 TestResult, out TruckInformation SetInfo,
                                                     out Int16 NumOfVars, InteractiveResults_t[] InteractiveResults)
        {
            const Byte MAXSTVARIABLES = 16;

            ValidResult = -1;
            MessageMode = MessageMode.Undefined;
            TestID = -1;
            TestCase = -1;
            TestResult = -1;
            SetInfo = TruckInformation.Undefined;
            NumOfVars = -1;

            // Initiate transaction with embedded target
            CommunicationError commError = m_PtuTargetCommunication.SendDataRequestToEmbedded(m_CommDevice, ProtocolPTU.PacketType.GET_SELF_TEST_PACKET, m_RxMessage);

            if (commError != CommunicationError.Success)
            {
                return commError;
            }

            // Extract all of the information from the received data
            Byte validResult = m_RxMessage[8];
            Byte messageMode = m_RxMessage[9];
            UInt16 setInfo = BitConverter.ToUInt16(m_RxMessage, 10);
            UInt16 testID = BitConverter.ToUInt16(m_RxMessage, 12);
            Byte testCase = m_RxMessage[15];
            Byte numOfVars = m_RxMessage[16];
            Byte testResult = m_RxMessage[17];


            if (m_CommDevice.IsTargetBigEndian())
            {
                validResult = Utils.ReverseByteOrder(validResult);
                messageMode = Utils.ReverseByteOrder(messageMode);
                setInfo = Utils.ReverseByteOrder(setInfo);
                testID = Utils.ReverseByteOrder(testID);
                testCase = Utils.ReverseByteOrder(testCase);
                numOfVars = Utils.ReverseByteOrder(numOfVars);
                testResult = Utils.ReverseByteOrder(testResult);
            }

            ValidResult = validResult;
            MessageMode = (MessageMode)messageMode;
            TestID = (Int16)testID;
            TestCase = testCase;
            TestResult = testResult;
            SetInfo = (TruckInformation)setInfo;
            NumOfVars = numOfVars;


            if (numOfVars > MAXSTVARIABLES)
            {
                numOfVars = MAXSTVARIABLES;
            }

            if ((messageMode == STC_MSG_MODE_INTERACTIVE) && (validResult == 1))
            {

                UInt16 valueOffset = 28;
                UInt16 tagOffset = 32;
                UInt16 typeOffSet = 33;
                for (Byte index = 0; index < numOfVars; index++)
                {
                    Byte varType = m_RxMessage[typeOffSet];
                    Byte tag = m_RxMessage[tagOffset];
                    if (m_CommDevice.IsTargetBigEndian())
                    {
                        varType = Utils.ReverseByteOrder(varType);
                        tag = Utils.ReverseByteOrder(tag);
                    }

                    InteractiveResults[index].Tag = tag;

                    // All data written to "value" is 32 bits. Therefore, all values
                    // can be extracted as a 32 bit number
                    switch ((ProtocolPTU.VariableType)varType)
                    {
                        case ProtocolPTU.VariableType.UINT_8_TYPE:
                        case ProtocolPTU.VariableType.UINT_16_TYPE:
                        case ProtocolPTU.VariableType.UINT_32_TYPE:
                            UInt32 u32 = BitConverter.ToUInt32(m_RxMessage, valueOffset);
                            if (m_CommDevice.IsTargetBigEndian())
                            {
                                u32 = Utils.ReverseByteOrder(u32);
                            }
                            InteractiveResults[index].Value = (double)u32;;
                            break;

                        case ProtocolPTU.VariableType.INT_8_TYPE:
                        case ProtocolPTU.VariableType.INT_16_TYPE:
                        case ProtocolPTU.VariableType.INT_32_TYPE:
                            Int32 i32 = BitConverter.ToInt32(m_RxMessage, valueOffset);
                            if (m_CommDevice.IsTargetBigEndian())
                            {
                                i32 = Utils.ReverseByteOrder(i32);
                            }
                            InteractiveResults[index].Value = (double)i32;
                            break;

                        default:
                            InteractiveResults[index].Value = 0;
                            break;

                    }

                    typeOffSet += 6;
                    valueOffset += 6;
                    tagOffset += 6;
                }
            }
            
            return CommunicationError.Success;
        }


        #endregion --- Public Methods ---


        #endregion --- Methods ---
    }
}