using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VGMToolbox.util;

namespace VGMToolbox.format
{
    public class CriAcbCueRecord
    {
        public uint CueId { set; get; }
        public byte ReferenceType { set; get; }
        public ushort ReferenceIndex { set; get; }

        public bool IsWaveformIdentified { set; get; }
        public ushort WaveformIndex { set; get; }
        public ushort WaveformId { set; get; }
        public byte EncodeType { set; get; }
        public bool IsStreaming { set; get; }

        public string CueName { set; get; }
    }

    public class CriAcbFile : CriUtfTable
    {
        public const string EXTRACTION_FOLDER_FORMAT = "_vgmt_acb_ext_{0}";
        public static readonly string ACB_AWB_EXTRACTION_FOLDER = "acb" + Path.DirectorySeparatorChar + "awb";
        public static readonly string ACB_CPK_EXTRACTION_FOLDER = "acb" + Path.DirectorySeparatorChar + "cpk";
        public static readonly string EXT_AWB_EXTRACTION_FOLDER = "awb";
        public static readonly string EXT_CPK_EXTRACTION_FOLDER = "cpk";

        public const byte WAVEFORM_ENCODE_TYPE_ADX = 0;
        public const byte WAVEFORM_ENCODE_TYPE_HCA = 2;
        public const byte WAVEFORM_ENCODE_TYPE_HCA_ALT = 6;
        public const byte WAVEFORM_ENCODE_TYPE_VAG = 7;
        public const byte WAVEFORM_ENCODE_TYPE_ATRAC3 = 8;
        public const byte WAVEFORM_ENCODE_TYPE_BCWAV = 9;
        public const byte WAVEFORM_ENCODE_TYPE_ATRAC9 = 11;
        public const byte WAVEFORM_ENCODE_TYPE_NINTENDO_DSP = 13;

        public const string AWB_FORMAT1 = "{0}_streamfiles.awb";
        public const string AWB_FORMAT2 = "{0}.awb";
        public const string AWB_FORMAT3 = "{0}_STR.awb";

        protected enum AwbToExtract
        {
            Internal,
            External
        };


        public CriAcbFile(FileStream fs, long offset, bool includeCueIdInFileName)
        {
            // initialize UTF
            Initialize(fs, offset);

            // initialize ACB specific items
            InitializeCueList(fs);
            InitializeCueNameToWaveformMap(fs, includeCueIdInFileName);

            // initialize internal AWB
            if (InternalAwbFileSize > 0)
            {
                if (CriAfs2Archive.IsCriAfs2Archive(fs, (long) InternalAwbFileOffset))
                {
                    InternalAwb = new CriAfs2Archive(fs, (long) InternalAwbFileOffset);
                }
                else if (CriCpkArchive.IsCriCpkArchive(fs, (long) InternalAwbFileOffset))
                {
                    InternalCpk = new CriCpkArchive();
                    InternalCpk.Initialize(fs, (long) InternalAwbFileOffset, true);
                }
            }

            // initialize external AWB

            // @TODO: This isn't correct for files with CPKs
            if (StreamAwbAfs2HeaderSize > 0 ||
                !ByteConversion.IsZeroFilledByteArray(StreamAwbHash))
            {
                // get external file name
                StreamfilePath = GetStreamfilePath();

                // get file type
                using (var awbFs = File.Open(StreamfilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    // AWB
                    if (CriAfs2Archive.IsCriAfs2Archive(awbFs, 0))
                    {
                        ExternalAwb = new CriAfs2Archive(awbFs, 0);
                    }
                    // CPK
                    else if (CriCpkArchive.IsCriCpkArchive(awbFs, 0))
                    {
                        ExternalCpk = new CriCpkArchive();
                        ExternalCpk.Initialize(awbFs, 0, true);
                    }
                }
            }
        }

        public string Name => (string) GetUtfFieldForRow(this, 0, "Name");
        public string VersionString => (string) GetUtfFieldForRow(this, 0, "VersionString");

        public ulong InternalAwbFileOffset => (ulong) GetOffsetForUtfFieldForRow(this, 0, "AwbFile");
        public ulong InternalAwbFileSize => (ulong) GetSizeForUtfFieldForRow(this, 0, "AwbFile");

        public ulong CueTableOffset => GetOffsetForUtfFieldForRow(this, 0, "CueTable");

        public ulong CueNameTableOffset => GetOffsetForUtfFieldForRow(this, 0, "CueNameTable");

        public ulong WaveformTableOffset => GetOffsetForUtfFieldForRow(this, 0, "WaveformTable");

        public ulong SynthTableOffset => GetOffsetForUtfFieldForRow(this, 0, "SynthTable");

        public CriAcbCueRecord[] CueList { set; get; }
        public Dictionary<string, ushort> CueNamesToWaveforms { set; get; }

        public byte[] AcfMd5Hash => (byte[]) GetUtfFieldForRow(this, 0, "AcfMd5Hash");
        public ulong AwbFileOffset => GetOffsetForUtfFieldForRow(this, 0, "AwbFile");
        public CriAfs2Archive InternalAwb { set; get; }
        public CriCpkArchive InternalCpk { set; get; }

        public byte[] StreamAwbHash => (byte[]) GetUtfFieldForRow(this, 0, "StreamAwbHash");

        public ulong StreamAwbAfs2HeaderOffset => (ulong) GetOffsetForUtfFieldForRow(this, 0, "StreamAwbAfs2Header");

        public ulong StreamAwbAfs2HeaderSize => (ulong) GetSizeForUtfFieldForRow(this, 0, "StreamAwbAfs2Header");
        public string StreamfilePath { set; get; }
        public CriAfs2Archive ExternalAwb { set; get; }
        public CriCpkArchive ExternalCpk { set; get; }

        public void ExtractAll()
        {
            var baseExtractionFolder = Path.Combine(Path.GetDirectoryName(SourceFile),
                string.Format(EXTRACTION_FOLDER_FORMAT, Path.GetFileNameWithoutExtension(SourceFile)));

            ExtractAllUsingCueList(baseExtractionFolder);
        }


        protected void InitializeCueNameToWaveformMap(FileStream fs, bool includeCueIdInFileName)
        {
            ushort cueIndex;
            string cueName;

            var cueNameTableUtf = new CriUtfTable();
            cueNameTableUtf.Initialize(fs, (long) CueNameTableOffset);

            for (var i = 0; i < cueNameTableUtf.NumberOfRows; i++)
            {
                cueIndex = (ushort) GetUtfFieldForRow(cueNameTableUtf, i, "CueIndex");

                // skip cues with unidentified waveforms (see 'vc05_0140.acb, vc10_0372.acb' in Kidou_Senshi_Gundam_AGE_Universe_Accel (PSP))
                if (CueList[cueIndex].IsWaveformIdentified)
                {
                    cueName = (string) GetUtfFieldForRow(cueNameTableUtf, i, "CueName");

                    CueList[cueIndex].CueName = cueName;
                    CueList[cueIndex].CueName += GetFileExtensionForEncodeType(CueList[cueIndex].EncodeType);

                    if (includeCueIdInFileName)
                        CueList[cueIndex].CueName = string.Format("{0}_{1}",
                            CueList[cueIndex].CueId.ToString("D5"), CueList[cueIndex].CueName);

                    CueNamesToWaveforms.Add(CueList[cueIndex].CueName, CueList[cueIndex].WaveformId);
                }
            }
        }

        protected void InitializeCueList(FileStream fs)
        {
            CueNamesToWaveforms = new Dictionary<string, ushort>();

            ulong referenceItemsOffset = 0;
            ulong referenceItemsSize = 0;
            ulong referenceCorrection = 0;

            object dummy;
            byte isStreaming = 0;

            var cueTableUtf = new CriUtfTable();
            cueTableUtf.Initialize(fs, (long) CueTableOffset);

            var waveformTableUtf = new CriUtfTable();
            waveformTableUtf.Initialize(fs, (long) WaveformTableOffset);

            var synthTableUtf = new CriUtfTable();
            synthTableUtf.Initialize(fs, (long) SynthTableOffset);

            CueList = new CriAcbCueRecord[cueTableUtf.NumberOfRows];

            for (var i = 0; i < cueTableUtf.NumberOfRows; i++)
            {
                CueList[i] = new CriAcbCueRecord();
                CueList[i].IsWaveformIdentified = false;

                CueList[i].CueId = (uint) GetUtfFieldForRow(cueTableUtf, i, "CueId");
                CueList[i].ReferenceType = (byte) GetUtfFieldForRow(cueTableUtf, i, "ReferenceType");
                CueList[i].ReferenceIndex = (ushort) GetUtfFieldForRow(cueTableUtf, i, "ReferenceIndex");

                switch (CueList[i].ReferenceType)
                {
                    case 2:
                        referenceItemsOffset = (ulong) GetOffsetForUtfFieldForRow(synthTableUtf,
                            CueList[i].ReferenceIndex, "ReferenceItems");
                        referenceItemsSize =
                            GetSizeForUtfFieldForRow(synthTableUtf, CueList[i].ReferenceIndex, "ReferenceItems");
                        referenceCorrection = referenceItemsSize + 2;
                        break;
                    case 3:
                    case 8:
                        if (i == 0)
                        {
                            referenceItemsOffset =
                                (ulong) GetOffsetForUtfFieldForRow(synthTableUtf, 0, "ReferenceItems");
                            referenceItemsSize = GetSizeForUtfFieldForRow(synthTableUtf, 0, "ReferenceItems");
                            referenceCorrection =
                                referenceItemsSize -
                                2; // samples found have only had a '01 00' record => Always Waveform[0]?.                       
                        }
                        else
                        {
                            referenceCorrection += 4; // relative to previous offset, do not lookup
                            // @TODO: Should this do a referenceItemsSize - 2 for the ReferenceIndex?  Need to find 
                            //    one where size > 4.
                            //referenceItemsOffset = (ulong)CriUtfTable.GetOffsetForUtfFieldForRow(synthTableUtf, this.CueList[i].ReferenceIndex, "ReferenceItems");
                            //referenceItemsSize = CriUtfTable.GetSizeForUtfFieldForRow(synthTableUtf, this.CueList[i].ReferenceIndex, "ReferenceItems");
                            //referenceCorrection = referenceItemsSize - 2;
                        }

                        break;
                    default:
                        throw new FormatException(string.Format(
                            "  Unexpected ReferenceType: '{0}' for CueIndex: '{1}.'  Please report to VGMToolbox thread at hcs64.com forums, see link in 'Other' menu item.",
                            CueList[i].ReferenceType.ToString("D"), i.ToString("D")));
                }

                if (referenceItemsSize != 0)
                {
                    // get wave form info
                    CueList[i].WaveformIndex =
                        ParseFile.ReadUshortBE(fs, (long) (referenceItemsOffset + referenceCorrection));

                    // get Streaming flag, 0 = in ACB files, 1 = in AWB file
                    dummy = GetUtfFieldForRow(waveformTableUtf, CueList[i].WaveformIndex, "Streaming");

                    if (dummy != null) // check to see if this Id actually exists in the WaveformIndex
                    {
                        isStreaming = (byte) dummy;
                        CueList[i].IsStreaming = isStreaming == 0 ? false : true;

                        // get waveform id and encode type from corresponding waveform
                        var waveformId = GetUtfFieldForRow(waveformTableUtf, CueList[i].WaveformIndex, "Id");

                        if (waveformId != null)
                        {
                            // early revisions of ACB spec used "Id"
                            CueList[i].WaveformId = (ushort) waveformId;
                        }
                        else
                        {
                            // later versions using "MemoryAwbId" and  "StreamingAwbId"
                            if (CueList[i].IsStreaming)
                                CueList[i].WaveformId = (ushort) GetUtfFieldForRow(waveformTableUtf,
                                    CueList[i].WaveformIndex, "StreamAwbId");
                            else
                                CueList[i].WaveformId = (ushort) GetUtfFieldForRow(waveformTableUtf,
                                    CueList[i].WaveformIndex, "MemoryAwbId");
                        }

                        CueList[i].EncodeType =
                            (byte) GetUtfFieldForRow(waveformTableUtf, CueList[i].WaveformIndex, "EncodeType");


                        // update flag
                        CueList[i].IsWaveformIdentified = true;
                    } // if (dummy != null)
                }
            }
        }


        // @TODO: Need to update this for Internal/External CPK
        protected void ExtractAwbWithCueNames(string destinationFolder, AwbToExtract whichAwb)
        {
            CriUtfTable waveformTableUtf;
            byte encodeType;
            string rawFileName;

            CriAfs2Archive awb;
            var rawFileFormat = "{0}_{1}{2}";

            if (whichAwb == AwbToExtract.Internal)
                awb = InternalAwb;
            else
                awb = ExternalAwb;

            using (var fs = File.Open(awb.SourceFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                // use files names for internal AWB
                foreach (var key in CueNamesToWaveforms.Keys)
                    // extract file
                    ParseFile.ExtractChunkToFile64(fs,
                        (ulong) awb.Files[CueNamesToWaveforms[key]].FileOffsetByteAligned,
                        (ulong) awb.Files[CueNamesToWaveforms[key]].FileLength,
                        Path.Combine(destinationFolder, FileUtil.CleanFileName(key)), false, false);

                //-------------------------------------
                // extract any items without a CueName
                //-------------------------------------
                using (var acbStream = File.Open(SourceFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    waveformTableUtf = new CriUtfTable();
                    waveformTableUtf.Initialize(acbStream, (long) WaveformTableOffset);
                }

                // get list of unextracted files
                var unextractedFiles = awb.Files.Keys.Where(x => !CueNamesToWaveforms.ContainsValue(x));
                foreach (var key in unextractedFiles)
                {
                    encodeType = (byte) GetUtfFieldForRow(waveformTableUtf, key, "EncodeType");

                    rawFileName = string.Format(rawFileFormat,
                        Path.GetFileNameWithoutExtension(awb.SourceFile), key.ToString("D5"),
                        GetFileExtensionForEncodeType(encodeType));

                    // extract file
                    ParseFile.ExtractChunkToFile64(fs,
                        (ulong) awb.Files[key].FileOffsetByteAligned,
                        (ulong) awb.Files[key].FileLength,
                        Path.Combine(destinationFolder, rawFileName), false, false);
                }
            }
        }

        protected void ExtractAllUsingCueList(string destinationFolder)
        {
            CriUtfTable waveformTableUtf;
            ushort waveformIndex;
            byte encodeType;
            string rawFileName;
            var rawFileFormat = "{0}.{1}{2}";

            FileStream internalFs = null;
            FileStream externalFs = null;

            var internalIdsExtracted = new ArrayList();
            var externalIdsExtracted = new ArrayList();

            var acbAwbDestinationFolder = Path.Combine(destinationFolder, ACB_AWB_EXTRACTION_FOLDER);
            var extAwbDestinationFolder = Path.Combine(destinationFolder, EXT_AWB_EXTRACTION_FOLDER);
            var acbCpkDestinationFolder = Path.Combine(destinationFolder, ACB_CPK_EXTRACTION_FOLDER);
            var extCpkDestinationFolder = Path.Combine(destinationFolder, EXT_CPK_EXTRACTION_FOLDER);

            try
            {
                // open streams
                if (InternalAwb != null)
                    internalFs = File.Open(InternalAwb.SourceFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                else if (InternalCpk != null)
                    internalFs = File.Open(InternalCpk.SourceFileName, FileMode.Open, FileAccess.Read, FileShare.Read);

                if (ExternalAwb != null)
                    externalFs = File.Open(ExternalAwb.SourceFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                else if (ExternalCpk != null)
                    externalFs = File.Open(ExternalCpk.SourceFileName, FileMode.Open, FileAccess.Read, FileShare.Read);

                // loop through cues and extract
                for (var i = 0; i < CueList.Length; i++)
                {
                    var cue = CueList[i];

                    if (cue.IsWaveformIdentified)
                    {
                        if (cue.IsStreaming) // external AWB file
                        {
                            if (ExternalAwb != null)
                                ParseFile.ExtractChunkToFile64(externalFs,
                                    (ulong) ExternalAwb.Files[cue.WaveformId].FileOffsetByteAligned,
                                    (ulong) ExternalAwb.Files[cue.WaveformId].FileLength,
                                    Path.Combine(extAwbDestinationFolder, FileUtil.CleanFileName(cue.CueName)), false,
                                    false);
                            else if (ExternalCpk != null)
                                ParseFile.ExtractChunkToFile64(externalFs,
                                    (ulong) ExternalCpk.ItocFiles[cue.WaveformId].FileOffsetByteAligned,
                                    (ulong) ExternalCpk.ItocFiles[cue.WaveformId].FileLength,
                                    Path.Combine(extCpkDestinationFolder, FileUtil.CleanFileName(cue.CueName)), false,
                                    false);

                            externalIdsExtracted.Add(cue.WaveformId);
                        }
                        else // internal AWB file (inside ACB)
                        {
                            if (InternalAwb != null)
                                ParseFile.ExtractChunkToFile64(internalFs,
                                    (ulong) InternalAwb.Files[cue.WaveformId].FileOffsetByteAligned,
                                    (ulong) InternalAwb.Files[cue.WaveformId].FileLength,
                                    Path.Combine(acbAwbDestinationFolder, FileUtil.CleanFileName(cue.CueName)), false,
                                    false);
                            else if (InternalCpk != null)
                                ParseFile.ExtractChunkToFile64(internalFs,
                                    (ulong) InternalCpk.ItocFiles[cue.WaveformId].FileOffsetByteAligned,
                                    (ulong) InternalCpk.ItocFiles[cue.WaveformId].FileLength,
                                    Path.Combine(acbCpkDestinationFolder, FileUtil.CleanFileName(cue.CueName)), false,
                                    false);

                            internalIdsExtracted.Add(cue.WaveformId);
                        }
                    } // if (cue.IsWaveformIdentified)                    
                }

                // extract leftovers
                using (var acbStream = File.Open(SourceFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    waveformTableUtf = new CriUtfTable();
                    waveformTableUtf.Initialize(acbStream, (long) WaveformTableOffset);
                }

                if (ExternalAwb != null)
                {
                    var unextractedExternalFiles = ExternalAwb.Files.Keys.Where(x => !externalIdsExtracted.Contains(x));
                    foreach (var key in unextractedExternalFiles)
                    {
                        waveformIndex = GetWaveformRowIndexForWaveformId(waveformTableUtf, key, true);

                        encodeType = (byte) GetUtfFieldForRow(waveformTableUtf, waveformIndex, "EncodeType");

                        rawFileName = string.Format(rawFileFormat,
                            Path.GetFileName(ExternalAwb.SourceFile), key.ToString("D5"),
                            GetFileExtensionForEncodeType(encodeType));

                        // extract file
                        ParseFile.ExtractChunkToFile64(externalFs,
                            (ulong) ExternalAwb.Files[key].FileOffsetByteAligned,
                            (ulong) ExternalAwb.Files[key].FileLength,
                            Path.Combine(extAwbDestinationFolder, rawFileName), false, false);
                    }
                }
                else if (ExternalCpk != null)
                {
                    var unextractedExternalFiles =
                        ExternalCpk.ItocFiles.Keys.Where(x => !externalIdsExtracted.Contains(x));
                    foreach (var key in unextractedExternalFiles)
                    {
                        waveformIndex = GetWaveformRowIndexForWaveformId(waveformTableUtf, key, true);

                        encodeType = (byte) GetUtfFieldForRow(waveformTableUtf, waveformIndex, "EncodeType");

                        rawFileName = string.Format(rawFileFormat,
                            Path.GetFileName(ExternalCpk.SourceFileName), key.ToString("D5"),
                            GetFileExtensionForEncodeType(encodeType));

                        // extract file
                        ParseFile.ExtractChunkToFile64(externalFs,
                            (ulong) ExternalCpk.ItocFiles[key].FileOffsetByteAligned,
                            (ulong) ExternalCpk.ItocFiles[key].FileLength,
                            Path.Combine(extCpkDestinationFolder, rawFileName), false, false);
                    }
                }

                if (InternalAwb != null)
                {
                    var unextractedInternalFiles = InternalAwb.Files.Keys.Where(x => !internalIdsExtracted.Contains(x));
                    foreach (var key in unextractedInternalFiles)
                    {
                        waveformIndex = GetWaveformRowIndexForWaveformId(waveformTableUtf, key, false);

                        encodeType = (byte) GetUtfFieldForRow(waveformTableUtf, waveformIndex, "EncodeType");

                        rawFileName = string.Format(rawFileFormat,
                            Path.GetFileName(InternalAwb.SourceFile), key.ToString("D5"),
                            GetFileExtensionForEncodeType(encodeType));

                        // extract file
                        ParseFile.ExtractChunkToFile64(internalFs,
                            (ulong) InternalAwb.Files[key].FileOffsetByteAligned,
                            (ulong) InternalAwb.Files[key].FileLength,
                            Path.Combine(acbAwbDestinationFolder, rawFileName), false, false);
                    }
                }
                else if (InternalCpk != null)
                {
                    var unextractedInternalFiles =
                        InternalCpk.ItocFiles.Keys.Where(x => !internalIdsExtracted.Contains(x));
                    foreach (var key in unextractedInternalFiles)
                    {
                        waveformIndex = GetWaveformRowIndexForWaveformId(waveformTableUtf, key, false);

                        encodeType = (byte) GetUtfFieldForRow(waveformTableUtf, waveformIndex, "EncodeType");

                        rawFileName = string.Format(rawFileFormat,
                            Path.GetFileName(InternalCpk.SourceFileName), key.ToString("D5"),
                            GetFileExtensionForEncodeType(encodeType));

                        // extract file
                        ParseFile.ExtractChunkToFile64(internalFs,
                            (ulong) InternalCpk.ItocFiles[key].FileOffsetByteAligned,
                            (ulong) InternalCpk.ItocFiles[key].FileLength,
                            Path.Combine(acbCpkDestinationFolder, rawFileName), false, false);
                    }
                }
            }
            finally
            {
                if (internalFs != null)
                {
                    internalFs.Close();
                    internalFs.Dispose();
                }

                if (externalFs != null)
                {
                    externalFs.Close();
                    externalFs.Dispose();
                }
            }
        }

        protected string GetStreamfilePath()
        {
            string streamfilePath;

            string awbDirectory;
            string awbMask;
            string acbBaseFileName;
            string[] awbFiles;

            byte[] awbHashStored;
            byte[] awbHashCalculated;

            awbDirectory = Path.GetDirectoryName(SourceFile);

            // try format 1
            acbBaseFileName = Path.GetFileNameWithoutExtension(SourceFile);
            awbMask = string.Format(AWB_FORMAT1, acbBaseFileName);
            awbFiles = Directory.GetFiles(awbDirectory, awbMask, SearchOption.TopDirectoryOnly);

            if (awbFiles.Length < 1)
            {
                // try format 2
                awbMask = string.Format(AWB_FORMAT2, acbBaseFileName);
                awbFiles = Directory.GetFiles(awbDirectory, awbMask, SearchOption.TopDirectoryOnly);
            }

            if (awbFiles.Length < 1)
            {
                // try format 3
                awbMask = string.Format(AWB_FORMAT3, acbBaseFileName);
                awbFiles = Directory.GetFiles(awbDirectory, awbMask, SearchOption.TopDirectoryOnly);
            }

            // file not found
            if (awbFiles.Length < 1)
                throw new FileNotFoundException(string.Format(
                    "Cannot find AWB file. Please verify corresponding AWB file is named '{0}', '{1}', or '{2}'.",
                    string.Format(AWB_FORMAT1, acbBaseFileName), string.Format(AWB_FORMAT2, acbBaseFileName),
                    string.Format(AWB_FORMAT3, acbBaseFileName)));

            if (awbFiles.Length > 1)
                throw new FileNotFoundException(string.Format(
                    "More than one matching AWB file for this ACB. Please verify only one AWB file is named '{1}' or '{2}'.",
                    string.Format(AWB_FORMAT1, acbBaseFileName), string.Format(AWB_FORMAT2, acbBaseFileName)));

            // initialize AFS2 file                        
            using (var fs = File.Open(awbFiles[0], FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                awbHashStored = StreamAwbHash;

                if (awbHashStored.Length == 0x10) // MD5, hash in newer format unknown
                {
                    // validate MD5 checksum
                    awbHashCalculated = ByteConversion.GetBytesFromHexString(ChecksumUtil.GetMd5OfFullFile(fs));

                    if (!ParseFile.CompareSegment(awbHashCalculated, 0, awbHashStored))
                        throw new FormatException(string.Format(
                            "AWB file, <{0}>, did not match checksum inside ACB file.", Path.GetFileName(fs.Name)));
                }

                streamfilePath = awbFiles[0];
            }

            return streamfilePath;
        }

        protected CriAfs2Archive InitializeExternalAwbArchive()
        {
            CriAfs2Archive afs2 = null;

            string awbDirectory;
            string awbMask;
            string acbBaseFileName;
            string[] awbFiles;

            byte[] awbHashStored;
            byte[] awbHashCalculated;

            awbDirectory = Path.GetDirectoryName(SourceFile);

            // try format 1
            acbBaseFileName = Path.GetFileNameWithoutExtension(SourceFile);
            awbMask = string.Format(AWB_FORMAT1, acbBaseFileName);
            awbFiles = Directory.GetFiles(awbDirectory, awbMask, SearchOption.TopDirectoryOnly);

            if (awbFiles.Length < 1)
            {
                // try format 2
                awbMask = string.Format(AWB_FORMAT2, acbBaseFileName);
                awbFiles = Directory.GetFiles(awbDirectory, awbMask, SearchOption.TopDirectoryOnly);
            }

            if (awbFiles.Length < 1)
            {
                // try format 3
                awbMask = string.Format(AWB_FORMAT3, acbBaseFileName);
                awbFiles = Directory.GetFiles(awbDirectory, awbMask, SearchOption.TopDirectoryOnly);
            }

            // file not found
            if (awbFiles.Length < 1)
                throw new FileNotFoundException(string.Format(
                    "Cannot find AWB file. Please verify corresponding AWB file is named '{0}', '{1}', or '{2}'.",
                    string.Format(AWB_FORMAT1, acbBaseFileName), string.Format(AWB_FORMAT2, acbBaseFileName),
                    string.Format(AWB_FORMAT3, acbBaseFileName)));

            if (awbFiles.Length > 1)
                throw new FileNotFoundException(string.Format(
                    "More than one matching AWB file for this ACB. Please verify only one AWB file is named '{1}' or '{2}'.",
                    string.Format(AWB_FORMAT1, acbBaseFileName), string.Format(AWB_FORMAT2, acbBaseFileName)));

            // initialize AFS2 file                        
            using (var fs = File.Open(awbFiles[0], FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                awbHashStored = StreamAwbHash;

                if (awbHashStored.Length == 0x10) // MD5, hash in newer format unknown
                {
                    // validate MD5 checksum
                    awbHashCalculated = ByteConversion.GetBytesFromHexString(ChecksumUtil.GetMd5OfFullFile(fs));

                    if (!ParseFile.CompareSegment(awbHashCalculated, 0, awbHashStored))
                        throw new FormatException(string.Format(
                            "AWB file, <{0}>, did not match checksum inside ACB file.", Path.GetFileName(fs.Name)));
                }

                afs2 = new CriAfs2Archive(fs, 0);
            }

            return afs2;
        }

        public static ushort GetWaveformRowIndexForWaveformId(CriUtfTable utfTable, ushort waveformId, bool isStreamed)
        {
            var ret = ushort.MaxValue;

            for (var i = 0; i < utfTable.NumberOfRows; i++)
            {
                var tempId = GetUtfFieldForRow(utfTable, i, "Id");

                // newer archives use different labels
                if (tempId == null)
                {
                    if (isStreamed)
                        tempId = GetUtfFieldForRow(utfTable, i, "StreamAwbId");
                    else
                        tempId = GetUtfFieldForRow(utfTable, i, "MemoryAwbId");
                }

                if ((ushort) tempId == waveformId) ret = (ushort) i;
            }

            return ret;
        }

        public static string GetFileExtensionForEncodeType(byte encodeType)
        {
            string ext;

            switch (encodeType)
            {
                case WAVEFORM_ENCODE_TYPE_ADX:
                    ext = ".adx";
                    break;
                case WAVEFORM_ENCODE_TYPE_HCA:
                case WAVEFORM_ENCODE_TYPE_HCA_ALT:
                    ext = ".hca";
                    break;
                case WAVEFORM_ENCODE_TYPE_ATRAC3:
                    ext = ".at3";
                    break;
                case WAVEFORM_ENCODE_TYPE_ATRAC9:
                    ext = ".at9";
                    break;
                case WAVEFORM_ENCODE_TYPE_VAG:
                    ext = ".vag";
                    break;
                case WAVEFORM_ENCODE_TYPE_BCWAV:
                    ext = ".bcwav";
                    break;
                case WAVEFORM_ENCODE_TYPE_NINTENDO_DSP:
                    ext = ".dsp";
                    break;
                default:
                    ext = string.Format(".EncodeType-{0}.bin", encodeType.ToString("D2"));
                    break;
            }

            return ext;
        }
    }
}