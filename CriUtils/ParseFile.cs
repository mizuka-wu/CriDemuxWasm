﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace VGMToolbox.util
{
    /// <summary>
    ///     Class for Parsing Files.
    /// </summary>
    public static class ParseFile
    {
        public const string LogFileName = "vgmt_extraction_log.txt";
        public const string SnakeBiteBatchFileName = "vgmt_extraction_log.bat";
        public const string VirtualFileSystemExtractionFolder = "vgmt_vfs_cut";

        /// <summary>
        ///     Extract a section from the incoming byte array.
        /// </summary>
        /// <param name="sourceArray">Bytes to extract from.</param>
        /// <param name="startingOffset">Offset to begin cutting from.</param>
        /// <param name="lengthToCut">Number of bytes to cut.</param>
        /// <returns>Byte array containing the extracted section.</returns>
        public static byte[] ParseSimpleOffset(byte[] sourceArray, int startingOffset, int lengthToCut)
        {
            var ret = new byte[lengthToCut];
            uint j = 0;

            for (var i = startingOffset; i < startingOffset + lengthToCut; i++)
            {
                ret[j] = sourceArray[i];
                j++;
            }

            return ret;
        }

        /// <summary>
        ///     Extract a section from the incoming stream.
        /// </summary>
        /// <param name="stream">Stream to extract the chunk from.</param>
        /// <param name="startingOffset">Offset to begin cutting from.</param>
        /// <param name="lengthToCut">Number of bytes to cut.</param>
        /// <returns>Byte array containing the extracted section.</returns>
        public static byte[] ParseSimpleOffset(Stream stream, int startingOffset, int lengthToCut)
        {
            var ret = new byte[lengthToCut];
            var currentStreamPosition = stream.Position;

            stream.Seek(startingOffset, SeekOrigin.Begin);
            var br = new BinaryReader(stream);
            ret = br.ReadBytes(lengthToCut);

            stream.Position = currentStreamPosition;

            return ret;
        }

        /// <summary>
        ///     Extract a section from the incoming stream.
        /// </summary>
        /// <param name="stream">Stream to extract the chunk from.</param>
        /// <param name="startingOffset">Offset to begin cutting from.</param>
        /// <param name="lengthToCut">Number of bytes to cut.</param>
        /// <returns>Byte array containing the extracted section.</returns>
        public static byte[] ParseSimpleOffset(Stream stream, long startingOffset, int lengthToCut)
        {
            var currentStreamPosition = stream.Position;

            stream.Seek(startingOffset, SeekOrigin.Begin);
            var ret = new byte[lengthToCut];
            stream.Read(ret, 0, lengthToCut);

            stream.Position = currentStreamPosition;

            return ret;
        }

        /// <summary>
        ///     Get the length from the input offset to the location of the input terminator bytes or zero.
        /// </summary>
        /// <param name="sourceBytes">Bytes to check.</param>
        /// <param name="searchOffset">Offset at which to begin searching for the terminator bytes.</param>
        /// <param name="terminatorBytes">Bytes to search for.</param>
        /// <returns>Length of distance between offset and terminator or zero if not found.</returns>
        public static int GetSegmentLength(byte[] sourceBytes, int searchOffset, byte[] terminatorBytes)
        {
            int ret;
            var terminatorFound = false;
            var i = searchOffset;

            while (i < sourceBytes.Length)
            {
                // first char match
                if (sourceBytes[i] == terminatorBytes[0])
                    if (CompareSegment(sourceBytes, i, terminatorBytes))
                    {
                        terminatorFound = true;
                        break;
                    }

                i++;
            } // while (!terminatorFound)

            if (terminatorFound)
                ret = i - searchOffset;
            else
                ret = 0;

            return ret;
        }

        /// <summary>
        ///     Get the length from the input offset to the location of the input terminator bytes or zero.
        /// </summary>
        /// <param name="stream">Stream to check.</param>
        /// <param name="searchOffset">Offset at which to begin searching for the terminator bytes.</param>
        /// <param name="terminatorBytes">Bytes to search for.</param>
        /// <returns>Length of distance between offset and terminator or zero if not found.</returns>
        public static int GetSegmentLength(Stream stream, int searchOffset, byte[] terminatorBytes)
        {
            int ret;
            var terminatorFound = false;
            var i = searchOffset;
            var checkBytes = new byte[terminatorBytes.Length];

            while (i < stream.Length)
            {
                stream.Seek(i, SeekOrigin.Begin);
                stream.Read(checkBytes, 0, 1);

                // first char match
                if (checkBytes[0] == terminatorBytes[0])
                {
                    stream.Seek(i, SeekOrigin.Begin);
                    stream.Read(checkBytes, 0, terminatorBytes.Length);

                    if (CompareSegment(checkBytes, 0, terminatorBytes))
                    {
                        terminatorFound = true;
                        break;
                    }
                }

                i++;
            } // while (!terminatorFound)

            if (terminatorFound)
                ret = i - searchOffset;
            else
                ret = 0;

            return ret;
        }

        /// <summary>
        ///     Get the offset of the first instance of pSearchBytes after the input offset.
        /// </summary>
        /// <param name="stream">Stream to search.</param>
        /// <param name="startingOffset">Offset to begin searching from.</param>
        /// <param name="searchBytes">Bytes to search for.</param>
        /// <returns>Returns the offset of the first instance of pSearchBytes after the input offset or -1 otherwise.</returns>
        public static long GetNextOffset(Stream stream, long startingOffset, byte[] searchBytes)
        {
            return GetNextOffset(stream, startingOffset, searchBytes, true);
        }

        public static long GetNextOffsetMasked(Stream stream, long startingOffset, byte[] searchBytes,
            byte[] searchMask)
        {
            return GetNextOffsetMasked(stream, startingOffset, searchBytes, searchMask, true);
        }

        public static long GetNextOffset(Stream stream, long startingOffset,
            byte[] searchBytes, bool returnStreamToIncomingPosition)
        {
            long initialStreamPosition = 0;

            if (returnStreamToIncomingPosition) initialStreamPosition = stream.Position;

            var itemFound = false;
            var absoluteOffset = startingOffset;
            int relativeOffset;
            //var checkBytes = new byte[Constants.FileReadChunkSize];
            Span<byte> checkSpan = stackalloc byte[Constants.FileReadChunkSize];

            long ret = -1;

            while (!itemFound && absoluteOffset < stream.Length)
            {
                stream.Position = absoluteOffset;
                //stream.Read(checkBytes, 0, Constants.FileReadChunkSize);
                stream.Read(checkSpan);
                relativeOffset = 0;

                while (relativeOffset < Constants.FileReadChunkSize)
                {
                    //if (relativeOffset + searchBytes.Length < checkBytes.Length)
                    if (relativeOffset + searchBytes.Length < checkSpan.Length)
                    {
                        //var compareSpan = new ReadOnlySpan<byte>(checkBytes, relativeOffset, searchBytes.Length);
                        var compareSpan = checkSpan.Slice(relativeOffset, searchBytes.Length);
                        var searchSpan = new ReadOnlySpan<byte>(searchBytes);

                        if (compareSpan.SequenceEqual(searchSpan))
                        {
                            itemFound = true;
                            ret = absoluteOffset + relativeOffset;
                            break;
                        }
                    }

                    relativeOffset++;
                }

                absoluteOffset += Constants.FileReadChunkSize - searchBytes.Length;
            }

            // return stream to incoming position
            if (returnStreamToIncomingPosition) stream.Position = initialStreamPosition;

            return ret;
        }

        public static long GetNextOffsetMasked(Stream stream, long startingOffset,
            byte[] searchBytes, byte[] searchMask, bool returnStreamToIncomingPosition)
        {
            long initialStreamPosition = 0;

            if (returnStreamToIncomingPosition) initialStreamPosition = stream.Position;

            var itemFound = false;
            var absoluteOffset = startingOffset;
            long relativeOffset;
            var checkBytes = new byte[Constants.FileReadChunkSize];
            byte[] compareBytes;

            long ret = -1;

            while (!itemFound && absoluteOffset < stream.Length)
            {
                stream.Position = absoluteOffset;
                stream.Read(checkBytes, 0, Constants.FileReadChunkSize);
                relativeOffset = 0;

                while (!itemFound && relativeOffset < Constants.FileReadChunkSize)
                {
                    if (relativeOffset + searchBytes.Length < checkBytes.Length)
                    {
                        compareBytes = new byte[searchBytes.Length];
                        Array.Copy(checkBytes, relativeOffset, compareBytes, 0, searchBytes.Length);
                        for (var idx = 0; idx < searchMask.Length; ++idx) compareBytes[idx] &= searchMask[idx];

                        if (CompareSegment(compareBytes, 0, searchBytes))
                        {
                            itemFound = true;
                            ret = absoluteOffset + relativeOffset;
                            break;
                        }
                    }

                    relativeOffset++;
                }

                absoluteOffset += Constants.FileReadChunkSize - searchBytes.Length;
            }

            // return stream to incoming position
            if (returnStreamToIncomingPosition) stream.Position = initialStreamPosition;

            return ret;
        }

        public static long GetNextOffset(Stream stream, long startingOffset,
            byte[] searchBytes, bool doOffsetModulo, long offsetModuloDivisor,
            long offsetModuloResult)
        {
            return GetNextOffset(stream, startingOffset, searchBytes,
                doOffsetModulo, offsetModuloDivisor, offsetModuloResult, true);
        }

        public static long GetNextOffset(Stream stream, long startingOffset,
            byte[] searchBytes, bool doOffsetModulo, long offsetModuloDivisor,
            long offsetModuloResult, bool returnStreamToIncomingPosition)
        {
            long initialStreamPosition = 0;

            if (returnStreamToIncomingPosition) initialStreamPosition = stream.Position;

            var itemFound = false;
            var absoluteOffset = startingOffset;
            long relativeOffset;
            long actualOffset;
            var checkBytes = new byte[Constants.FileReadChunkSize];
            int checkBytesRead;
            byte[] compareBytes;

            long ret = -1;

            while (!itemFound && absoluteOffset < stream.Length)
            {
                stream.Position = absoluteOffset;
                checkBytesRead = stream.Read(checkBytes, 0, Constants.FileReadChunkSize);
                relativeOffset = 0;

                while (!itemFound && relativeOffset < checkBytesRead)
                {
                    actualOffset = absoluteOffset + relativeOffset;

                    if (!doOffsetModulo ||
                        actualOffset % offsetModuloDivisor == offsetModuloResult)
                        if (relativeOffset + searchBytes.Length < checkBytes.Length)
                        {
                            compareBytes = new byte[searchBytes.Length];
                            Array.Copy(checkBytes, relativeOffset, compareBytes, 0, searchBytes.Length);

                            if (CompareSegment(compareBytes, 0, searchBytes))
                            {
                                itemFound = true;
                                ret = actualOffset;
                                break;
                            }
                        }

                    relativeOffset++;
                }

                absoluteOffset += Constants.FileReadChunkSize - searchBytes.Length;
            }

            // return stream to incoming position
            if (returnStreamToIncomingPosition) stream.Position = initialStreamPosition;

            return ret;
        }

        public static Dictionary<byte[], long[]> GetAllOffsets(Stream stream, long startingOffset,
            byte[][] searchByteArrays, bool returnStreamToIncomingPosition)
        {
            long initialStreamPosition = 0;

            if (returnStreamToIncomingPosition) initialStreamPosition = stream.Position;

            var maxSearchBytesLength = 0;
            var absoluteOffset = startingOffset;
            long relativeOffset;
            var checkBytes = new byte[Constants.FileReadChunkSize];
            byte[] compareBytes;

            var tempOutput = new Dictionary<byte[], ArrayList>();
            var ret = new Dictionary<byte[], long[]>();

            foreach (var b in searchByteArrays)
            {
                tempOutput.Add(b, new ArrayList());

                if (b.Length > maxSearchBytesLength) maxSearchBytesLength = b.Length;
            }

            while (absoluteOffset < stream.Length)
            {
                stream.Position = absoluteOffset;
                stream.Read(checkBytes, 0, Constants.FileReadChunkSize);
                relativeOffset = 0;

                while (relativeOffset < Constants.FileReadChunkSize)
                {
                    foreach (var searchBytes in searchByteArrays)
                        if (relativeOffset + searchBytes.Length < checkBytes.Length)
                        {
                            compareBytes = new byte[searchBytes.Length];
                            Array.Copy(checkBytes, relativeOffset, compareBytes, 0, searchBytes.Length);

                            if (CompareSegment(compareBytes, 0, searchBytes))
                            {
                                tempOutput[searchBytes].Add(absoluteOffset + relativeOffset);
                                break;
                            }
                        }

                    relativeOffset++;
                }

                absoluteOffset += Constants.FileReadChunkSize - maxSearchBytesLength;
            }

            // return stream to incoming position
            if (returnStreamToIncomingPosition) stream.Position = initialStreamPosition;

            // sort offsets and add to output
            foreach (var key in tempOutput.Keys)
            {
                tempOutput[key].Sort();
                ret.Add(key, (long[]) tempOutput[key].ToArray(typeof(long)));
            }

            return ret;
        }

        public static long[] GetAllOffsets(Stream stream, long startingOffset,
            byte[] searchBytes, bool doOffsetModulo, long offsetModuloDivisor,
            long offsetModuloResult, bool returnStreamToIncomingPosition)
        {
            long initialStreamPosition = 0;

            if (returnStreamToIncomingPosition) initialStreamPosition = stream.Position;

            var absoluteOffset = startingOffset;
            long relativeOffset;
            long actualOffset;
            var checkBytes = new byte[Constants.FileReadChunkSize];
            int checkBytesRead;
            byte[] compareBytes;

            var offsetList = new ArrayList();
            long[] ret;

            while (absoluteOffset < stream.Length)
            {
                stream.Position = absoluteOffset;
                checkBytesRead = stream.Read(checkBytes, 0, Constants.FileReadChunkSize);
                relativeOffset = 0;

                while (relativeOffset < checkBytesRead)
                {
                    actualOffset = absoluteOffset + relativeOffset;

                    if (!doOffsetModulo ||
                        actualOffset % offsetModuloDivisor == offsetModuloResult)
                        if (relativeOffset + searchBytes.Length < checkBytes.Length)
                        {
                            compareBytes = new byte[searchBytes.Length];
                            Array.Copy(checkBytes, relativeOffset, compareBytes, 0, searchBytes.Length);

                            if (CompareSegment(compareBytes, 0, searchBytes)) offsetList.Add(actualOffset);
                        }

                    relativeOffset++;
                }

                absoluteOffset += Constants.FileReadChunkSize - searchBytes.Length;
            }

            // return stream to incoming position
            if (returnStreamToIncomingPosition) stream.Position = initialStreamPosition;

            ret = (long[]) offsetList.ToArray(typeof(long));

            return ret;
        }

        /// <summary>
        ///     Search for bytes string within file offset [startingOffset - maxOffset].
        /// </summary>
        /// <param name="stream">Stream to search for bytes.</param>
        /// <param name="startingOffset">Offset to begin searching at.</param>
        /// <param name="maxOffset">Maximum offset to include in search.</param>
        /// <param name="searchBytes">Bytes to search for.</param>
        /// <param name="returnStreamToIncomingPosition">Return stream to incoming position.</param>
        /// <returns></returns>
        public static long GetNextOffsetWithLimit(Stream stream, long startingOffset,
            long maxOffset, byte[] searchBytes, bool returnStreamToIncomingPosition)
        {
            long initialStreamPosition = 0;

            if (returnStreamToIncomingPosition) initialStreamPosition = stream.Position;

            var itemFound = false;
            var absoluteOffset = startingOffset;
            long relativeOffset;
            var checkBytes = new byte[Constants.FileReadChunkSize];
            byte[] compareBytes;

            long ret = -1;

            while (!itemFound && absoluteOffset < maxOffset)
            {
                stream.Position = absoluteOffset;
                stream.Read(checkBytes, 0, Constants.FileReadChunkSize);
                relativeOffset = 0;

                while (!itemFound && relativeOffset < Constants.FileReadChunkSize)
                {
                    if (relativeOffset + searchBytes.Length < checkBytes.Length)
                    {
                        compareBytes = new byte[searchBytes.Length];
                        Array.Copy(checkBytes, relativeOffset, compareBytes, 0, searchBytes.Length);

                        if (CompareSegment(compareBytes, 0, searchBytes))
                        {
                            itemFound = true;
                            ret = absoluteOffset + relativeOffset;
                            break;
                        }
                    }

                    relativeOffset++;
                }

                absoluteOffset += Constants.FileReadChunkSize - searchBytes.Length;
            }

            // return stream to incoming position
            if (returnStreamToIncomingPosition) stream.Position = initialStreamPosition;

            return ret;
        }

        public static long GetNextOffsetWithLimitMasked(Stream stream, long startingOffset,
            long maxOffset, byte[] searchBytes, byte[] searchMask, bool returnStreamToIncomingPosition)
        {
            long initialStreamPosition = 0;

            if (returnStreamToIncomingPosition) initialStreamPosition = stream.Position;

            var itemFound = false;
            var absoluteOffset = startingOffset;
            long relativeOffset;
            var checkBytes = new byte[Constants.FileReadChunkSize];
            byte[] compareBytes;

            long ret = -1;

            while (!itemFound && absoluteOffset < maxOffset)
            {
                stream.Position = absoluteOffset;
                stream.Read(checkBytes, 0, Constants.FileReadChunkSize);
                relativeOffset = 0;

                while (!itemFound && relativeOffset < Constants.FileReadChunkSize)
                {
                    if (relativeOffset + searchBytes.Length < checkBytes.Length)
                    {
                        compareBytes = new byte[searchBytes.Length];
                        Array.Copy(checkBytes, relativeOffset, compareBytes, 0, searchBytes.Length);
                        for (var idx = 0; idx < searchMask.Length; ++idx) compareBytes[idx] &= searchMask[idx];

                        if (CompareSegment(compareBytes, 0, searchBytes))
                        {
                            itemFound = true;
                            ret = absoluteOffset + relativeOffset;
                            break;
                        }
                    }

                    relativeOffset++;
                }

                absoluteOffset += Constants.FileReadChunkSize - searchBytes.Length;
            }

            // return stream to incoming position
            if (returnStreamToIncomingPosition) stream.Position = initialStreamPosition;

            return ret;
        }

        /// <summary>
        ///     Get the offset of the first instance of pSearchBytes after the input offset.
        /// </summary>
        /// <param name="bufferToSearch">Byte array to search.</param>
        /// <param name="offset">Offset to begin searching from.</param>
        /// <param name="searchValue">Bytes to search for.</param>
        /// <returns>Returns the offset of the first instance of pSearchBytes after the input offset or -1 otherwise.</returns>
        public static long GetNextOffset(byte[] bufferToSearch, long offset, byte[] searchValue)
        {
            var itemFound = false;
            var absoluteOffset = offset;
            byte[] compareBytes;

            long ret = -1;

            while (!itemFound && absoluteOffset < bufferToSearch.Length - searchValue.Length)
            {
                compareBytes = new byte[searchValue.Length];
                Array.Copy(bufferToSearch, absoluteOffset, compareBytes, 0, searchValue.Length);

                if (CompareSegment(compareBytes, 0, searchValue))
                {
                    itemFound = true;
                    ret = absoluteOffset;
                    break;
                }

                absoluteOffset++;
            }

            return ret;
        }

        /// <summary>
        ///     Get the offset of the first instance of pSearchBytes before the input offset.
        /// </summary>
        /// <param name="stream">Stream to search.</param>
        /// <param name="offset">Offset to begin searching from.</param>
        /// <param name="searchBytes">Bytes to search for.</param>
        /// <returns>Returns the offset of the first instance of pSearchBytes before the input offset or -1 otherwise.</returns>
        public static long GetPreviousOffset(Stream stream, long offset, byte[] searchBytes)
        {
            var initialStreamPosition = stream.Position;

            var itemFound = false;
            long relativeOffset;
            var checkBytes = new byte[Constants.FileReadChunkSize];
            byte[] compareBytes;

            long ret = -1;

            var absoluteOffset = offset - Constants.FileReadChunkSize + searchBytes.Length;
            while (!itemFound && absoluteOffset > -1)
            {
                stream.Position = absoluteOffset;
                relativeOffset = stream.Read(checkBytes, 0, Constants.FileReadChunkSize);

                while (!itemFound && relativeOffset > -1)
                {
                    if (relativeOffset + searchBytes.Length <= checkBytes.Length)
                    {
                        compareBytes = new byte[searchBytes.Length];
                        Array.Copy(checkBytes, relativeOffset, compareBytes, 0, searchBytes.Length);

                        if (CompareSegment(compareBytes, 0, searchBytes))
                        {
                            itemFound = true;
                            ret = absoluteOffset + relativeOffset;

                            if (ret == offset) ret -= searchBytes.Length;

                            break;
                        }
                    }

                    relativeOffset--;
                }

                absoluteOffset = absoluteOffset - Constants.FileReadChunkSize + searchBytes.Length;
            }

            // return stream to incoming position
            stream.Position = initialStreamPosition;

            return ret;
        }

        /// <summary>
        ///     Compare bytes at input offset to target bytes.
        /// </summary>
        /// <param name="sourceArray">Bytes to compare.</param>
        /// <param name="offset">Offset to begin comparison of pBytes to pTarget.</param>
        /// <param name="target">Target bytes to compare.</param>
        /// <returns>True if the bytes at pOffset match the pTarget bytes.</returns>
        public static bool CompareSegment(byte[] sourceArray, int offset, byte[] target)
        {
            if (sourceArray.Length <= 0)
                return false;

            var sourceSpan = new ReadOnlySpan<byte>(sourceArray, offset, target.Length);
            var targetSpan = new ReadOnlySpan<byte>(target, 0, target.Length);
            return sourceSpan.SequenceEqual(targetSpan);
        }

        /// <summary>
        ///     Compare bytes at input offset to target bytes.
        /// </summary>
        /// <param name="sourceArray">Bytes to compare.</param>
        /// <param name="offset">Offset to begin comparison of pBytes to pTarget.</param>
        /// <param name="target">Target bytes to compare.</param>
        /// <returns>True if the bytes at pOffset match the pTarget bytes.</returns>
        public static bool CompareSegment(ReadOnlySpan<byte> sourceArray, int offset, ReadOnlySpan<byte> target)
        {
            if (sourceArray.Length <= 0)
                return false;

            //var sourceSpan = new ReadOnlySpan<byte>(sourceArray, offset, target.Length);
            var sourceSpan = sourceArray.Slice(offset, target.Length);
            //var targetSpan = new ReadOnlySpan<byte>(target, 0, target.Length);
            return sourceSpan.SequenceEqual(target);
        }

        //var sourceSpan = new ReadOnlySpan<byte>(sourceArray, offset, target.Length - offset);

        /// <summary>
        ///     Compare bytes at input offset to target bytes.
        /// </summary>
        /// <param name="sourceArray">Bytes to compare.</param>
        /// <param name="offset">Offset to begin comparison of pBytes to pTarget.</param>
        /// <param name="target">Target bytes to compare.</param>
        /// <returns>True if the bytes at pOffset match the pTarget bytes.</returns>
        public static bool CompareSegment(byte[] sourceArray, long offset, byte[] target)
        {
            var ret = true;
            uint j = 0;
            for (var i = offset; i < target.Length; i++)
            {
                if (sourceArray[i] != target[j])
                {
                    ret = false;
                    break;
                }

                j++;
            }

            return ret;
        }

        public static bool CompareSegmentUsingSourceOffset(byte[] sourceArray, int sourceOffset,
            int numBytesToCompare, byte[] target)
        {
            var ret = true;
            uint j = 0;

            if (sourceArray.Length > 0)
                for (var i = sourceOffset;
                    i < sourceOffset + target.Length || i < sourceOffset + numBytesToCompare;
                    i++)
                {
                    if (sourceArray[i] != target[j])
                    {
                        ret = false;
                        break;
                    }

                    j++;
                }
            else
                ret = false;

            return ret;
        }

        public static void ExtractChunkToFile(Stream stream, long startingOffset, long length,
            string filePath)
        {
            ExtractChunkToFile(stream, startingOffset, length, filePath, false, false);
        }

        public static void ExtractChunkToFile(Stream stream, long startingOffset, long length,
            string filePath, bool outputLogFile)
        {
            ExtractChunkToFile(stream, startingOffset, length, filePath, outputLogFile, false);
        }

        /// <summary>
        ///     Extracts a section of the incoming stream to a file.
        /// </summary>
        /// <param name="stream">Stream to extract from.</param>
        /// <param name="startingOffset">Offset to begin the cut.</param>
        /// <param name="length">Number of bytes to cut.</param>
        /// <param name="filePath">File path to output the extracted chunk to.</param>
        public static void ExtractChunkToFile(
            Stream stream,
            long startingOffset,
            long length,
            string filePath,
            bool outputLogFile,
            bool outputSnakebiteBatchFile)
        {
            var makeBatchFile = outputSnakebiteBatchFile && stream is FileStream;
            var logInfo = new StringBuilder();
            var snakeBiteBatch = new StringBuilder();
            BinaryWriter bw = null;
            var fullFilePath = Path.GetFullPath(filePath);
            var fullOutputDirectory = Path.GetDirectoryName(fullFilePath);

            // create output folder if needed
            if (!Directory.Exists(fullOutputDirectory))
            {
                Directory.CreateDirectory(fullOutputDirectory);

                if (outputLogFile) logInfo.AppendLine("Created Directory: " + fullOutputDirectory);
            }

            // check if file exists and change name as needed
            if (File.Exists(fullFilePath))
            {
                var fileCount = Directory.GetFiles(fullOutputDirectory,
                    Path.GetFileNameWithoutExtension(fullFilePath) + "*" + Path.GetExtension(fullFilePath),
                    SearchOption.TopDirectoryOnly).Length;
                fullFilePath = Path.Combine(fullOutputDirectory,
                    string.Format("{0}_{1}{2}", Path.GetFileNameWithoutExtension(fullFilePath),
                        fileCount.ToString("X3"), Path.GetExtension(fullFilePath)));
            }

            try
            {
                bw = new BinaryWriter(File.Open(fullFilePath, FileMode.Create, FileAccess.Write));

                var read = 0;
                long totalBytes = 0;
                var bytes = new byte[Constants.FileReadChunkSize];
                stream.Seek(startingOffset, SeekOrigin.Begin);

                var maxread = length > (long) bytes.Length ? bytes.Length : (int) length;

                while ((read = stream.Read(bytes, 0, maxread)) > 0)
                {
                    bw.Write(bytes, 0, read);
                    totalBytes += read;

                    maxread = length - totalBytes > (long) bytes.Length ? bytes.Length : (int) (length - totalBytes);
                }

                if (outputLogFile)
                {
                    logInfo.AppendLine(
                        string.Format("Extracted - Offset: 0x{0}    Length: 0x{1}    File: {2}",
                            startingOffset.ToString("X8"),
                            length.ToString("X8"),
                            Path.GetFileName(fullFilePath)));

                    using (var logWriter = new StreamWriter(Path.Combine(fullOutputDirectory, LogFileName), true))
                    {
                        logWriter.Write(logInfo.ToString());
                    }
                }

                if (makeBatchFile)
                {
                    snakeBiteBatch.AppendLine(
                        string.Format("snakebite.exe \"{0}\" \"{1}\" 0x{2} 0x{3}",
                            Path.GetFileName(((FileStream) stream).Name),
                            Path.GetFileNameWithoutExtension(((FileStream) stream).Name) + Path.DirectorySeparatorChar +
                            Path.GetFileName(fullFilePath),
                            startingOffset.ToString("X8"),
                            (startingOffset + length - 1).ToString("X8")));

                    using (var batchWriter =
                        new StreamWriter(Path.Combine(fullOutputDirectory, SnakeBiteBatchFileName), true))
                    {
                        batchWriter.Write(snakeBiteBatch.ToString());
                    }
                }
            }
            finally
            {
                if (bw != null) bw.Close();
            }
        }

        public static void ExtractChunkToFile64(
            Stream stream,
            ulong startingOffset,
            ulong length,
            string filePath,
            bool outputLogFile,
            bool outputSnakebiteBatchFile)
        {
            var makeBatchFile = outputSnakebiteBatchFile && stream is FileStream;
            var logInfo = new StringBuilder();
            var snakeBiteBatch = new StringBuilder();
            BinaryWriter bw = null;
            var fullFilePath = Path.GetFullPath(filePath);
            var fullOutputDirectory = Path.GetDirectoryName(fullFilePath);

            // create output folder if needed
            if (!Directory.Exists(fullOutputDirectory))
            {
                Directory.CreateDirectory(fullOutputDirectory);

                if (outputLogFile) logInfo.AppendLine("Created Directory: " + fullOutputDirectory);
            }

            // check if file exists and change name as needed
            if (File.Exists(fullFilePath))
            {
                var fileCount = Directory.GetFiles(fullOutputDirectory, Path.GetFileName(fullFilePath) + "*",
                    SearchOption.TopDirectoryOnly).Length;
                fullFilePath = Path.Combine(fullOutputDirectory,
                    string.Format("{0}_{1}{2}", Path.GetFileNameWithoutExtension(fullFilePath),
                        fileCount.ToString("X3"), Path.GetExtension(fullFilePath)));
            }

            try
            {
                bw = new BinaryWriter(File.Open(fullFilePath, FileMode.Create, FileAccess.Write));

                var read = 0;
                ulong totalBytes = 0;
                var bytes = new byte[Constants.FileReadChunkSize];

                // reset location
                stream.Seek(0, SeekOrigin.Begin);

                // move stream pointer in long size chunks
                while (startingOffset > long.MaxValue) //
                {
                    stream.Seek(long.MaxValue, SeekOrigin.Current);
                    startingOffset -= long.MaxValue;
                }

                // less than a long now, should be ok
                stream.Seek((long) startingOffset, SeekOrigin.Current);

                var maxread = length > (ulong) bytes.Length ? bytes.Length : (int) length;

                while ((read = stream.Read(bytes, 0, maxread)) > 0)
                {
                    bw.Write(bytes, 0, read);
                    totalBytes += (ulong) read;

                    maxread = length - totalBytes > (ulong) bytes.Length ? bytes.Length : (int) (length - totalBytes);
                }

                if (outputLogFile)
                {
                    logInfo.AppendLine(
                        string.Format("Extracted - Offset: 0x{0}    Length: 0x{1}    File: {2}",
                            startingOffset.ToString("X8"),
                            length.ToString("X8"),
                            Path.GetFileName(fullFilePath)));

                    using (var logWriter = new StreamWriter(Path.Combine(fullOutputDirectory, LogFileName), true))
                    {
                        logWriter.Write(logInfo.ToString());
                    }
                }

                if (makeBatchFile)
                {
                    snakeBiteBatch.AppendLine(
                        string.Format("snakebite.exe \"{0}\" \"{1}\" 0x{2} 0x{3}",
                            Path.GetFileName(((FileStream) stream).Name),
                            Path.GetFileNameWithoutExtension(((FileStream) stream).Name) + Path.DirectorySeparatorChar +
                            Path.GetFileName(fullFilePath),
                            startingOffset.ToString("X8"),
                            (startingOffset + length - 1).ToString("X8")));

                    using (var batchWriter =
                        new StreamWriter(Path.Combine(fullOutputDirectory, SnakeBiteBatchFileName), true))
                    {
                        batchWriter.Write(snakeBiteBatch.ToString());
                    }
                }
            }
            finally
            {
                if (bw != null) bw.Close();
            }
        }

        public static byte[] ExtractChunkToFile64ReturningHash(
            Stream stream,
            ulong startingOffset,
            ulong length,
            string filePath,
            HashAlgorithm checksumAlgorithm,
            bool outputLogFile,
            bool outputSnakebiteBatchFile)
        {
            byte[] checksumHash;
            var makeBatchFile = outputSnakebiteBatchFile && stream is FileStream;
            var logInfo = new StringBuilder();
            var snakeBiteBatch = new StringBuilder();
            //BinaryWriter bw = null;
            var fullFilePath = Path.GetFullPath(filePath);
            var fullOutputDirectory = Path.GetDirectoryName(fullFilePath);

            // create output folder if needed
            if (!Directory.Exists(fullOutputDirectory))
            {
                Directory.CreateDirectory(fullOutputDirectory);

                if (outputLogFile) logInfo.AppendLine("Created Directory: " + fullOutputDirectory);
            }

            // check if file exists and change name as needed
            if (File.Exists(fullFilePath))
            {
                var fileCount = Directory.GetFiles(fullOutputDirectory, Path.GetFileName(fullFilePath) + "*",
                    SearchOption.TopDirectoryOnly).Length;
                fullFilePath = Path.Combine(fullOutputDirectory,
                    string.Format("{0}_{1}{2}", Path.GetFileNameWithoutExtension(fullFilePath),
                        fileCount.ToString("X3"), Path.GetExtension(fullFilePath)));
            }

            using (var fs = File.Open(fullFilePath, FileMode.Create, FileAccess.Write))
            using (var hashStream = new CryptoStream(fs, checksumAlgorithm, CryptoStreamMode.Write))
            {
                var read = 0;
                ulong totalBytes = 0;
                var bytes = new byte[Constants.FileReadChunkSize];

                // reset location
                stream.Seek(0, SeekOrigin.Begin);

                // move stream pointer in long size chunks
                while (startingOffset > long.MaxValue) //
                {
                    stream.Seek(long.MaxValue, SeekOrigin.Current);
                    startingOffset -= long.MaxValue;
                }

                // less than a long now, should be ok
                stream.Seek((long) startingOffset, SeekOrigin.Current);

                var maxread = length > (ulong) bytes.Length ? bytes.Length : (int) length;

                while ((read = stream.Read(bytes, 0, maxread)) > 0)
                {
                    hashStream.Write(bytes, 0, read);
                    totalBytes += (ulong) read;

                    maxread = length - totalBytes > (ulong) bytes.Length ? bytes.Length : (int) (length - totalBytes);
                }

                hashStream.FlushFinalBlock();
                checksumHash = checksumAlgorithm.Hash;

                #region Log Files

                if (outputLogFile)
                {
                    logInfo.AppendLine(
                        string.Format("Extracted - Offset: 0x{0}    Length: 0x{1}    File: {2}",
                            startingOffset.ToString("X8"),
                            length.ToString("X8"),
                            Path.GetFileName(fullFilePath)));

                    using (var logWriter = new StreamWriter(Path.Combine(fullOutputDirectory, LogFileName), true))
                    {
                        logWriter.Write(logInfo.ToString());
                    }
                }

                if (makeBatchFile)
                {
                    snakeBiteBatch.AppendLine(
                        string.Format("snakebite.exe \"{0}\" \"{1}\" 0x{2} 0x{3}",
                            Path.GetFileName(((FileStream) stream).Name),
                            Path.GetFileNameWithoutExtension(((FileStream) stream).Name) + Path.DirectorySeparatorChar +
                            Path.GetFileName(fullFilePath),
                            startingOffset.ToString("X8"),
                            (startingOffset + length - 1).ToString("X8")));

                    using (var batchWriter =
                        new StreamWriter(Path.Combine(fullOutputDirectory, SnakeBiteBatchFileName), true))
                    {
                        batchWriter.Write(snakeBiteBatch.ToString());
                    }
                }

                #endregion

                return checksumHash;
            }
        }

        public static void AppendChunkToFile(
            Stream stream,
            long startingOffset,
            long length,
            string filePath)
        {
            BinaryWriter bw = null;
            var fullFilePath = Path.GetFullPath(filePath);
            var fullOutputDirectory = Path.GetDirectoryName(fullFilePath);

            // create directory if needed
            if (!Directory.Exists(fullOutputDirectory)) Directory.CreateDirectory(fullOutputDirectory);

            try
            {
                bw = new BinaryWriter(File.Open(fullFilePath, FileMode.Append, FileAccess.Write));

                var read = 0;
                long totalBytes = 0;
                var bytes = new byte[Constants.FileReadChunkSize];
                stream.Seek(startingOffset, SeekOrigin.Begin);

                var maxread = length > (long) bytes.Length ? bytes.Length : (int) length;

                while ((read = stream.Read(bytes, 0, maxread)) > 0)
                {
                    bw.Write(bytes, 0, read);
                    totalBytes += read;

                    maxread = length - totalBytes > (long) bytes.Length ? bytes.Length : (int) (length - totalBytes);
                }
            }
            finally
            {
                if (bw != null) bw.Close();
            }
        }


        /// <summary>
        ///     Convert the input bytes to a string containing the hex values.
        /// </summary>
        /// <param name="value">Bytes to convert to a string.</param>
        /// <returns>String of hex values that represent the incoming byte array.</returns>
        public static string ByteArrayToString(byte[] value)
        {
            var checksum = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (var i = 0; i < value.Length; i++)
                checksum.Append(value[i].ToString("X2", CultureInfo.InvariantCulture));

            return checksum.ToString();
        }

        /// <summary>
        ///     Find an offset and cut the file based on incoming criteria.
        /// </summary>
        /// <param name="sourcePath">Path of file to search.</param>
        /// <param name="searchCriteria">Struct containing search criteria.</param>
        /// <param name="messages">Output messages.</param>
        /// <returns>Directory that extracted files were output into.</returns>
        public static string FindOffsetAndCutFile(string sourcePath, FindOffsetStruct searchCriteria,
            out string messages, bool outputLog, bool outputBatchFile)
        {
            int i;
            var j = 0;
            byte[] searchBytes;
            byte[] terminatorBytes = null;
            var enc = Encoding.ASCII;

            long cutStart;
            long cutSize = 0;
            long cutSizeOffset;
            byte[] cutSizeBytes;
            long lengthMultiplier;
            long minimumCutSize = -1;

            long previousPosition;
            string outputFolder;
            string outputFile;
            var chunkCount = 0;

            long offset;
            long searchStringModuloDivisor = 0;
            long searchStringModuloResult = 0;
            long previousOffset;

            long terminatorOffset;
            long terminatorModuloDivisor = 0;
            long terminatorModuloResult = 0;

            bool skipCut;

            var ret = new StringBuilder();

            // create search bytes
            if (searchCriteria.TreatSearchStringAsHex)
            {
                searchBytes = ByteConversion.GetBytesFromHexString(searchCriteria.SearchString);

                // convert the search string to bytes
                for (i = 0; i < searchCriteria.SearchString.Length; i += 2)
                {
                    searchBytes[j] = BitConverter.GetBytes(short.Parse(searchCriteria.SearchString.Substring(i, 2),
                        NumberStyles.AllowHexSpecifier, CultureInfo.CurrentCulture))[0];
                    j++;
                }
            }
            else
            {
                searchBytes = enc.GetBytes(searchCriteria.SearchString);
            }

            // parse minimum cut size
            if (!string.IsNullOrEmpty(searchCriteria.MinimumSize))
                minimumCutSize = ByteConversion.GetLongValueFromString(searchCriteria.MinimumSize);

            // parse Search String modulo information
            if (searchCriteria.DoSearchStringModulo)
            {
                searchStringModuloDivisor =
                    ByteConversion.GetLongValueFromString(searchCriteria.SearchStringModuloDivisor);
                searchStringModuloResult =
                    ByteConversion.GetLongValueFromString(searchCriteria.SearchStringModuloResult);
            }

            // create terminator bytes
            j = 0;
            if (searchCriteria.TreatTerminatorStringAsHex)
                terminatorBytes = ByteConversion.GetBytesFromHexString(searchCriteria.TerminatorString);
            else if (!string.IsNullOrEmpty(searchCriteria.TerminatorString))
                terminatorBytes = enc.GetBytes(searchCriteria.TerminatorString);

            var fi = new FileInfo(sourcePath);

            using (var fs = File.Open(Path.GetFullPath(sourcePath), FileMode.Open, FileAccess.Read))
            {
                ret.AppendFormat("[{0}]", sourcePath);
                ret.Append(Environment.NewLine);

                // setup starting offset
                previousOffset =
                    string.IsNullOrEmpty(searchCriteria.StartingOffset)
                        ? 0
                        : ByteConversion.GetLongValueFromString(searchCriteria.StartingOffset);

                // build output folder path
                if (string.IsNullOrEmpty(searchCriteria.OutputFolder))
                    outputFolder = Path.GetFullPath(
                        Path.Combine(
                            Path.GetDirectoryName(sourcePath),
                            Path.GetFileNameWithoutExtension(sourcePath) + "_CUT"));
                else
                    outputFolder = searchCriteria.OutputFolder;

                // search for our string
                // while ((offset = ParseFile.GetNextOffset(fs, previousOffset, searchBytes)) != -1)
                while ((offset = GetNextOffset(fs, previousOffset, searchBytes,
                        searchCriteria.DoSearchStringModulo, searchStringModuloDivisor,
                        searchStringModuloResult)) != -1)
                    // do cut file tasks
                    if (searchCriteria.CutFile)
                    {
                        skipCut = false;

                        cutStart = offset - ByteConversion.GetLongValueFromString(searchCriteria.SearchStringOffset);

                        // determine cut size from value at offset
                        if (searchCriteria.IsCutSizeAnOffset)
                        {
                            cutSizeOffset = cutStart + ByteConversion.GetLongValueFromString(searchCriteria.CutSize);
                            previousPosition = fs.Position;
                            cutSizeBytes = ParseSimpleOffset(
                                fs,
                                cutSizeOffset,
                                (int) ByteConversion.GetLongValueFromString(searchCriteria.CutSizeOffsetSize));
                            fs.Position = previousPosition;

                            if (!searchCriteria.IsLittleEndian) Array.Reverse(cutSizeBytes);

                            switch (cutSizeBytes.Length)
                            {
                                case 1:
                                    cutSize = cutSizeBytes[0];
                                    break;
                                case 2:
                                    cutSize = BitConverter.ToInt16(cutSizeBytes, 0);
                                    break;
                                case 4:
                                    cutSize = BitConverter.ToInt32(cutSizeBytes, 0);
                                    break;
                                default:
                                    cutSize = 0;
                                    break;
                            }

                            if (searchCriteria.UseLengthMultiplier)
                            {
                                lengthMultiplier =
                                    ByteConversion.GetLongValueFromString(searchCriteria.LengthMultiplier);
                                cutSize *= lengthMultiplier;
                            }
                        }
                        else if (searchCriteria.UseTerminatorForCutSize) // look for terminator
                        {
                            if (cutStart >= 0)
                            {
                                if (searchCriteria.DoTerminatorModulo)
                                {
                                    terminatorModuloDivisor =
                                        ByteConversion.GetLongValueFromString(searchCriteria
                                            .TerminatorStringModuloDivisor);
                                    terminatorModuloResult =
                                        ByteConversion.GetLongValueFromString(searchCriteria
                                            .TerminatorStringModuloResult);
                                }

                                terminatorOffset = GetNextOffset(fs, offset + searchBytes.Length, terminatorBytes,
                                    searchCriteria.DoTerminatorModulo, terminatorModuloDivisor,
                                    terminatorModuloResult);

                                // cut to EOF if terminator not found
                                if (searchCriteria.CutToEofIfTerminatorNotFound && terminatorOffset == -1)
                                    cutSize = fs.Length - cutStart;
                                else
                                    cutSize = terminatorOffset - cutStart;

                                if (searchCriteria.IncludeTerminatorLength) cutSize += terminatorBytes.Length;
                            }
                        }
                        else // static size
                        {
                            cutSize = ByteConversion.GetLongValueFromString(searchCriteria.CutSize);
                        }

                        outputFile = string.Format(CultureInfo.InvariantCulture, "{0}_{1}{2}",
                            Path.GetFileNameWithoutExtension(sourcePath),
                            chunkCount.ToString("X8", CultureInfo.InvariantCulture),
                            searchCriteria.OutputFileExtension);

                        if (cutStart < 0)
                        {
                            ret.AppendFormat(
                                CultureInfo.CurrentCulture,
                                "  Warning: For string found at: 0x{0}, cut begin is less than 0 ({1})...Skipping",
                                offset.ToString("X8", CultureInfo.InvariantCulture),
                                cutStart.ToString("X8", CultureInfo.InvariantCulture));
                            ret.Append(Environment.NewLine);

                            skipCut = true;
                        }
                        else if (cutSize < 1)
                        {
                            ret.AppendFormat(
                                CultureInfo.CurrentCulture,
                                "  Warning: For string found at: 0x{0}, cut size is less than 1 ({1})...Skipping",
                                offset.ToString("X8", CultureInfo.InvariantCulture),
                                cutSize.ToString("X8", CultureInfo.InvariantCulture));
                            ret.Append(Environment.NewLine);

                            skipCut = true;
                        }
                        else if (cutStart + cutSize > fi.Length)
                        {
                            ret.AppendFormat(
                                CultureInfo.CurrentCulture,
                                "  Warning: For string found at: 0x{0}, total file end will go past the end of the file ({1})",
                                offset.ToString("X8", CultureInfo.InvariantCulture),
                                (cutStart + cutSize).ToString("X8", CultureInfo.InvariantCulture));
                            ret.Append(Environment.NewLine);
                        }

                        if (skipCut)
                        {
                            previousOffset = offset + 1;
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(searchCriteria.ExtraCutSizeBytes))
                                cutSize += ByteConversion.GetLongValueFromString(searchCriteria.ExtraCutSizeBytes);

                            // check minimum cut size
                            if (minimumCutSize > 0 && cutSize >= minimumCutSize ||
                                minimumCutSize < 1)
                            {
                                ExtractChunkToFile(fs, cutStart, cutSize, Path.Combine(outputFolder, outputFile),
                                    outputLog, outputBatchFile);

                                ret.AppendFormat(
                                    CultureInfo.CurrentCulture,
                                    "  Extracted [{3}] begining at 0x{0}, for string found at: 0x{1}, with size 0x{2}",
                                    cutStart.ToString("X8", CultureInfo.InvariantCulture),
                                    offset.ToString("X8", CultureInfo.InvariantCulture),
                                    cutSize.ToString("X8", CultureInfo.InvariantCulture),
                                    outputFile);
                                ret.Append(Environment.NewLine);

                                chunkCount++;
                            }

                            previousOffset = cutStart + cutSize;
                        }
                    }
                    else // just output text
                    {
                        // just append the offset
                        ret.AppendFormat(CultureInfo.CurrentCulture, "  String found at: 0x{0}",
                            offset.ToString("X8", CultureInfo.InvariantCulture));
                        ret.Append(Environment.NewLine);

                        previousOffset = offset + searchBytes.Length;
                    }
            }

            messages = ret.ToString();
            return outputFolder;
        }

        public static long GetVaryingByteValueAtOffset(Stream inStream, string valueOffset, string valueLength,
            bool valueIsLittleEndian)
        {
            long newValueOffset;
            long newValueLength;

            newValueOffset = ByteConversion.GetLongValueFromString(valueOffset);
            newValueLength = ByteConversion.GetLongValueFromString(valueLength);

            return GetVaryingByteValueAtOffset(inStream, newValueOffset, newValueLength, valueIsLittleEndian);
        }

        public static long GetVaryingByteValueAtAbsoluteOffset(Stream inStream, OffsetDescription offsetInfo)
        {
            return GetVaryingByteValueAtAbsoluteOffset(inStream, offsetInfo, false);
        }

        public static long GetVaryingByteValueAtAbsoluteOffset(Stream inStream, OffsetDescription offsetInfo,
            bool allowNegativeOffset)
        {
            long newValueOffset;
            long newValueLength;

            newValueOffset = ByteConversion.GetLongValueFromString(offsetInfo.OffsetValue);
            newValueLength = ByteConversion.GetLongValueFromString(offsetInfo.OffsetSize);

            return GetVaryingByteValueAtOffset(inStream, newValueOffset, newValueLength,
                offsetInfo.OffsetByteOrder.Equals(Constants.LittleEndianByteOrder), allowNegativeOffset);
        }

        public static long GetVaryingByteValueAtRelativeOffset(Stream inStream, OffsetDescription offsetInfo,
            long currentOffset)
        {
            long newValueOffset;
            long newValueLength;

            newValueOffset = currentOffset + ByteConversion.GetLongValueFromString(offsetInfo.OffsetValue);
            newValueLength = ByteConversion.GetLongValueFromString(offsetInfo.OffsetSize);

            return GetVaryingByteValueAtOffset(inStream, newValueOffset, newValueLength,
                offsetInfo.OffsetByteOrder.Equals(Constants.LittleEndianByteOrder));
        }

        public static long GetVaryingByteValueAtOffset(Stream inStream, long valueOffset, long valueLength,
            bool valueIsLittleEndian)
        {
            return GetVaryingByteValueAtOffset(inStream, valueOffset, valueLength,
                valueIsLittleEndian, false);
        }

        public static long GetVaryingByteValueAtOffset(Stream inStream, long valueOffset, long valueLength,
            bool valueIsLittleEndian, bool allowNegativeOffset)
        {
            long newValue;

            if (allowNegativeOffset && valueOffset < 0) valueOffset = inStream.Length + valueOffset;

            var newValueBytes = ParseSimpleOffset(inStream, valueOffset, (int) valueLength);

            if (!valueIsLittleEndian) Array.Reverse(newValueBytes);

            switch (newValueBytes.Length)
            {
                case 1:
                    newValue = newValueBytes[0];
                    break;
                case 2:
                    newValue = BitConverter.ToUInt16(newValueBytes, 0);
                    break;
                case 4:
                    newValue = BitConverter.ToUInt32(newValueBytes, 0);
                    break;
                default:
                    newValue = -1;
                    break;
            }

            return newValue;
        }

        public static string RemoveLeadingPathSeparator(string pathToCheck)
        {
            char firstChar;
            var ret = pathToCheck;

            if (!string.IsNullOrEmpty(pathToCheck) && pathToCheck.Length > 0)
            {
                firstChar = pathToCheck.ToCharArray()[0];

                if (firstChar.Equals(Path.DirectorySeparatorChar) ||
                    firstChar.Equals(Path.AltDirectorySeparatorChar) ||
                    firstChar.Equals(Path.VolumeSeparatorChar))
                    ret = pathToCheck.Substring(1);
            }

            return ret;
        }

        public static string RemoveIllegalCharactersFromPath(string path)
        {
            var illegalFileNameChars = Path.GetInvalidFileNameChars();
            var illegalPathChars = Path.GetInvalidPathChars();

            foreach (var c in path)
            {
                for (var i = 0; i < illegalFileNameChars.Length; i++)
                    if (c == illegalFileNameChars[i])
                        path.Replace(illegalFileNameChars[i], '_');

                for (var i = 0; i < illegalPathChars.Length; i++)
                    if (c == illegalPathChars[i])
                        path.Replace(illegalPathChars[i], '_');
            }

            return path.Trim();
        }

        public static string TrimFileNameToNullBytes(string path)
        {
            byte[] stringBytes;
            var nameLength = path.Length;

            stringBytes = Encoding.ASCII.GetBytes(path);

            for (var i = 0; i < stringBytes.Length; i++)
                if (stringBytes[i] == 0)
                {
                    nameLength = i;
                    break;
                }

            path = path.Substring(0, nameLength);

            return path;
        }

        public static byte[] SimpleArrayCopy(byte[] SourceArray, long offset, long length)
        {
            var value = new byte[length];
            Array.Copy(SourceArray, offset, value, 0, length);
            return value;
        }

        public static float ReadFloatLE(Stream inStream, long offset)
        {
            return BitConverter.ToSingle(ParseSimpleOffset(inStream, offset, 4), 0);
        }

        public static float ReadFloatBE(Stream inStream, long offset)
        {
            var val = ParseSimpleOffset(inStream, offset, 4);
            Array.Reverse(val);

            return BitConverter.ToSingle(val, 0);
        }

        public static uint ReadUintLE(Stream inStream, long offset)
        {
            return BitConverter.ToUInt32(ParseSimpleOffset(inStream, offset, 4), 0);
        }

        public static uint ReadUintLE(byte[] inBytes, long offset)
        {
            var val = new byte[4];

            Array.Copy(inBytes, offset, val, 0, 4);

            return BitConverter.ToUInt32(val, 0);
        }

        public static uint ReadUintBE(Stream inStream, long offset)
        {
            var val = ParseSimpleOffset(inStream, offset, 4);
            Array.Reverse(val);

            return BitConverter.ToUInt32(val, 0);
        }

        public static uint ReadUintBE(byte[] inBytes, long offset)
        {
            var val = new byte[4];

            Array.Copy(inBytes, offset, val, 0, 4);
            Array.Reverse(val);

            return BitConverter.ToUInt32(val, 0);
        }

        public static ushort ReadUshortLE(Stream inStream, long offset)
        {
            return BitConverter.ToUInt16(ParseSimpleOffset(inStream, offset, 2), 0);
        }

        public static ushort ReadUshortLE(byte[] inBytes, long offset)
        {
            var val = new byte[2];

            Array.Copy(inBytes, offset, val, 0, 2);

            return BitConverter.ToUInt16(val, 0);
        }

        public static ushort ReadUshortBE(Stream inStream, long offset)
        {
            var val = ParseSimpleOffset(inStream, offset, 2);
            Array.Reverse(val);

            return BitConverter.ToUInt16(val, 0);
        }

        public static ushort ReadUshortBE(byte[] inBytes, long offset)
        {
            var val = new byte[2];

            Array.Copy(inBytes, offset, val, 0, 2);
            Array.Reverse(val);

            return BitConverter.ToUInt16(val, 0);
        }

        public static byte ReadByte(Stream inStream, long offset)
        {
            return ParseSimpleOffset(inStream, offset, 1)[0];
        }

        public static sbyte ReadSByte(Stream inStream, long offset)
        {
            return (sbyte) ParseSimpleOffset(inStream, offset, 1)[0];
        }

        public static int ReadInt32LE(Stream inStream, long offset)
        {
            return BitConverter.ToInt32(ParseSimpleOffset(inStream, offset, 4), 0);
        }

        public static int ReadInt32BE(Stream inStream, long offset)
        {
            var val = ParseSimpleOffset(inStream, offset, 4);
            Array.Reverse(val);

            return BitConverter.ToInt32(val, 0);
        }

        public static short ReadInt16LE(Stream inStream, long offset)
        {
            return BitConverter.ToInt16(ParseSimpleOffset(inStream, offset, 2), 0);
        }

        public static short ReadInt16BE(Stream inStream, long offset)
        {
            var val = ParseSimpleOffset(inStream, offset, 2);
            Array.Reverse(val);

            return BitConverter.ToInt16(val, 0);
        }

        public static int ReadInt24LE(Stream inStream, long offset)
        {
            var val = ParseSimpleOffset(inStream, offset, 4);
            val[3] = 0;

            return BitConverter.ToInt32(val, 0);
        }

        public static int ReadInt24BE(Stream inStream, long offset)
        {
            var val = ParseSimpleOffset(inStream, offset, 4);
            val[3] = 0;
            Array.Reverse(val);

            return BitConverter.ToInt32(val, 0) >> 8;
        }

        public static uint ReadUint24LE(Stream inStream, long offset)
        {
            var val = ParseSimpleOffset(inStream, offset, 4);
            val[3] = 0;

            return BitConverter.ToUInt32(val, 0);
        }

        public static uint ReadUint24BE(Stream inStream, long offset)
        {
            var val = ParseSimpleOffset(inStream, offset, 4);
            val[3] = 0;
            Array.Reverse(val);

            return BitConverter.ToUInt32(val, 0) >> 8;
        }

        public static ulong ReadUlongLE(Stream inStream, long offset)
        {
            return BitConverter.ToUInt64(ParseSimpleOffset(inStream, offset, 8), 0);
        }

        public static ulong ReadUlongLE(byte[] inBytes, long offset)
        {
            var val = new byte[8];

            Array.Copy(inBytes, offset, val, 0, 8);

            return BitConverter.ToUInt64(val, 0);
        }

        public static ulong ReadUlongBE(Stream inStream, long offset)
        {
            var val = ParseSimpleOffset(inStream, offset, 8);
            Array.Reverse(val);

            return BitConverter.ToUInt64(val, 0);
        }

        public static string ReadAsciiString(Stream inStream, long offset)
        {
            var streamLength = inStream.Length;
            var stringLength = 0;
            int codePage;

            int dummy;
            byte[] stringBytes;
            var ret = string.Empty;

            // move pointer
            inStream.Position = offset;

            // read until NULL
            for (var i = offset; i <= streamLength; i++)
            {
                dummy = inStream.ReadByte();

                if (dummy > 0)
                    stringLength++;
                else if (dummy <= 0) break;
            }

            // parse string
            stringBytes = ParseSimpleOffset(inStream, offset, stringLength);
            codePage = ByteConversion.GetPredictedCodePageForTags(stringBytes);
            ret = ByteConversion.GetEncodedText(stringBytes, codePage);
            //ret = ByteConversion.GetAsciiText(stringBytes);

            return ret;
        }
    }
}