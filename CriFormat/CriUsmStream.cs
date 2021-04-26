using System;
using System.Collections.Generic;
using System.IO;
using VGMToolbox.util;

namespace VGMToolbox.format
{
    public class CriUsmStream : MpegStream
    {
        public const string DefaultAudioExtension = ".adx";
        public const string DefaultVideoExtension = ".m2v";
        public const string HcaAudioExtension = ".hca";

        private static readonly byte[] HCA_SIG_BYTES = {0x48, 0x43, 0x41, 0x00};

        protected static readonly byte[] ALP_BYTES = {0x40, 0x41, 0x4C, 0x50};
        protected static readonly byte[] CRID_BYTES = {0x43, 0x52, 0x49, 0x44};
        protected static readonly byte[] SFV_BYTES = {0x40, 0x53, 0x46, 0x56};
        protected static readonly byte[] SFA_BYTES = {0x40, 0x53, 0x46, 0x41};
        protected static readonly byte[] SBT_BYTES = {0x40, 0x53, 0x42, 0x54};
        protected static readonly byte[] CUE_BYTES = {0x40, 0x43, 0x55, 0x45};

        protected static readonly byte[] UTF_BYTES = {0x40, 0x55, 0x54, 0x46};

        protected static readonly byte[] HEADER_END_BYTES =
        {
            0x23, 0x48, 0x45, 0x41, 0x44, 0x45, 0x52, 0x20,
            0x45, 0x4E, 0x44, 0x20, 0x20, 0x20, 0x20, 0x20,
            0x3D, 0x3D, 0x3D, 0x3D, 0x3D, 0x3D, 0x3D, 0x3D,
            0x3D, 0x3D, 0x3D, 0x3D, 0x3D, 0x3D, 0x3D, 0x00
        };

        protected static readonly byte[] METADATA_END_BYTES =
        {
            0x23, 0x4D, 0x45, 0x54, 0x41, 0x44, 0x41, 0x54,
            0x41, 0x20, 0x45, 0x4E, 0x44, 0x20, 0x20, 0x20,
            0x3D, 0x3D, 0x3D, 0x3D, 0x3D, 0x3D, 0x3D, 0x3D,
            0x3D, 0x3D, 0x3D, 0x3D, 0x3D, 0x3D, 0x3D, 0x00
        };

        protected static readonly byte[] CONTENTS_END_BYTES =
        {
            0x23, 0x43, 0x4F, 0x4E, 0x54, 0x45, 0x4E, 0x54,
            0x53, 0x20, 0x45, 0x4E, 0x44, 0x20, 0x20, 0x20,
            0x3D, 0x3D, 0x3D, 0x3D, 0x3D, 0x3D, 0x3D, 0x3D,
            0x3D, 0x3D, 0x3D, 0x3D, 0x3D, 0x3D, 0x3D, 0x00
        };

        public CriUsmStream(string path)
            : base(path)
        {
            UsesSameIdForMultipleAudioTracks = true;
            FileExtensionAudio = DefaultAudioExtension;
            FileExtensionVideo = DefaultVideoExtension;

            BlockIdDictionary.Clear();
            BlockIdDictionary[BitConverter.ToUInt32(ALP_BYTES, 0)] =
                new BlockSizeStruct(PacketSizeType.SizeBytes, 4); // @ALP
            BlockIdDictionary[BitConverter.ToUInt32(CRID_BYTES, 0)] =
                new BlockSizeStruct(PacketSizeType.SizeBytes, 4); // CRID
            BlockIdDictionary[BitConverter.ToUInt32(SFV_BYTES, 0)] =
                new BlockSizeStruct(PacketSizeType.SizeBytes, 4); // @SFV
            BlockIdDictionary[BitConverter.ToUInt32(SFA_BYTES, 0)] =
                new BlockSizeStruct(PacketSizeType.SizeBytes, 4); // @SFA
            BlockIdDictionary[BitConverter.ToUInt32(SBT_BYTES, 0)] =
                new BlockSizeStruct(PacketSizeType.SizeBytes, 4); // @SBT
            BlockIdDictionary[BitConverter.ToUInt32(CUE_BYTES, 0)] =
                new BlockSizeStruct(PacketSizeType.SizeBytes, 4); // @CUE
        }

        protected override byte[] GetPacketStartBytes()
        {
            return CRID_BYTES;
        }

        protected override int GetAudioPacketHeaderSize(Stream readStream, long currentOffset)
        {
            ushort checkBytes;
            var od = new OffsetDescription();

            od.OffsetByteOrder = Constants.BigEndianByteOrder;
            od.OffsetSize = "2";
            od.OffsetValue = "8";

            checkBytes = (ushort) ParseFile.GetVaryingByteValueAtRelativeOffset(readStream, od, currentOffset);

            return checkBytes;
        }

        protected override int GetVideoPacketHeaderSize(Stream readStream, long currentOffset)
        {
            ushort checkBytes;
            var od = new OffsetDescription();

            od.OffsetByteOrder = Constants.BigEndianByteOrder;
            od.OffsetSize = "2";
            od.OffsetValue = "8";

            checkBytes = (ushort) ParseFile.GetVaryingByteValueAtRelativeOffset(readStream, od, currentOffset);

            return checkBytes;
        }

        protected override bool IsThisAnAudioBlock(byte[] blockToCheck)
        {
            return ParseFile.CompareSegment(blockToCheck, 0, SFA_BYTES);
        }

        protected override bool IsThisAVideoBlock(byte[] blockToCheck)
        {
            return ParseFile.CompareSegment(blockToCheck, 0, SFV_BYTES);
        }

        protected override byte GetStreamId(Stream readStream, long currentOffset)
        {
            byte streamId;

            streamId = ParseFile.ParseSimpleOffset(readStream, currentOffset + 0xC, 1)[0];

            return streamId;
        }

        protected override int GetAudioPacketFooterSize(Stream readStream, long currentOffset)
        {
            ushort checkBytes;
            var od = new OffsetDescription();

            od.OffsetByteOrder = Constants.BigEndianByteOrder;
            od.OffsetSize = "2";
            od.OffsetValue = "0xA";

            checkBytes = (ushort) ParseFile.GetVaryingByteValueAtRelativeOffset(readStream, od, currentOffset);

            return checkBytes;
        }

        protected override int GetVideoPacketFooterSize(Stream readStream, long currentOffset)
        {
            ushort checkBytes;
            var od = new OffsetDescription();

            od.OffsetByteOrder = Constants.BigEndianByteOrder;
            od.OffsetSize = "2";
            od.OffsetValue = "0xA";

            checkBytes = (ushort) ParseFile.GetVaryingByteValueAtRelativeOffset(readStream, od, currentOffset);

            return checkBytes;
        }

        protected override string[] DoFinalTasks(FileStream sourceFileStream, Dictionary<uint, FileStream> outputFiles,
            bool addHeader)
        {
            long headerEndOffset;
            long metadataEndOffset;
            long headerSize;

            long footerOffset;
            long footerSize;

            string sourceFileName;
            string workingFile;
            string fileExtension;
            string destinationFileName;

            var files = new List<string>(outputFiles.Count);

            foreach (var streamId in outputFiles.Keys)
            {
                sourceFileName = outputFiles[streamId].Name;

                //--------------------------
                // get header size
                //--------------------------
                headerEndOffset = ParseFile.GetNextOffset(outputFiles[streamId], 0, HEADER_END_BYTES);
                metadataEndOffset = ParseFile.GetNextOffset(outputFiles[streamId], 0, METADATA_END_BYTES);

                if (metadataEndOffset > headerEndOffset)
                    headerSize = metadataEndOffset + METADATA_END_BYTES.Length;
                else
                    headerSize = headerEndOffset + METADATA_END_BYTES.Length;

                //-----------------
                // get footer size
                //-----------------
                footerOffset = ParseFile.GetNextOffset(outputFiles[streamId], 0, CONTENTS_END_BYTES) - headerSize;
                footerSize = outputFiles[streamId].Length - footerOffset;

                //------------------------------------------
                // check data to adjust extension if needed
                //------------------------------------------
                if (IsThisAnAudioBlock(BitConverter.GetBytes(streamId & 0xFFFFFFF0))
                ) // may need to change mask if more than 0xF streams
                {
                    var checkBytes = ParseFile.ParseSimpleOffset(outputFiles[streamId], headerSize, 4);

                    if (ParseFile.CompareSegment(checkBytes, 0, SofdecStream.AixSignatureBytes))
                        fileExtension = SofdecStream.AixAudioExtension;
                    else if (checkBytes[0] == 0x80)
                        fileExtension = SofdecStream.AdxAudioExtension;
                    else if (ParseFile.CompareSegment(checkBytes, 0, HCA_SIG_BYTES))
                        fileExtension = HcaAudioExtension;
                    else
                        fileExtension = ".bin";
                }
                else
                {
                    fileExtension = Path.GetExtension(sourceFileName);
                }

                outputFiles[streamId].Close();
                outputFiles[streamId].Dispose();

                workingFile = FileUtil.RemoveChunkFromFile(sourceFileName, 0, headerSize);
                File.Move(workingFile, sourceFileName, true);

                workingFile = FileUtil.RemoveChunkFromFile(sourceFileName, footerOffset, footerSize);
                destinationFileName = Path.ChangeExtension(sourceFileName, fileExtension);
                File.Move(workingFile, destinationFileName, true);
                files.Add(destinationFileName);

                if (sourceFileName != destinationFileName && File.Exists(sourceFileName)) File.Delete(sourceFileName);
            }

            return files.ToArray();
        }
    }
}