using System;
using System.Collections.Generic;
using System.IO;
using VGMToolbox.util;

namespace VGMToolbox.format
{
    public abstract class MpegStream
    {
        public enum PacketSizeType
        {
            Static,
            SizeBytes,
            Eof
        }

        protected static readonly byte[] PacketStartBytes = {0x00, 0x00, 0x01, 0xBA};
        protected static readonly byte[] PacketEndBytes = {0x00, 0x00, 0x01, 0xB9};

        #region Dictionary Initialization

        protected Dictionary<uint, BlockSizeStruct> BlockIdDictionary =
            new()
            {
                //********************
                // System Packets
                //********************
                {BitConverter.ToUInt32(PacketEndBytes, 0), new BlockSizeStruct(PacketSizeType.Eof, -1)}, // Program End
                {
                    BitConverter.ToUInt32(PacketStartBytes, 0), new BlockSizeStruct(PacketSizeType.Static, 0xE)
                }, // Pack Header
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xBB}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // System Header, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xBD}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Private Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xBE}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Padding Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xBF}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Private Stream, two bytes following equal length (Big Endian)

                //****************************
                // Audio Streams
                //****************************
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xC0}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Audio Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xC1}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Audio Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xC2}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Audio Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xC3}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Audio Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xC4}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Audio Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xC5}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Audio Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xC6}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Audio Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xC7}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Audio Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xC8}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Audio Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xC9}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Audio Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xCA}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Audio Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xCB}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Audio Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xCC}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Audio Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xCD}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Audio Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xCE}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Audio Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xCF}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Audio Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xD0}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Audio Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xD1}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Audio Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xD2}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Audio Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xD3}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Audio Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xD4}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Audio Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xD5}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Audio Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xD6}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Audio Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xD7}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Audio Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xD8}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Audio Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xD9}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Audio Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xDA}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Audio Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xDB}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Audio Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xDC}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Audio Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xDD}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Audio Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xDE}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Audio Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xDF}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Audio Stream, two bytes following equal length (Big Endian)

                //****************************
                // Video Streams
                //****************************
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xE0}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Video Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xE1}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Video Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xE2}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Video Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xE3}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Video Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xE4}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Video Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xE5}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Video Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xE6}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Video Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xE7}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Video Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xE8}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Video Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xE9}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Video Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xEA}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Video Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xEB}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Video Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xEC}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Video Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xED}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Video Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xEE}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                }, // Video Stream, two bytes following equal length (Big Endian)
                {
                    BitConverter.ToUInt32(new byte[] {0x00, 0x00, 0x01, 0xEF}, 0),
                    new BlockSizeStruct(PacketSizeType.SizeBytes, 2)
                } // Video Stream, two bytes following equal length (Big Endian)
            };

        #endregion

        protected Dictionary<byte, string> StreamIdFileType = new();

        public MpegStream(string path)
        {
            FilePath = path;
            UsesSameIdForMultipleAudioTracks = false;
            SubTitleExtractionSupported = false;
            BlockSizeIsLittleEndian = false;

            //********************
            // Add Slice Packets 
            //********************
            byte[] sliceBytes;
            uint sliceBytesValue;
            var blockSize = new BlockSizeStruct(PacketSizeType.Static, 0xE);

            for (byte i = 0; i <= 0xAF; i++)
            {
                sliceBytes = new byte[] {0x00, 0x00, 0x01, i};
                sliceBytesValue = BitConverter.ToUInt32(sliceBytes, 0);
                BlockIdDictionary.Add(sliceBytesValue, blockSize);
            }
        }

        public string FilePath { get; set; }
        public string FileExtensionAudio { get; set; }
        public string FileExtensionVideo { get; set; }

        public bool
            UsesSameIdForMultipleAudioTracks { set; get; } // for PMF/PAM/DVD, who use 000001BD for all audio tracks

        public bool SubTitleExtractionSupported { set; get; } // assume not supported.

        public bool BlockSizeIsLittleEndian { set; get; }

        protected virtual byte[] GetPacketStartBytes()
        {
            return PacketStartBytes;
        }

        protected virtual byte[] GetPacketEndBytes()
        {
            return PacketEndBytes;
        }

        protected abstract int GetAudioPacketHeaderSize(Stream readStream, long currentOffset);

        protected virtual int GetAudioPacketSubHeaderSize(Stream readStream, long currentOffset, byte streamId)
        {
            return 0;
        }

        protected abstract int GetVideoPacketHeaderSize(Stream readStream, long currentOffset);

        protected virtual int GetAudioPacketFooterSize(Stream readStream, long currentOffset)
        {
            return 0;
        }

        protected virtual int GetVideoPacketFooterSize(Stream readStream, long currentOffset)
        {
            return 0;
        }

        protected virtual bool IsThisAnAudioBlock(byte[] blockToCheck)
        {
            return blockToCheck[3] >= 0xC0 &&
                   blockToCheck[3] <= 0xDF;
        }

        protected virtual bool IsThisAVideoBlock(byte[] blockToCheck)
        {
            return blockToCheck[3] >= 0xE0 && blockToCheck[3] <= 0xEF;
        }

        protected virtual bool IsThisASubPictureBlock(byte[] blockToCheck)
        {
            return blockToCheck[3] >= 0xE0 && blockToCheck[3] <= 0xEF;
        }

        protected virtual string GetAudioFileExtension(Stream readStream, long currentOffset)
        {
            return FileExtensionAudio;
        }

        protected virtual string GetVideoFileExtension(Stream readStream, long currentOffset)
        {
            return FileExtensionVideo;
        }

        protected virtual byte GetStreamId(Stream readStream, long currentOffset)
        {
            return 0;
        }

        protected virtual long GetStartOffset(Stream readStream, long currentOffset)
        {
            return 0;
        }

        protected virtual string[] DoFinalTasks(FileStream sourceFileStream, Dictionary<uint, FileStream> outputFiles,
            bool addHeader)
        {
            return null;
        }

        public virtual string[] DemultiplexStreams(DemuxOptionsStruct demuxOptions)
        {
            using (var fs = File.OpenRead(FilePath))
            {
                var fileSize = fs.Length;
                long currentOffset = 0;

                byte[] currentBlockId;
                uint currentBlockIdVal;
                byte[] currentBlockIdNaming;

                var blockStruct = new BlockSizeStruct();
                byte[] blockSizeArray;
                uint blockSize;

                int audioBlockSkipSize;
                int videoBlockSkipSize;

                int audioBlockFooterSize;
                int videoBlockFooterSize;

                int cutSize;

                var eofFlagFound = false;

                var streamOutputWriters = new Dictionary<uint, FileStream>();
                string outputFileName;

                byte streamId = 0; // for types that have multiple streams in the same block ID
                uint currentStreamKey; // hash key for each file
                bool isAudioBlock;
                string audioFileExtension;

                // look for first packet
                currentOffset = GetStartOffset(fs, currentOffset);
                currentOffset = ParseFile.GetNextOffset(fs, currentOffset, GetPacketStartBytes());

                if (currentOffset != -1)
                {
                    while (currentOffset < fileSize)
                    {
#if DEBUG
                        //if (currentOffset == 0x414080e)
                        //{
                        //    int gggg = 1;
                        //}

                        //// hack for bad data (ni no kuni s09.pam)
                        //if ((currentOffset & 1) == 1)
                        //{
                        //    currentOffset = MathUtil.RoundUpToByteAlignment(currentOffset, 0x800);
                        //}
#endif

                        try
                        {
                            // get the current block
                            currentBlockId = ParseFile.ParseSimpleOffset(fs, currentOffset, 4);

                            // get value to use as key to hash table
                            currentBlockIdVal = BitConverter.ToUInt32(currentBlockId, 0);

                            if (BlockIdDictionary.ContainsKey(currentBlockIdVal))
                            {
                                // get info about this block type
                                blockStruct = BlockIdDictionary[currentBlockIdVal];

                                switch (blockStruct.SizeType)
                                {
                                    /////////////////////
                                    // Static Block Size
                                    /////////////////////
                                    case PacketSizeType.Static:
                                        currentOffset += blockStruct.Size; // skip this block
                                        break;

                                    //////////////////
                                    // End of Stream
                                    //////////////////
                                    case PacketSizeType.Eof:
                                        eofFlagFound = true; // set EOF block found so we can exit the loop
                                        break;

                                    //////////////////////
                                    // Varying Block Size
                                    //////////////////////
                                    case PacketSizeType.SizeBytes:

                                        // Get the block size
                                        blockSizeArray = ParseFile.ParseSimpleOffset(fs,
                                            currentOffset + currentBlockId.Length, blockStruct.Size);

                                        if (!BlockSizeIsLittleEndian) Array.Reverse(blockSizeArray);

                                        switch (blockStruct.Size)
                                        {
                                            case 4:
                                                blockSize = BitConverter.ToUInt32(blockSizeArray, 0);
                                                break;
                                            case 2:
                                                blockSize = BitConverter.ToUInt16(blockSizeArray, 0);
                                                break;
                                            case 1:
                                                blockSize = blockSizeArray[0];
                                                break;
                                            default:
                                                throw new ArgumentOutOfRangeException(
                                                    string.Format("Unhandled size block size.{0}",
                                                        Environment.NewLine));
                                        }


                                        // if block type is audio or video, extract it
                                        isAudioBlock = IsThisAnAudioBlock(currentBlockId);

                                        if (demuxOptions.ExtractAudio && isAudioBlock ||
                                            demuxOptions.ExtractVideo && IsThisAVideoBlock(currentBlockId))
                                        {
                                            // reset stream id
                                            streamId = 0;

                                            // if audio block, get the stream number from the queue                                                                                
                                            if (isAudioBlock && UsesSameIdForMultipleAudioTracks)
                                            {
                                                streamId = GetStreamId(fs, currentOffset);
                                                currentStreamKey = streamId | currentBlockIdVal;
                                            }
                                            else
                                            {
                                                currentStreamKey = currentBlockIdVal;
                                            }

                                            // check if we've already started parsing this stream
                                            if (!streamOutputWriters.ContainsKey(currentStreamKey))
                                            {
                                                // convert block id to little endian for naming
                                                currentBlockIdNaming = BitConverter.GetBytes(currentStreamKey);
                                                Array.Reverse(currentBlockIdNaming);

                                                // build output file name
                                                outputFileName = Path.GetFileNameWithoutExtension(FilePath);
                                                outputFileName = outputFileName + "_" + BitConverter
                                                    .ToUInt32(currentBlockIdNaming, 0).ToString("X8");

                                                // add proper extension
                                                if (IsThisAnAudioBlock(currentBlockId))
                                                {
                                                    audioFileExtension = GetAudioFileExtension(fs, currentOffset);
                                                    outputFileName += audioFileExtension;

                                                    if (!StreamIdFileType.ContainsKey(streamId))
                                                        StreamIdFileType.Add(streamId, audioFileExtension);
                                                }
                                                else
                                                {
                                                    FileExtensionVideo = GetVideoFileExtension(fs, currentOffset);
                                                    outputFileName += FileExtensionVideo;
                                                }

                                                // add output directory
                                                outputFileName = Path.Combine(Path.GetDirectoryName(FilePath),
                                                    outputFileName);

                                                // add an output stream for writing
                                                streamOutputWriters[currentStreamKey] = new FileStream(outputFileName,
                                                    FileMode.Create, FileAccess.ReadWrite);
                                            }

                                            // write the block
                                            if (IsThisAnAudioBlock(currentBlockId))
                                            {
                                                // write audio
                                                audioBlockSkipSize = GetAudioPacketHeaderSize(fs, currentOffset) +
                                                                     GetAudioPacketSubHeaderSize(fs, currentOffset,
                                                                         streamId);
                                                audioBlockFooterSize = GetAudioPacketFooterSize(fs, currentOffset);
                                                cutSize = (int) (blockSize - audioBlockSkipSize - audioBlockFooterSize);
                                                if (cutSize > 0)
                                                    streamOutputWriters[currentStreamKey].Write(
                                                        ParseFile.ParseSimpleOffset(fs,
                                                            currentOffset + currentBlockId.Length +
                                                            blockSizeArray.Length + audioBlockSkipSize,
                                                            (int) (blockSize - audioBlockSkipSize)), 0, cutSize);
#if DEBUG
                                                //else
                                                //{
                                                //    int aaa = 1;
                                                //}
#endif
                                            }
                                            else
                                            {
                                                // write video
                                                videoBlockSkipSize = GetVideoPacketHeaderSize(fs, currentOffset);
                                                videoBlockFooterSize = GetVideoPacketFooterSize(fs, currentOffset);
                                                cutSize = (int) (blockSize - videoBlockSkipSize - videoBlockFooterSize);
                                                if (cutSize > 0)
                                                    streamOutputWriters[currentStreamKey].Write(
                                                        ParseFile.ParseSimpleOffset(fs,
                                                            currentOffset + currentBlockId.Length +
                                                            blockSizeArray.Length + videoBlockSkipSize,
                                                            (int) (blockSize - videoBlockSkipSize)), 0, cutSize);
#if DEBUG
                                                //else
                                                //{
                                                //    int vvv = 1;
                                                //}
#endif
                                            }
                                        }

                                        // move to next block
                                        currentOffset += currentBlockId.Length + blockSizeArray.Length + blockSize;
                                        blockSizeArray = new byte[] { };
                                        break;
                                }
                            }
                            else // this is an undexpected block type
                            {
                                closeAllWriters(streamOutputWriters);
                                Array.Reverse(currentBlockId);
                                throw new FormatException(string.Format("Block ID at 0x{0} not found in table: 0x{1}",
                                    currentOffset.ToString("X8"),
                                    BitConverter.ToUInt32(currentBlockId, 0).ToString("X8")));
                            }

                            // exit loop if EOF block found
                            if (eofFlagFound) break;
                        }
                        catch (Exception _ex)
                        {
                            closeAllWriters(streamOutputWriters);
                            throw new Exception(
                                string.Format("Error parsing file at offset {0), '{1}'", currentOffset.ToString("X8"),
                                    _ex.Message), _ex);
                        }
                    } // while (currentOffset < fileSize)
                }
                else
                {
                    closeAllWriters(streamOutputWriters);
                    throw new FormatException(string.Format("Cannot find Pack Header for file: {0}{1}",
                        Path.GetFileName(FilePath), Environment.NewLine));
                }

                ///////////////////////////////////
                // Perform any final tasks needed
                ///////////////////////////////////
                var files = DoFinalTasks(fs, streamOutputWriters, demuxOptions.AddHeader);

                //////////////////////////
                // close all open writers
                //////////////////////////
                closeAllWriters(streamOutputWriters);
                return files;
            } // using (FileStream fs = File.OpenRead(path))
        }

        private void closeAllWriters(Dictionary<uint, FileStream> writers)
        {
            //////////////////////////
            // close all open writers
            //////////////////////////
            foreach (var b in writers.Keys)
                if (writers[b].CanRead)
                {
                    writers[b].Close();
                    writers[b].Dispose();
                }
        }

        public static int GetMpegStreamType(string path)
        {
            var mpegType = -1;

            using (var fs = File.OpenRead(path))
            {
                // look for first packet
                var currentOffset = ParseFile.GetNextOffset(fs, 0, PacketStartBytes);

                if (currentOffset != -1)
                {
                    currentOffset += 4;
                    fs.Position = currentOffset;
                    var idByte = (byte) fs.ReadByte();

                    if (ByteConversion.GetHighNibble(idByte) == 2)
                        mpegType = 1;
                    else if (ByteConversion.GetHighNibble(idByte) == 4) mpegType = 2;
                }
                else
                {
                    throw new FormatException(string.Format("Cannot find Pack Header for file: {0}{1}",
                        Path.GetFileName(path), Environment.NewLine));
                }
            }

            return mpegType;
        }

        public struct MpegDemuxOptions
        {
            public bool AddHeader { set; get; }
        }

        public struct BlockSizeStruct
        {
            public PacketSizeType SizeType;
            public int Size;

            public BlockSizeStruct(PacketSizeType sizeTypeValue, int sizeValue)
            {
                SizeType = sizeTypeValue;
                Size = sizeValue;
            }
        }

        public struct DemuxOptionsStruct
        {
            public bool ExtractVideo { set; get; }
            public bool ExtractAudio { set; get; }

            public bool AddHeader { set; get; }
            public bool SplitAudioStreams { set; get; }
            public bool AddPlaybackHacks { set; get; }
        }
    }
}