/* Copyright 2021 Orry Verducci, 2017 Cinegy GmbH.

  Licensed under the Apache License, Version 2.0 (the "License");
  you may not use this file except in compliance with the License.
  You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

  Unless required by applicable law or agreed to in writing, software
  distributed under the License is distributed on an "AS IS" BASIS,
  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  See the License for the specific language governing permissions and
  limitations under the License.
*/

using System;
using System.Collections.Generic;
using Cinegy.TsDecoder.TransportStream;

namespace TtxFromTS.DVB
{
    /// <summary>
    /// Decodes packets of data from a MPEG transport stream.
    /// </summary>
    public class TSDecoder : TsPacketFactory
    {
        #region Private Fields
        /// <summary>
        /// Indicates if a PES error warning has been output.
        /// </summary>
        private bool _errorWarning;

        /// <summary>
        /// Indicates if an encrypted warning has been output.
        /// </summary>
        private bool _encryptedWarning;

        /// <summary>
        /// Data carried over from previous calls to the packet decoding methods.
        /// </summary>
        private byte[]? _residualData;

        /// <summary>
        /// The last received PCR.
        /// </summary>
        private ulong _lastPcr;

        /// <summary>
        /// The TS sync byte.
        /// </summary>
        private const byte SyncByte = 0x47;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the packet identifier to be decoded. If set to -1 (the default) all packets are decoded.
        /// </summary>
        /// <value>The packet identifier.</value>
        public int PacketID { set; get; } = -1;

        /// <summary>
        /// Gets the number of TS packets that have been received from the data provided.
        /// </summary>
        /// <value>The packets received.</value>
        public int PacketsReceived { private set; get; }

        /// <summary>
        /// Gets the number of TS packets from the specified Packet ID required that have been successfully decoded.
        /// </summary>
        /// <value>The packets decoded.</value>
        public int PacketsDecoded { private set; get; }
        #endregion

        #region Decoding Methods
        /// <summary>
        /// Decodes transport stream packets from the data provided.
        /// </summary>
        /// <param name="data">The data to be decoded.</param>
        /// <returns>A list of transport stream packets.</returns>
        public List<TsPacket> DecodePackets(byte[] data)
        {
            // Create a list of TS packets to return
            List<TsPacket> tsPackets = new List<TsPacket>();
            // Get packets from the data
            TsPacket[]? decodedTsPackets = GetTsPacketsFromData(data);
            // Return empty list if no packets were decoded
            if (decodedTsPackets == null)
            {
                return tsPackets;
            }
            // For each TS packet decoded increase the packet counters and add it to the list of packets to be returned if the packet is from the specified packet ID
            foreach (TsPacket packet in decodedTsPackets)
            {
                // Increase the received packet counter
                PacketsReceived++;
                // If the packet is free from errors and from the right packet ID, add it to the list of packets to be returned
                if (packet.TransportErrorIndicator)
                {
                    if (!_errorWarning)
                    {
                        Logger.OutputWarning("The transport stream contains errors which will be ignored, possibly as a result of poor reception");
                        _errorWarning = true;
                    }
                }
                else if (PacketID == -1 || packet.Pid == PacketID)
                {
                    // Add the packet if it is not encrypted, otherwise output warning
                    if (packet.ScramblingControl == 0)
                    {
                        PacketsDecoded++;
                        tsPackets.Add(packet);
                    }
                    else if (!_encryptedWarning)
                    {
                        Logger.OutputWarning("The specified packet ID contains encrypted packets which will be ignored");
                        _encryptedWarning = true;
                    }
                }
            }
            // Return the decoded elementary stream packets
            return tsPackets;
        }

        /// <summary>
        /// Returns TsPackets for any input data. If data ends with incomplete packet, this is stored and prepended to next call. 
        /// If data stream is restarted, prior buffer will be skipped as sync will not be acknowledged - but any restarts should being with first byte as sync to avoid possible merging with prior data if lengths coincide.
        /// </summary>
        /// <remarks>
        /// This has been copied from the parent TsPacketFactory class, but modified to return packets with error indicators
        /// if the adaptation field size exceeds the payload and to ignore timestamps (and their errors).
        /// </remarks>
        /// <param name="data">Aligned or unaligned data buffer containing TS packets. Aligned is more efficient if possible.</param>
        /// <param name="dataSize">Optional length parameter to limit amount of data read from referenced array.</param>
        /// <param name="retainPayload">Optional parameter to trigger any resulting TS payload to be copied into the returned structure</param>
        /// <param name="preserveSourceData">Optional parameter to trigger complete copy of source data for TS packet to be held in array for quick access</param>
        /// <returns>Complete TS packets from this data and any prior partial data rolled over.</returns>
        public new TsPacket[]? GetTsPacketsFromData(byte[] data, int dataSize = 0, bool retainPayload = true, bool preserveSourceData = false)
        {
            try
            {
                if (dataSize == 0)
                {
                    dataSize = data.Length;
                }

                if (_residualData != null)
                {
                    byte[] tempArray = new byte[dataSize];
                    Buffer.BlockCopy(data, 0, tempArray, 0, dataSize);
                    data = new byte[_residualData.Length + tempArray.Length];
                    Buffer.BlockCopy(_residualData, 0, data, 0, _residualData.Length);
                    Buffer.BlockCopy(tempArray, 0, data, _residualData.Length, tempArray.Length);
                    dataSize = data.Length;
                }

                int maxPackets = (dataSize) / TsPacketFixedSize;
                TsPacket[] tsPackets = new TsPacket[maxPackets];

                int packetCounter = 0;

                int start = FindSync(data, 0, TsPacketFixedSize);

                while (start >= 0 && ((dataSize - start) >= TsPacketFixedSize))
                {
                    TsPacket tsPacket = new TsPacket
                    {
                        SyncByte = data[start],
                        Pid = (short)(((data[start + 1] & 0x1F) << 8) + (data[start + 2])),
                        TransportErrorIndicator = (data[start + 1] & 0x80) != 0,
                        PayloadUnitStartIndicator = (data[start + 1] & 0x40) != 0,
                        TransportPriority = (data[start + 1] & 0x20) != 0,
                        ScramblingControl = (short)(data[start + 3] >> 6),
                        AdaptationFieldExists = (data[start + 3] & 0x20) != 0,
                        ContainsPayload = (data[start + 3] & 0x10) != 0,
                        ContinuityCounter = (short)(data[start + 3] & 0xF),
                        SourceBufferIndex = start
                    };

                    if (preserveSourceData)
                    {
                        tsPacket.SourceData = new byte[TsPacketFixedSize];
                        Buffer.BlockCopy(data, start, tsPacket.SourceData, 0, TsPacketFixedSize);
                    }

                    // Skip packets with error indicators or on the null PID
                    if (!tsPacket.TransportErrorIndicator && (tsPacket.Pid != (short)PidType.NullPid))
                    {
                        var payloadOffs = start + 4;
                        var payloadSize = TsPacketFixedSize - 4;

                        if (tsPacket.AdaptationFieldExists)
                        {
                            tsPacket.AdaptationField = new AdaptationField()
                            {
                                FieldSize = data[start + 4],
                                DiscontinuityIndicator = (data[start + 5] & 0x80) != 0,
                                RandomAccessIndicator = (data[start + 5] & 0x40) != 0,
                                ElementaryStreamPriorityIndicator = (data[start + 5] & 0x20) != 0,
                                PcrFlag = (data[start + 5] & 0x10) != 0,
                                OpcrFlag = (data[start + 5] & 0x8) != 0,
                                SplicingPointFlag = (data[start + 5] & 0x4) != 0,
                                TransportPrivateDataFlag = (data[start + 5] & 0x2) != 0,
                                AdaptationFieldExtensionFlag = (data[start + 5] & 0x1) != 0
                            };

                            if (tsPacket.AdaptationField.FieldSize >= payloadSize)
                            {
                                tsPacket.TransportErrorIndicator = true;
                                tsPackets[packetCounter++] = tsPacket;
                                start += TsPacketFixedSize;
                                continue;
                            }

                            if (tsPacket.AdaptationField.PcrFlag && tsPacket.AdaptationField.FieldSize > 0)
                            {
                                //Packet has PCR
                                tsPacket.AdaptationField.Pcr = (((uint)(data[start + 6]) << 24) +
                                                                ((uint)(data[start + 7] << 16)) +
                                                                ((uint)(data[start + 8] << 8)) + (data[start + 9]));

                                tsPacket.AdaptationField.Pcr <<= 1;

                                if ((data[start + 10] & 0x80) == 1)
                                {
                                    tsPacket.AdaptationField.Pcr |= 1;
                                }

                                tsPacket.AdaptationField.Pcr *= 300;
                                uint iLow = (uint)((data[start + 10] & 1) << 8) + data[start + 11];
                                tsPacket.AdaptationField.Pcr += iLow;

                                if (_lastPcr == 0)
                                {
                                    _lastPcr = tsPacket.AdaptationField.Pcr;
                                }
                            }

                            payloadSize -= tsPacket.AdaptationField.FieldSize;
                            payloadOffs += tsPacket.AdaptationField.FieldSize;
                        }

                        if (tsPacket.ContainsPayload && tsPacket.PayloadUnitStartIndicator)
                        {
                            if (payloadOffs > (dataSize - 2) || data[payloadOffs] != 0 || data[payloadOffs + 1] != 0 || data[payloadOffs + 2] != 1)
                            {
                                // Do nothing
                            }
                            else
                            {
                                tsPacket.PesHeader = new PesHdr
                                {
                                    StartCode = (uint)((data[payloadOffs] << 16) + (data[payloadOffs + 1] << 8) + data[payloadOffs + 2]),
                                    StreamId = data[payloadOffs + 3],
                                    PacketLength = (ushort)((data[payloadOffs + 4] << 8) + data[payloadOffs + 5]),
                                    Pts = -1,
                                    Dts = -1
                                };

                                tsPacket.PesHeader.HeaderLength = (byte)tsPacket.PesHeader.PacketLength;

                                byte stmrId = tsPacket.PesHeader.StreamId; //just copying to small name to make code less huge and slightly faster...

                                if ((stmrId != (uint)PesStreamTypes.ProgramStreamMap) &&
                                    (stmrId != (uint)PesStreamTypes.PaddingStream) &&
                                    (stmrId != (uint)PesStreamTypes.PrivateStream2) &&
                                    (stmrId != (uint)PesStreamTypes.ECMStream) &&
                                    (stmrId != (uint)PesStreamTypes.EMMStream) &&
                                    (stmrId != (uint)PesStreamTypes.ProgramStreamDirectory) &&
                                    (stmrId != (uint)PesStreamTypes.DSMCCStream) &&
                                    (stmrId != (uint)PesStreamTypes.H2221TypeEStream))
                                {
                                    tsPacket.PesHeader.HeaderLength = (byte)(9 + data[payloadOffs + 8]);
                                }

                                tsPacket.PesHeader.Payload = new byte[tsPacket.PesHeader.HeaderLength];
                                Buffer.BlockCopy(data, payloadOffs, tsPacket.PesHeader.Payload, 0, tsPacket.PesHeader.HeaderLength);

                                payloadOffs += tsPacket.PesHeader.HeaderLength;
                                payloadSize -= tsPacket.PesHeader.HeaderLength;
                            }
                        }

                        if (payloadSize > 1 && retainPayload)
                        {
                            tsPacket.Payload = new byte[payloadSize];
                            Buffer.BlockCopy(data, payloadOffs, tsPacket.Payload, 0, payloadSize);
                        }
                    }

                    tsPackets[packetCounter++] = tsPacket;

                    start += TsPacketFixedSize;

                    if (start >= dataSize)
                    {
                        break;
                    }
                    if (data[start] != SyncByte)
                    {
                        break;  // but this is strange!
                    }
                }

                if ((start + TsPacketFixedSize) != dataSize)
                {
                    //we have 'residual' data to carry over to next call
                    _residualData = new byte[dataSize - start];
                    Buffer.BlockCopy(data, start, _residualData, 0, dataSize - start);
                }

                return tsPackets;
            }
            catch
            {
                return null;
            }
        }
        #endregion
    }
}
