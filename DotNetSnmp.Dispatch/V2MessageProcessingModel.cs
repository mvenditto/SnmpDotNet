﻿using DotNetSnmp.Common.Definitions;
using DotNetSnmp.Protocol.V2;
using DotNetSnmp.Protocol.V3;
using DotNetSnmp.Transport.Targets;
using System.Formats.Asn1;

namespace DotNetSnmp.Client
{
    /// <summary>
    /// The message processing model for SNMPv2c.
    /// </summary>
    public class V2MessageProcessingModel : IMessageProcessingModel
    {
        public bool IsProtocolVersionSupported(ProtocolVersion version)
        {
            return version == ProtocolVersion.SnmpV1;
        }

        public bool TryPrepareDataElements(
            in ReadOnlyMemory<byte> incomingMessage,
            out string securityName,
            out SecurityLevel securityLevel,
            out SecurityModel securityModel,
            out Pdu pdu,
            out int sendPduHandle,
            out MessageProcessingResult result)
        {
            result = MessageProcessingResult.Success;
            sendPduHandle = -1;
            securityLevel = SecurityLevel.None;
            securityModel = SecurityModel.SnmpV2c;
            securityName = string.Empty;
            pdu = null;

            try
            {
                var reader = new AsnReader(incomingMessage, AsnEncodingRules.BER);
                var message = SnmpV2Message.ReadFrom(reader);
                pdu = message.Pdu;
                sendPduHandle = message.Pdu.RequestId;
                securityName = message.Community;
                securityModel = (SecurityModel)(int)message.ProtocolVersion;
                return true;
            }
            catch (Exception ex)
            {
                result = MessageProcessingResult.InternalError;
                return false;
            }
        }

        public bool TryPrepareOutgoingMessage(
            ISnmpTarget target,
            Pdu pdu,
            in ReadOnlyMemory<byte> secEngineId,
            out int sendPduHandle,
            out SnmpMessage outgoingMessage,
            out MessageProcessingResult result,
            bool expectResponse = true)
        {

            result = MessageProcessingResult.Success;
            sendPduHandle = -1;
            outgoingMessage = null;

            if (pdu == null)
            {
                throw new ArgumentNullException(nameof(pdu));
            }

            if (target.SecurityLevel != SecurityLevel.None
                || target.SecurityModel != SecurityModel.SnmpV2c)
            {
                result = MessageProcessingResult.UnsupportedSecurityModel;
                return false;
            }

            if (pdu is ScopedPdu)
            {
                throw new ArgumentException(
                    $"{nameof(pdu)} of type ScopedPdu is NOT supported by V2MessageProcessingModel");
            }

            if (pdu.RequestId <= 0)
            {
                pdu.RequestId = Random.Shared.Next();
            }

            outgoingMessage = new SnmpV2Message
            {
                Community = target.SecurityName,
                Pdu = pdu
            };

            sendPduHandle = pdu.RequestId;

            return true;
        }

    }
}
