namespace VGMToolbox.util
{
    /// <summary>
    ///     Struct containing criteria used to find offsets.
    /// </summary>
    public struct FindOffsetStruct
    {
        public bool DoSearchStringModulo { set; get; }

        public string SearchStringModuloDivisor { set; get; }

        public string SearchStringModuloResult { set; get; }

        public bool DoTerminatorModulo { set; get; }

        public string TerminatorStringModuloDivisor { set; get; }

        public string TerminatorStringModuloResult { set; get; }

        public string MinimumSize { set; get; }

        public string SearchString { get; set; }

        /// <summary>
        ///     Gets or sets offset to being searching at
        /// </summary>
        public string StartingOffset { set; get; }

        /// <summary>
        ///     Gets or sets flag to indicate search string is a hex value.
        /// </summary>
        public bool TreatSearchStringAsHex { get; set; }

        /// <summary>
        ///     Gets or sets flag to cut the file when the offset is found.
        /// </summary>
        public bool CutFile { get; set; }

        /// <summary>
        ///     Gets or sets offset within destination file Search String would reside.
        /// </summary>
        public string SearchStringOffset { get; set; }

        /// <summary>
        ///     Gets or sets size to cut from file
        /// </summary>
        public string CutSize { get; set; }

        /// <summary>
        ///     Gets or sets size of offset holding cut size
        /// </summary>
        public string CutSizeOffsetSize { get; set; }

        /// <summary>
        ///     Gets or sets flag indicating that cut size is an offset.
        /// </summary>
        public bool IsCutSizeAnOffset { get; set; }

        /// <summary>
        ///     Gets or sets file extension to use for cut files.
        /// </summary>
        public string OutputFileExtension { get; set; }

        /// <summary>
        ///     Gets or sets flag indicating that offset based cut size is stored in Little Endian byte order.
        /// </summary>
        public bool IsLittleEndian { get; set; }

        /// <summary>
        ///     Gets or sets flag indicating that a terminator should be used to determine the cut size.
        /// </summary>

        public bool UseLengthMultiplier { set; get; }

        public string LengthMultiplier { set; get; }

        public bool UseTerminatorForCutSize { get; set; }

        /// <summary>
        ///     Gets or sets terminator string to search for.
        /// </summary>
        public string TerminatorString { get; set; }

        /// <summary>
        ///     Gets or sets flag indicating that Terminator String is hex.
        /// </summary>
        public bool TreatTerminatorStringAsHex { get; set; }

        /// <summary>
        ///     Gets or sets flag indicating that the length of the terminator should be included in the cut size.
        /// </summary>
        public bool IncludeTerminatorLength { get; set; }

        public bool CutToEofIfTerminatorNotFound { set; get; }

        /// <summary>
        ///     Gets or sets additional bytes to include in the cut size.
        /// </summary>
        public string ExtraCutSizeBytes { get; set; }

        public string OutputFolder { get; set; }
    }

    /// <summary>
    ///     Struct used to send messages conveying progress.
    /// </summary>
    public struct ProgressStruct
    {
        /// <summary>
        ///     File name to display in progress bar.
        /// </summary>
        private string fileName;

        /// <summary>
        ///     Error message to display in output window.
        /// </summary>
        private string errorMessage;

        /// <summary>
        ///     Generic message to display in output window.
        /// </summary>
        private string genericMessage;

        /// <summary>
        ///     Gets or sets fileName.
        /// </summary>
        public string FileName
        {
            get => fileName;
            set => fileName = value;
        }

        /// <summary>
        ///     Gets or sets errorMessage.
        /// </summary>
        public string ErrorMessage
        {
            get => errorMessage;
            set => errorMessage = value;
        }

        /// <summary>
        ///     Gets or sets genericMessage.
        /// </summary>
        public string GenericMessage
        {
            get => genericMessage;
            set => genericMessage = value;
        }

        /// <summary>
        ///     Reset this node's values
        /// </summary>
        public void Clear()
        {
            fileName = string.Empty;
            errorMessage = string.Empty;
            genericMessage = string.Empty;
        }
    }

    /// <summary>
    ///     Struct used to allow TreeView to select a specific form and modify the originating node upon completion of a task.
    /// </summary>
    public struct NodeTagStruct
    {
        /// <summary>
        ///     Class name of the Form this node will bring to focus.
        /// </summary>
        private string formClass;

        /// <summary>
        ///     Object type this node represents.
        /// </summary>
        private string objectType;

        /// <summary>
        ///     File path of the file this node represents.
        /// </summary>
        private string filePath;

        /// <summary>
        ///     Gets or sets formClass
        /// </summary>
        public string FormClass
        {
            get => formClass;
            set => formClass = value;
        }

        /// <summary>
        ///     Gets or sets objectType
        /// </summary>
        public string ObjectType
        {
            get => objectType;
            set => objectType = value;
        }

        /// <summary>
        ///     Gets or sets filePath
        /// </summary>
        public string FilePath
        {
            get => filePath;
            set => filePath = value;
        }
    }

    public enum VfsFileRecordRelativeOffsetLocationType
    {
        FileRecordStart,
        FileRecordEnd
    }

    public struct VfsExtractionStruct
    {
        // header size
        public bool UseStaticHeaderSize { set; get; }
        public string StaticHeaderSize { set; get; }
        public bool UseHeaderSizeOffset { set; get; }
        public OffsetDescription HeaderSizeOffsetDescription { set; get; }
        public bool ReadHeaderToEof { set; get; }

        // file count
        public bool UseStaticFileCount { set; get; }
        public string StaticFileCount { set; get; }
        public bool UseFileCountOffset { set; get; }
        public OffsetDescription FileCountOffsetDescription { set; get; }

        // file record basic information
        public string FileRecordsStartOffset { set; get; }
        public string FileRecordSize { set; get; }

        // file offset
        public bool UseFileOffsetOffset { set; get; }
        public CalculatingOffsetDescription FileOffsetOffsetDescription { set; get; }

        public bool UsePreviousFilesSizeToDetermineOffset { set; get; }
        public string BeginCuttingFilesAtOffset { set; get; }
        public bool UseByteAlignmentValue { set; get; }
        public string ByteAlignmentValue { set; get; }

        // file length/size
        public bool UseFileLengthOffset { set; get; }
        public CalculatingOffsetDescription FileLengthOffsetDescription { set; get; }

        public bool UseLocationOfNextFileToDetermineLength { set; get; }

        // file name
        public bool FileNameIsPresent { set; get; }

        public bool UseStaticFileNameOffsetWithinRecord { set; get; }
        public string StaticFileNameOffsetWithinRecord { set; get; }

        public bool UseAbsoluteFileNameOffset { set; get; }
        public OffsetDescription AbsoluteFileNameOffsetDescription { set; get; }

        public bool UseRelativeFileNameOffset { set; get; }
        public OffsetDescription RelativeFileNameOffsetDescription { set; get; }
        public VfsFileRecordRelativeOffsetLocationType FileRecordNameRelativeOffsetLocation { set; get; }

        // name size
        public bool UseStaticFileNameLength { set; get; }
        public string StaticFileNameLength { set; get; }

        public bool UseFileNameTerminatorString { set; get; }
        public string FileNameTerminatorString { set; get; }
    }

    public struct SimpleFileExtractionStruct
    {
        public string FilePath { set; get; }
        public long FileOffset { set; get; }
        public long FileLength { set; get; }
        public long FileNameLength { set; get; }

        public void Clear()
        {
            FilePath = string.Empty;
            FileOffset = -1;
            FileLength = -1;
            FileNameLength = -1;
        }
    }

    /// <summary>
    ///     Class containing universal constants.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        ///     Chunk size to use when reading from files.  Used to grab maximum buffer
        ///     size without using the large object heap which has poor collection.
        /// </summary>
        public const int FileReadChunkSize = 71680;

        /// <summary>
        ///     Constant used to send an ignore the value message to the progress bar.
        /// </summary>
        public const int IgnoreProgress = -1;

        /// <summary>
        ///     Constant used to send a generic message to the progress bar.
        /// </summary>
        public const int ProgressMessageOnly = -2;

        /// <summary>
        ///     Text description to use when describing a Big Endian option
        /// </summary>
        public const string BigEndianByteOrder = "Big Endian";

        /// <summary>
        ///     Text description to use when describing a Little Endian option
        /// </summary>
        public const string LittleEndianByteOrder = "Little Endian";

        public const string StringNullTerminator = "\0";

        public static readonly byte[] RiffHeaderBytes = {0x52, 0x49, 0x46, 0x46};
        public static readonly byte[] RiffDataBytes = {0x64, 0x61, 0x74, 0x61};
        public static readonly byte[] RiffWaveBytes = {0x57, 0x41, 0x56, 0x45};
        public static readonly byte[] RiffFmtBytes = {0x66, 0x6D, 0x74, 0x20};

        public static readonly byte[] NullByteArray = {0x00};

        // empty constructor
    }
}