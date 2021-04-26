using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using VGMToolbox.util;

namespace VGMToolbox.format
{
    public class CriAfs2File
    {
        public ushort CueId { set; get; }
        public long FileOffsetRaw { set; get; }
        public long FileOffsetByteAligned { set; get; }
        public long FileLength { set; get; }

        // public string FileName { set; get; } // comes from ACB for now, maybe included in archive later?
    }

    public class CriAfs2Archive
    {
        public static readonly byte[] SIGNATURE = new byte[] {0x41, 0x46, 0x53, 0x32};
        public const string EXTRACTION_FOLDER_FORMAT = "_vgmt_awb_ext_{0}";

        public string SourceFile { set; get; }
        public byte[] MagicBytes { set; get; }
        public byte[] Version { set; get; }
        public uint FileCount { set; get; }
        public uint ByteAlignment { set; get; }
        public Dictionary<ushort, CriAfs2File> Files { set; get; }

        public CriAfs2Archive(FileStream fs, long offset)
        {
            var previousCueId = ushort.MaxValue;

            if (IsCriAfs2Archive(fs, offset))
            {
                SourceFile = fs.Name;
                var afs2FileSize = fs.Length;

                MagicBytes = ParseFile.ParseSimpleOffset(fs, offset, SIGNATURE.Length);
                Version = ParseFile.ParseSimpleOffset(fs, offset + 4, 4);
                FileCount = ParseFile.ReadUintLE(fs, offset + 8);

                // setup offset field size
                int offsetFieldSize =
                    Version[1]; // known values: 2 and 4.  4 is most common.  I've only seen 2 in 'se_enemy_gurdon_galaga_bee.acb' from Sonic Lost World.
                uint offsetMask = 0;

                for (var j = 0; j < offsetFieldSize; j++) offsetMask |= (uint) ((byte) 0xFF << (j * 8));

                if (FileCount > ushort.MaxValue)
                    throw new FormatException(string.Format(
                        "ERROR, file count exceeds max value for ushort.  Please report this at official feedback forums (see 'Other' menu item).",
                        fs.Name));

                ByteAlignment = ParseFile.ReadUintLE(fs, offset + 0xC);
                Files = new Dictionary<ushort, CriAfs2File>((int) FileCount);

                CriAfs2File dummy;

                for (ushort i = 0; i < FileCount; i++)
                {
                    dummy = new CriAfs2File();

                    dummy.CueId = ParseFile.ReadUshortLE(fs, offset + (0x10 + 2 * i));
                    dummy.FileOffsetRaw = ParseFile.ReadUintLE(fs, offset + 0x10 + FileCount * 2 + offsetFieldSize * i);

                    // mask off unneeded info
                    dummy.FileOffsetRaw &= offsetMask;

                    // add offset
                    dummy.FileOffsetRaw += offset; // for AFS2 files inside of other files (ACB, etc.)

                    // set file offset to byte alignment
                    if (dummy.FileOffsetRaw % ByteAlignment != 0)
                        dummy.FileOffsetByteAligned =
                            MathUtil.RoundUpToByteAlignment(dummy.FileOffsetRaw, ByteAlignment);
                    else
                        dummy.FileOffsetByteAligned = dummy.FileOffsetRaw;

                    //---------------
                    // set file size
                    //---------------
                    // last file will use final offset entry
                    if (i == FileCount - 1)
                        dummy.FileLength =
                            ParseFile.ReadUintLE(fs,
                                offset + 0x10 + FileCount * 2 + offsetFieldSize * i + offsetFieldSize) + offset -
                            dummy.FileOffsetByteAligned;

                    // else set length for previous cue id
                    if (previousCueId != ushort.MaxValue)
                        Files[previousCueId].FileLength =
                            dummy.FileOffsetRaw - Files[previousCueId].FileOffsetByteAligned;

                    Files.Add(dummy.CueId, dummy);
                    previousCueId = dummy.CueId;
                } // for (uint i = 0; i < this.FileCount; i++)
            }
            else
            {
                throw new FormatException(string.Format("AFS2 magic bytes not found at offset: 0x{0}.",
                    offset.ToString("X8")));
            }
        }

        public static bool IsCriAfs2Archive(FileStream fs, long offset)
        {
            var ret = false;
            var checkBytes = ParseFile.ParseSimpleOffset(fs, offset, SIGNATURE.Length);

            if (ParseFile.CompareSegment(checkBytes, 0, SIGNATURE)) ret = true;

            return ret;
        }

        public void ExtractAll()
        {
            var baseExtractionFolder = Path.Combine(Path.GetDirectoryName(SourceFile),
                string.Format(EXTRACTION_FOLDER_FORMAT, Path.GetFileNameWithoutExtension(SourceFile)));

            ExtractAllRaw(baseExtractionFolder);
        }

        public void ExtractAllRaw(string destinationFolder)
        {
            var rawFileFormat = "{0}.{1}.bin";

            using (var fs = File.Open(SourceFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                foreach (var key in Files.Keys)
                    ParseFile.ExtractChunkToFile64(fs,
                        (ulong) Files[key].FileOffsetByteAligned,
                        (ulong) Files[key].FileLength,
                        Path.Combine(destinationFolder,
                            string.Format(rawFileFormat, Path.GetFileName(SourceFile), key.ToString("D5"))), false,
                        false);
            }
        }
    }
}