using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace VGMToolbox.util
{
    public static class FileUtil
    {
        /// <summary>
        ///     Reads data into a complete array, throwing an EndOfStreamException
        ///     if the stream runs out of data first, or if an IOException
        ///     naturally occurs.
        /// </summary>
        /// <param name="stream">The stream to read data from</param>
        /// <param name="data">
        ///     The array to read bytes into. The array
        ///     will be completely filled from the stream, so an appropriate
        ///     size must be given.
        /// </param>
        public static void ReadWholeArray(Stream stream, byte[] data)
        {
            ReadWholeArray(stream, data, data.Length);
        }

        public static void ReadWholeArray(Stream stream, byte[] data, int pLength)
        {
            var offset = 0;
            var remaining = pLength;
            while (remaining > 0)
            {
                var read = stream.Read(data, offset, remaining);
                if (read <= 0)
                    throw new EndOfStreamException(
                        string.Format(CultureInfo.CurrentCulture, "End of stream reached with {0} bytes left to read",
                            remaining));

                remaining -= read;
                offset += read;
            }
        }

        /// <summary>
        ///     Replaces 0x00 with 0x20 in an array of bytes.
        /// </summary>
        /// <param name="value">Array of bytes to alter.</param>
        /// <returns>Original array with 0x00 replaced by 0x20.</returns>
        public static byte[] ReplaceNullByteWithSpace(byte[] value)
        {
            for (var i = 0; i < value.Length; i++)
                if (value[i] == 0x00)
                    value[i] = 0x20;

            return value;
        }

        /// <summary>
        ///     Returns the count of files contained in the input directories and their subdirectories.
        /// </summary>
        /// <param name="paths">Paths to count files within.</param>
        /// <returns>Number of files in the incoming directories and their subdirectories.</returns>
        public static int GetFileCount(string[] paths)
        {
            return GetFileCount(paths, true);
        }

        public static int GetFileCount(string[] paths, bool includeSubdirs)
        {
            var totalFileCount = 0;

            foreach (var path in paths)
                if (File.Exists(path))
                {
                    totalFileCount++;
                }
                else if (Directory.Exists(path))
                {
                    if (includeSubdirs)
                        totalFileCount += Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).Length;
                    else
                        totalFileCount += Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly).Length;
                }

            return totalFileCount;
        }

        public static string CleanFileName(string pDirtyFileName)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) pDirtyFileName = pDirtyFileName.Replace(c, '_');

            return pDirtyFileName;
        }

        public static void UpdateTextField(
            string pFilePath,
            string pFieldValue,
            int pOffset,
            int pMaxLength)
        {
            var enc = Encoding.ASCII;

            using (var bw =
                new BinaryWriter(File.Open(pFilePath, FileMode.Open, FileAccess.ReadWrite)))
            {
                var newBytes = new byte[pMaxLength];
                var convertedBytes = enc.GetBytes(pFieldValue);

                var numBytesToCopy =
                    convertedBytes.Length <= pMaxLength ? convertedBytes.Length : pMaxLength;
                Array.ConstrainedCopy(convertedBytes, 0, newBytes, 0, numBytesToCopy);

                bw.Seek(pOffset, SeekOrigin.Begin);
                bw.Write(newBytes);
            }
        }

        public static void UpdateChunk(
            string pFilePath,
            int pOffset,
            byte[] value)
        {
            using (var bw =
                new BinaryWriter(File.Open(pFilePath, FileMode.Open, FileAccess.ReadWrite)))
            {
                bw.Seek(pOffset, SeekOrigin.Begin);
                bw.Write(value);
            }
        }

        public static void ReplaceFileChunk(
            string pSourceFilePath,
            long pSourceOffset,
            long pLength,
            string pDestinationFilePath,
            long pDestinationOffset)
        {
            var read = 0;
            long maxread;
            var totalBytes = 0;
            var bytes = new byte[Constants.FileReadChunkSize];

            using (var bw =
                new BinaryWriter(File.Open(pDestinationFilePath, FileMode.Open, FileAccess.ReadWrite)))
            {
                using (var br =
                    new BinaryReader(File.Open(pSourceFilePath, FileMode.Open, FileAccess.Read)))
                {
                    br.BaseStream.Position = pSourceOffset;
                    bw.BaseStream.Position = pDestinationOffset;

                    maxread = pLength > bytes.Length ? bytes.Length : pLength;

                    while ((read = br.Read(bytes, 0, (int) maxread)) > 0)
                    {
                        bw.Write(bytes, 0, read);
                        totalBytes += read;

                        maxread = pLength - totalBytes > bytes.Length ? bytes.Length : pLength - totalBytes;
                    }
                }
            }
        }

        public static void ZeroOutFileChunk(string pPath, long pOffset, int pLength)
        {
            var bytesToWrite = pLength;
            byte[] bytes;

            var maxWrite = bytesToWrite > Constants.FileReadChunkSize ? Constants.FileReadChunkSize : bytesToWrite;

            using (var bw =
                new BinaryWriter(File.Open(pPath, FileMode.Open, FileAccess.Write)))
            {
                bw.BaseStream.Position = pOffset;

                while (bytesToWrite > 0)
                {
                    bytes = new byte[maxWrite];
                    bw.Write(bytes);
                    bytesToWrite -= maxWrite;
                    maxWrite = bytesToWrite > bytes.Length ? bytes.Length : bytesToWrite;
                }
            }
        }

        public static void TrimFileToLength(string path, long totalLength)
        {
            var fullPath = Path.GetFullPath(path);

            if (File.Exists(fullPath))
            {
                var destinationPath = Path.ChangeExtension(fullPath, ".trimmed");

                using (var fs = File.OpenRead(path))
                {
                    ParseFile.ExtractChunkToFile(fs, 0, totalLength, destinationPath);
                }

                File.Copy(destinationPath, path, true);
                File.Delete(destinationPath);
            }
        }

        public static string RemoveChunkFromFile(string path, long startingOffset, long length)
        {
            var fullPath = Path.GetFullPath(path);

            int bytesRead;
            var bytes = new byte[1024];

            var ret = string.Empty;

            if (File.Exists(fullPath))
            {
                var destinationPath = Path.ChangeExtension(fullPath, ".cut");

                using (var sourceFs = File.OpenRead(fullPath))
                {
                    // extract initial chunk
                    ParseFile.ExtractChunkToFile(sourceFs, 0, startingOffset, destinationPath);

                    // append remainder
                    using (var outFs = File.Open(destinationPath, FileMode.Append, FileAccess.Write))
                    {
                        sourceFs.Position = startingOffset + length;
                        bytesRead = sourceFs.Read(bytes, 0, bytes.Length);

                        while (bytesRead > 0)
                        {
                            outFs.Write(bytes, 0, bytesRead);
                            bytesRead = sourceFs.Read(bytes, 0, bytes.Length);
                        }
                    }

                    ret = destinationPath;
                }
            }

            return ret;
        }

        public static string RemoveAllChunksFromFile(FileStream fs, byte[] chunkToRemove)
        {
            int bytesRead;

            long currentReadOffset = 0;
            long totalBytesRead = 0;
            var maxReadSize = 0;
            long currentChunkSize;

            var bytes = new byte[Constants.FileReadChunkSize];
            var offsets = ParseFile.GetAllOffsets(fs, 0, chunkToRemove, false, -1, -1, true);

            var destinationPath = Path.ChangeExtension(fs.Name, ".cut");

            using (var destinationFs = File.OpenWrite(destinationPath))
            {
                for (var i = 0; i < offsets.Length; i++)
                {
                    // move position
                    fs.Position = currentReadOffset;

                    // get length of current size to write
                    currentChunkSize = offsets[i] - currentReadOffset;

                    // calculcate max cut size for this loop iteration
                    maxReadSize = currentChunkSize - totalBytesRead > (long) bytes.Length
                        ? bytes.Length
                        : (int) (currentChunkSize - totalBytesRead);

                    while ((bytesRead = fs.Read(bytes, 0, maxReadSize)) > 0)
                    {
                        destinationFs.Write(bytes, 0, bytesRead);
                        totalBytesRead += bytesRead;

                        maxReadSize = currentChunkSize - totalBytesRead > (long) bytes.Length
                            ? bytes.Length
                            : (int) (currentChunkSize - totalBytesRead);
                    }

                    totalBytesRead = 0;
                    currentReadOffset = offsets[i] + chunkToRemove.Length;
                }

                ////////////////////////////
                // write remainder of file
                ////////////////////////////
                // move position
                fs.Position = currentReadOffset;

                // get length of current size to write
                currentChunkSize = fs.Length - currentReadOffset;

                // calculcate max cut size
                maxReadSize = currentChunkSize - totalBytesRead > (long) bytes.Length
                    ? bytes.Length
                    : (int) (currentChunkSize - totalBytesRead);

                while ((bytesRead = fs.Read(bytes, 0, maxReadSize)) > 0)
                {
                    destinationFs.Write(bytes, 0, bytesRead);
                    totalBytesRead += bytesRead;

                    maxReadSize = currentChunkSize - totalBytesRead > (long) bytes.Length
                        ? bytes.Length
                        : (int) (currentChunkSize - totalBytesRead);
                }
            }

            return destinationPath;
        }

        public static string RemoveAllChunksFromFile(string path, byte[] chunkToRemove)
        {
            string destinationPath;

            using (var fs = File.OpenRead(path))
            {
                destinationPath = RemoveAllChunksFromFile(fs, chunkToRemove);
            }

            return destinationPath;
        }

        public static bool ExecuteExternalProgram(
            string pathToExecuatable,
            string arguments,
            string workingDirectory,
            out string standardOut,
            out string standardError)
        {
            Process externalExecutable;
            var isSuccess = false;

            standardOut = string.Empty;
            standardError = string.Empty;

            using (externalExecutable = new Process())
            {
                externalExecutable.StartInfo = new ProcessStartInfo(pathToExecuatable, arguments);
                externalExecutable.StartInfo.WorkingDirectory = workingDirectory;
                externalExecutable.StartInfo.UseShellExecute = false;
                externalExecutable.StartInfo.CreateNoWindow = true;

                externalExecutable.StartInfo.RedirectStandardOutput = true;
                externalExecutable.StartInfo.RedirectStandardError = true;
                isSuccess = externalExecutable.Start();

                standardOut = externalExecutable.StandardOutput.ReadToEnd();
                standardError = externalExecutable.StandardError.ReadToEnd();
                externalExecutable.WaitForExit();
            }

            return isSuccess;
        }

        public static void AddHeaderToFile(byte[] headerBytes, string sourceFile, string destinationFile)
        {
            int bytesRead;
            var readBuffer = new byte[Constants.FileReadChunkSize];

            using (var destinationStream = File.Open(destinationFile, FileMode.CreateNew, FileAccess.Write))
            {
                // write header
                destinationStream.Write(headerBytes, 0, headerBytes.Length);

                // write the source file
                using (var sourceStream = File.Open(sourceFile, FileMode.Open, FileAccess.Read))
                {
                    while ((bytesRead = sourceStream.Read(readBuffer, 0, readBuffer.Length)) > 0)
                        destinationStream.Write(readBuffer, 0, bytesRead);
                }
            }
        }

        public static void RenameFileUsingInternalName(string path,
            long offset, int length, byte[] terminatorBytes, bool maintainFileExtension)
        {
            var destinationDirectory = Path.GetDirectoryName(path);
            string destinationFile;
            string originalExtension;

            int nameLength;
            byte[] nameByteArray;

            using (var fs = File.OpenRead(path))
            {
                if (terminatorBytes != null)
                    nameLength = ParseFile.GetSegmentLength(fs, (int) offset, terminatorBytes);
                else
                    nameLength = length;

                if (nameLength < 1) throw new ArgumentOutOfRangeException("Name Length", "Name Length is less than 1.");

                if (maintainFileExtension) originalExtension = Path.GetExtension(path);

                nameByteArray = ParseFile.ParseSimpleOffset(fs, offset, nameLength);
                destinationFile = ByteConversion.GetAsciiText(ReplaceNullByteWithSpace(nameByteArray)).Trim();
                destinationFile = Path.Combine(destinationDirectory, destinationFile);

                if (maintainFileExtension)
                {
                    originalExtension = Path.GetExtension(path);
                    destinationFile = Path.ChangeExtension(destinationFile, originalExtension);
                }
            }

            // try to copy using the new name
            if (!path.Equals(destinationFile))
                try
                {
                    if (!Directory.Exists(Path.GetDirectoryName(destinationFile)))
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));

                    if (File.Exists(destinationFile))
                    {
                        var sameNamedFiles = Directory.GetFiles(Path.GetDirectoryName(destinationFile),
                            Path.GetFileNameWithoutExtension(destinationFile) + "*");

                        // rename to prevent overwrite
                        destinationFile = Path.Combine(Path.GetDirectoryName(destinationFile),
                            string.Format("{0}_{1}{2}", Path.GetFileNameWithoutExtension(destinationFile),
                                sameNamedFiles.Length.ToString("D4"), Path.GetExtension(destinationFile)));
                    }

                    File.Copy(path, destinationFile);
                    File.Delete(path);
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message, ex);
                }
        }

        public static void InterleaveFiles(string[] sourceFiles, uint interleaveValue,
            long startOffest, byte[] paddingBytes, string destinationFile)
        {
            long currentOffset = 0;
            long maxLength = 0;
            long sizeDifference = 0;
            long bytesToWrite;
            long bytesRemaining;

            var inputStreams = new FileStream[sourceFiles.Length];
            var fileLengths = new long[sourceFiles.Length];
            destinationFile = Path.GetFullPath(destinationFile);

            // open destination file for writing
            using (var destinationStream = File.OpenWrite(destinationFile))
            {
                try
                {
                    // build input streams array                   
                    for (var i = 0; i < sourceFiles.Length; i++)
                    {
                        inputStreams[i] = File.OpenRead(sourceFiles[i]);
                        fileLengths[i] = inputStreams[i].Length;

                        // get max file length
                        if (maxLength < fileLengths[i]) maxLength = fileLengths[i];
                    }

                    // write out blocks
                    currentOffset = startOffest;

                    while (currentOffset < maxLength)
                    {
                        for (var i = 0; i < sourceFiles.Length; i++)
                            if (currentOffset + interleaveValue < fileLengths[i])
                            {
                                // write from file
                                destinationStream.Write(
                                    ParseFile.ParseSimpleOffset(inputStreams[i], currentOffset, (int) interleaveValue),
                                    0, (int) interleaveValue);
                            }
                            else if (currentOffset < fileLengths[i])
                            {
                                // write some from file, some from padding
                                sizeDifference = currentOffset + interleaveValue - fileLengths[i];

                                // write from file
                                destinationStream.Write(
                                    ParseFile.ParseSimpleOffset(inputStreams[i], currentOffset,
                                        (int) (interleaveValue - sizeDifference)),
                                    0, (int) (interleaveValue - sizeDifference));

                                // write padding bytes
                                bytesRemaining = sizeDifference;

                                while (bytesRemaining > 0)
                                {
                                    bytesToWrite = bytesRemaining > paddingBytes.Length
                                        ? paddingBytes.Length
                                        : bytesRemaining;
                                    destinationStream.Write(paddingBytes, 0, (int) bytesToWrite);
                                    bytesRemaining -= bytesToWrite;
                                }
                            }
                            else
                            {
                                // write padding bytes
                                bytesRemaining = interleaveValue;

                                while (bytesRemaining > 0)
                                {
                                    bytesToWrite = bytesRemaining > paddingBytes.Length
                                        ? paddingBytes.Length
                                        : bytesRemaining;
                                    destinationStream.Write(paddingBytes, 0, (int) bytesToWrite);
                                    bytesRemaining -= bytesToWrite;
                                }
                            }

                        // increment offset
                        currentOffset += interleaveValue;
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message, ex);
                }
                finally
                {
                    // close all readers
                    for (var i = 0; i < inputStreams.Length; i++)
                        if (inputStreams[i].CanRead)
                        {
                            inputStreams[i].Close();
                            inputStreams[i].Dispose();
                        }
                }
            }
        }

        public static string GetNonDuplicateFileName(string destinationFile)
        {
            if (File.Exists(destinationFile))
            {
                var sameNamedFiles = Directory.GetFiles(Path.GetDirectoryName(destinationFile),
                    Path.GetFileNameWithoutExtension(destinationFile) + "*");

                // rename to prevent overwrite
                destinationFile = Path.Combine(Path.GetDirectoryName(destinationFile),
                    string.Format("{0}_{1}{2}", Path.GetFileNameWithoutExtension(destinationFile),
                        sameNamedFiles.Length.ToString("D4"), Path.GetExtension(destinationFile)));
            }

            return destinationFile;
        }

        public static string[] SplitFile(string sourceFile, long startingOffset, ulong chunkSizeInBytes,
            string outputFolder)
        {
            string[] outputFileList;
            string outputFileName;
            var outputFiles = new ArrayList();

            long fileLength;
            ulong currentOffset;

            int chunkCount;

            using (var sourceStream = File.OpenRead(sourceFile))
            {
                // get file length
                fileLength = sourceStream.Length;

                // init counters
                chunkCount = 1;
                currentOffset = (ulong) startingOffset;

                while (currentOffset < (ulong) fileLength)
                {
                    // construct output file name
                    outputFileName = Path.Combine(outputFolder,
                        string.Format("{0}.{1}",
                            Path.GetFileName(sourceFile),
                            chunkCount.ToString("D3")));

                    ParseFile.ExtractChunkToFile64(sourceStream, currentOffset, chunkSizeInBytes,
                        outputFileName, true, true);


                    outputFiles.Add(outputFileName);
                    currentOffset += chunkSizeInBytes;
                    chunkCount++;
                }
            }

            outputFileList = (string[]) outputFiles.ToArray(typeof(string));
            return outputFileList;
        }

        public static long? GetFileSize(string path)
        {
            long? fileSize;

            try
            {
                var fi = new FileInfo(Path.GetFullPath(path));
                fileSize = fi.Length;
            }
            catch (Exception)
            {
                fileSize = null;
            }

            return fileSize;
        }

        public static void CreateFileFromByteArray(string destinationFile, byte[] sourceBytes)
        {
            var destinationDirectory = Path.GetDirectoryName(destinationFile);

            if (!Directory.Exists(destinationDirectory)) Directory.CreateDirectory(destinationDirectory);

            using (var outStream = File.Open(destinationFile, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                outStream.Write(sourceBytes, 0, sourceBytes.Length);
            }
        }

        public static void CreateFileFromString(string destinationFile, string sourceText)
        {
            var destinationDirectory = Path.GetDirectoryName(destinationFile);

            if (!Directory.Exists(destinationDirectory)) Directory.CreateDirectory(destinationDirectory);

            using (var outStream = File.Open(destinationFile, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                using (var sw = new StreamWriter(outStream, Encoding.ASCII))
                {
                    sw.Write(sourceText);
                }
            }
        }

        public static string GetStringFromFileChunk(FileStream fs, ulong offset, ulong size)
        {
            var sb = new StringBuilder();

            var read = 0;
            ulong totalBytes = 0;
            var bytes = new byte[Constants.FileReadChunkSize];

            // reset location
            fs.Seek(0, SeekOrigin.Begin);

            // move stream pointer in long size chunks
            while (offset > long.MaxValue) //
            {
                fs.Seek(long.MaxValue, SeekOrigin.Current);
                offset -= long.MaxValue;
            }

            // less than a long now, should be ok
            fs.Seek((long) offset, SeekOrigin.Current);

            var maxread = size > (ulong) bytes.Length ? bytes.Length : (int) size;

            while ((read = fs.Read(bytes, 0, maxread)) > 0)
            {
                for (var i = 0; i < read; i++) sb.Append(bytes[i].ToString("X2"));

                totalBytes += (ulong) read;

                maxread = size - totalBytes > (ulong) bytes.Length ? bytes.Length : (int) (size - totalBytes);
            }

            return sb.ToString();
        }
    }
}