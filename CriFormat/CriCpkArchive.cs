using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VGMToolbox.format.iso;
using VGMToolbox.util;

namespace VGMToolbox.format
{
    public class CriCpkArchive : IVolume
    {
        public static readonly byte[] STANDARD_IDENTIFIER = new byte[] {0x43, 0x50, 0x4B, 0x20};
        public const uint IDENTIFIER_OFFSET = 0x00;
        public const string FORMAT_DESCRIPTION_STRING = "CRI CPK Archive";

        public const string NULL_FILENAME = "<NULL>";

        public static readonly byte[] CRILAYLA_SIGNATURE = new byte[] {0x43, 0x52, 0x49, 0x4C, 0x41, 0x59, 0x4C, 0x41};
        public const string CRI_CPK_EXTRACTION_FOLDER = "VGMT_CPK_EXTRACT_{0}";

        // public static byte[] CriCpkDecompressionOutputBuffer = new byte[64000000];

        public string SourceFileName { set; get; }

        public CriUtfTable TocUtf { set; get; }
        public CriUtfTable EtocUtf { set; get; }
        public CriUtfTable ItocUtf { set; get; }
        public CriUtfTable GtocUtf { set; get; }

        public Dictionary<ushort, CriAfs2File> ItocFiles { set; get; } // for use by ACB extraction

        public static bool IsCriCpkArchive(FileStream fs, long offset)
        {
            var ret = false;
            var checkBytes = ParseFile.ParseSimpleOffset(fs, offset, STANDARD_IDENTIFIER.Length);

            if (ParseFile.CompareSegment(checkBytes, 0, STANDARD_IDENTIFIER)) ret = true;

            return ret;
        }

        public void Initialize(FileStream fs, long offset, bool isRawDump)
        {
            FormatDescription = FORMAT_DESCRIPTION_STRING;

            VolumeBaseOffset = offset;
            IsRawDump = isRawDump;
            VolumeType = VolumeDataType.Data;
            DirectoryStructureArray = new ArrayList();

            SourceFileName = fs.Name;

            var cpkUtf = new CriUtfTable();
            cpkUtf.Initialize(fs, VolumeBaseOffset + 0x10);

            VolumeIdentifier = cpkUtf.TableName;

            ItocFiles = new Dictionary<ushort, CriAfs2File>();

            TocUtf = InitializeToc(fs, cpkUtf, "TocOffset");
            // this.EtocUtf = InitializeToc(fs, cpkUtf, "EtocOffset");
            ItocUtf = InitializeToc(fs, cpkUtf, "ItocOffset");
            // this.GtocUtf = InitializeToc(fs, cpkUtf, "GtocOffset");


            var dummy = new CriCpkDirectory(SourceFileName, string.Empty, string.Empty);

            if (TocUtf != null)
            {
                var tocDir = GetDirectoryForToc(fs, cpkUtf, "TOC", VolumeBaseOffset);

                if (tocDir.FileArray.Count > 0 || tocDir.SubDirectoryArray.Count > 0)
                    dummy.SubDirectoryArray.Add(tocDir);
            }

            /*
            if (etocUtf != null)
            {
                CriCpkDirectory etocDir = this.GetDirectoryForToc(fs, cpkUtf, etocUtf, "ETOC");

                if ((etocDir.FileArray.Count > 0) || (etocDir.SubDirectoryArray.Count > 0))
                {
                    dummy.SubDirectoryArray.Add(etocDir);
                }
            }
            */

            if (ItocUtf != null)
            {
                var itocDir = GetDirectoryForItoc(fs, cpkUtf, "ITOC", VolumeBaseOffset);

                if (itocDir.FileArray.Count > 0 || itocDir.SubDirectoryArray.Count > 0)
                    dummy.SubDirectoryArray.Add(itocDir);
            }


            //if (this.GtocUtf != null)
            //{
            //    CriCpkDirectory gtocDir = this.GetDirectoryForGtoc(fs, cpkUtf, "GTOC");

            //    if ((gtocDir.FileArray.Count > 0) || (gtocDir.SubDirectoryArray.Count > 0))
            //    {
            //        dummy.SubDirectoryArray.Add(gtocDir);
            //    }
            //}


            DirectoryStructureArray.Add(dummy);
        }

        #region IVolume

        public ArrayList DirectoryStructureArray { set; get; }

        public IDirectoryStructure[] Directories
        {
            set => Directories = value;
            get
            {
                DirectoryStructureArray.Sort();
                return (IDirectoryStructure[]) DirectoryStructureArray.ToArray(typeof(CriCpkDirectory));
            }
        }

        public long VolumeBaseOffset { set; get; }
        public string FormatDescription { set; get; }
        public VolumeDataType VolumeType { set; get; }
        public string VolumeIdentifier { set; get; }
        public bool IsRawDump { set; get; }

        #endregion

        /// <summary>
        /// Extract all files in archive.
        /// </summary>
        /// <param name="streamCache"></param>
        /// <param name="destintionFolder"></param>
        /// <param name="extractAsRaw"></param>
        public void ExtractAll(ref Dictionary<string, FileStream> streamCache, string destintionFolder,
            bool extractAsRaw)
        {
            foreach (CriCpkDirectory ds in DirectoryStructureArray)
                ds.Extract(ref streamCache, destintionFolder, extractAsRaw);
        }

        public void ExtractAll()
        {
            var extractionDirectoryBase = Path.Combine(Path.GetDirectoryName(SourceFileName),
                string.Format(CRI_CPK_EXTRACTION_FOLDER, Path.GetFileName(SourceFileName)));
            var streamCache = new Dictionary<string, FileStream>();

            foreach (CriCpkDirectory ds in DirectoryStructureArray)
                ds.Extract(ref streamCache, extractionDirectoryBase, false);
        }


        #region Static Functions

        public static CriUtfTable InitializeToc(FileStream fs, CriUtfTable cpkUtf, string tocIdentifierString)
        {
            CriUtfTable toc = null;
            if (cpkUtf.Rows[0].ContainsKey(tocIdentifierString) &&
                cpkUtf.Rows[0][tocIdentifierString].Value != null)
            {
                toc = new CriUtfTable();
                toc.Initialize(fs,
                    (long) ((ulong) cpkUtf.BaseOffset + (ulong) cpkUtf.Rows[0][tocIdentifierString].Value));
            }

            return toc;
        }

        public static CriUtfTocFileInfo GetUtfTocFileInfo(CriUtfTable tocUtf, int rowIndex)
        {
            var fileInfo = new CriUtfTocFileInfo();
            object temp;

            temp = CriUtfTable.GetUtfFieldForRow(tocUtf, rowIndex, "DirName");
            fileInfo.DirName = temp != null ? (string) temp : null;
            fileInfo.DirName = fileInfo.DirName.Equals(NULL_FILENAME) ? string.Empty : fileInfo.DirName;

            temp = CriUtfTable.GetUtfFieldForRow(tocUtf, rowIndex, "FileName");
            fileInfo.FileName = temp != null ? (string) temp : null;
            fileInfo.FileName = fileInfo.FileName.Equals(NULL_FILENAME)
                ? string.Format("{0}.bin", rowIndex.ToString("D5"))
                : fileInfo.FileName;

            temp = CriUtfTable.GetUtfFieldForRow(tocUtf, rowIndex, "FileOffset");
            fileInfo.FileOffset = temp != null ? (ulong) temp : ulong.MaxValue;

            temp = CriUtfTable.GetUtfFieldForRow(tocUtf, rowIndex, "FileSize");
            fileInfo.FileSize = temp != null ? (uint) temp : uint.MaxValue;

            temp = CriUtfTable.GetUtfFieldForRow(tocUtf, rowIndex, "ExtractSize");
            fileInfo.ExtractSize = temp != null ? (uint) temp : uint.MaxValue;

            return fileInfo;
        }

        public static CriUtfTocFileInfo GetUtfItocFileInfo(CriUtfTable dataUtf, int rowIndex)
        {
            var fileInfo = new CriUtfTocFileInfo();
            object temp;

            temp = CriUtfTable.GetUtfFieldForRow(dataUtf, rowIndex, "ID");
            fileInfo.FileName = temp != null ? ((ushort) temp).ToString("D5") : null;

            temp = CriUtfTable.GetUtfFieldForRow(dataUtf, rowIndex, "FileSize");
            fileInfo.FileSize = temp != null ? Convert.ToUInt32(temp) : uint.MaxValue;

            temp = CriUtfTable.GetUtfFieldForRow(dataUtf, rowIndex, "ExtractSize");
            fileInfo.ExtractSize = temp != null ? Convert.ToUInt32(temp) : uint.MaxValue;

            return fileInfo;
        }

        public static CriCpkDirectory GetGtocDirectory(CriUtfTable dataUtf, int rowIndex, string sourceFile,
            string parentDirectory)
        {
            CriCpkDirectory gtocDirectory;
            object temp;

            temp = CriUtfTable.GetUtfFieldForRow(dataUtf, rowIndex, "Gname");
            gtocDirectory = new CriCpkDirectory(sourceFile, (string) temp, parentDirectory);

            temp = CriUtfTable.GetUtfFieldForRow(dataUtf, rowIndex, "Child");

            if ((short) temp != -1)
            {
                // add files
            }

            return gtocDirectory;
        }

        public CriCpkDirectory GetDirectoryForToc(FileStream fs, CriUtfTable cpkUtf, string BaseDirectoryName,
            long volumeBaseOffset)
        {
            var fileInfo = new CriUtfTocFileInfo();

            long trueTocBaseOffset;
            var contentOffset = (ulong) CriUtfTable.GetUtfFieldForRow(cpkUtf, 0, "ContentOffset");
            var align = (ushort) CriUtfTable.GetUtfFieldForRow(cpkUtf, 0, "Align");

            var separators = new char[] {'/', '\\'};
            var allDirectories = new ArrayList();

            var baseDirectory = new CriCpkDirectory(SourceFileName, BaseDirectoryName, string.Empty);
            CriCpkDirectory newDirectory;
            CriCpkDirectory currentDirectory;
            int indexOfNewDirectory;

            CriCpkFile tempFile;
            var fileListDictionary = new Dictionary<string, ArrayList>();
            string parentName;

            // loop over files to get unique dirs
            for (var i = 0; i < TocUtf.Rows.GetLength(0); i++)
            {
                fileInfo = GetUtfTocFileInfo(TocUtf, i);

                if (fileInfo.FileName != null)
                {
                    //---------------
                    // get directory
                    //---------------
                    if (!fileListDictionary.ContainsKey(fileInfo.DirName))
                        fileListDictionary.Add(fileInfo.DirName, new ArrayList());

                    //--------------
                    // create file
                    //--------------
                    // set true base offset, since UTF header starts at 0x10 of a container type
                    trueTocBaseOffset = TocUtf.BaseOffset - 0x10;

                    // get absolute offset
                    if (contentOffset < (ulong) trueTocBaseOffset)
                        fileInfo.FileOffset += contentOffset;
                    else
                        fileInfo.FileOffset += (ulong) trueTocBaseOffset;

                    if (fileInfo.FileOffset % align != 0)
                        fileInfo.FileOffset = MathUtil.RoundUpToByteAlignment(fileInfo.FileOffset, align);

                    parentName = BaseDirectoryName + Path.DirectorySeparatorChar + fileInfo.DirName;
                    parentName = parentName.Replace('/', Path.DirectorySeparatorChar);

                    tempFile = new CriCpkFile(parentName, SourceFileName, fileInfo.FileName, (long) fileInfo.FileOffset,
                        VolumeBaseOffset,
                        (long) fileInfo.FileOffset, fileInfo.FileSize, fileInfo.ExtractSize);

                    // add to Dictionary
                    fileListDictionary[fileInfo.DirName].Add(tempFile);
                }
            }

            foreach (var path in fileListDictionary.Keys)
            {
                currentDirectory = baseDirectory;
                var pathItems = path.Split(separators);

                parentName = BaseDirectoryName;

                for (var i = 0; i < pathItems.Count(); i++)
                {
                    var item = pathItems[i];

                    var tmp = currentDirectory.SubDirectoryArray.Cast<CriCpkDirectory>()
                        .Where(x => x.DirectoryName.Equals(item));

                    if (tmp.Count() > 0)
                    {
                        currentDirectory = tmp.Single();
                    }
                    else
                    {
                        newDirectory = new CriCpkDirectory(SourceFileName, item, parentName);
                        indexOfNewDirectory = currentDirectory.SubDirectoryArray.Add(newDirectory);
                        currentDirectory = (CriCpkDirectory) currentDirectory.SubDirectoryArray[indexOfNewDirectory];
                    }

                    if (i == pathItems.GetUpperBound(0))
                        foreach (CriCpkFile f in fileListDictionary[path])
                            currentDirectory.FileArray.Add(f);

                    parentName += Path.DirectorySeparatorChar + item;
                }
            }

            return baseDirectory;
        }

        public CriCpkDirectory GetDirectoryForItoc(FileStream fs, CriUtfTable cpkUtf, string BaseDirectoryName,
            long volumeBaseOffset)
        {
            ulong currentOffset = 0;
            var fileInfo = new CriUtfTocFileInfo();
            var afs2File = new CriAfs2File();

            // get content offset and align
            var contentOffset = (ulong) CriUtfTable.GetUtfFieldForRow(cpkUtf, 0, "ContentOffset");
            var align = (ushort) CriUtfTable.GetUtfFieldForRow(cpkUtf, 0, "Align");

            // build direcory path
            var baseDirectory = new CriCpkDirectory(SourceFileName, BaseDirectoryName, string.Empty);
            CriCpkFile tempFile;

            // read file groups
            uint filesH = 0;
            uint filesL = 0;
            var filesHObj = CriUtfTable.GetUtfFieldForRow(ItocUtf, 0, "FilesH"); //count of files in DataH
            var filesLObj = CriUtfTable.GetUtfFieldForRow(ItocUtf, 0, "FilesL"); // count of files in DataL

            if (filesHObj != null) filesH = (uint) filesHObj;

            if (filesHObj != null) filesL = (uint) filesLObj;

            if (filesH > 0 || filesL > 0)
            {
                var fileList = new Dictionary<string, CriUtfTocFileInfo>();

                if (filesH > 0)
                {
                    // read DataH group
                    var dataH = new CriUtfTable();
                    dataH.Initialize(fs, (long) CriUtfTable.GetOffsetForUtfFieldForRow(ItocUtf, 0, "DataH"));

                    for (var i = 0; i < dataH.Rows.GetLength(0); i++)
                    {
                        fileInfo = GetUtfItocFileInfo(dataH, i);
                        fileList.Add(fileInfo.FileName, fileInfo);
                    }
                }

                if (filesL > 0)
                {
                    // read DataL group
                    var dataL = new CriUtfTable();
                    dataL.Initialize(fs, (long) CriUtfTable.GetOffsetForUtfFieldForRow(ItocUtf, 0, "DataL"));

                    for (var i = 0; i < dataL.Rows.GetLength(0); i++)
                    {
                        fileInfo = GetUtfItocFileInfo(dataL, i);
                        fileList.Add(fileInfo.FileName, fileInfo);
                    }
                }

                // initialize current offset
                currentOffset = contentOffset;

                // populate offsets for files
                var keys = fileList.Keys.ToList();
                keys.Sort();

                foreach (var key in keys)
                {
                    // afs2 file for ACB extraction
                    afs2File = new CriAfs2File();
                    afs2File.CueId =
                        Convert.ToUInt16(fileList[key].FileName); // ??? @TODO maybe key is enough?  need to check.
                    afs2File.FileOffsetRaw = volumeBaseOffset + (long) currentOffset;

                    // align offset
                    if (currentOffset % align != 0)
                        currentOffset = MathUtil.RoundUpToByteAlignment(currentOffset, align);

                    // update file info
                    fileList[key].FileOffset = (ulong) volumeBaseOffset + currentOffset;
                    fileList[key].FileName += ".bin";

                    // afs2 file for ACB extraction
                    afs2File.FileOffsetByteAligned = (long) fileList[key].FileOffset;
                    afs2File.FileLength = fileList[key].FileSize;

                    // increment current offset
                    currentOffset += fileList[key].FileSize;

                    // create file and add to base directory
                    tempFile = new CriCpkFile(BaseDirectoryName, SourceFileName, fileList[key].FileName,
                        (long) fileList[key].FileOffset, VolumeBaseOffset, (long) fileList[key].FileOffset,
                        fileList[key].FileSize, fileList[key].ExtractSize);

                    baseDirectory.FileArray.Add(tempFile);

                    // add afs2 file to ItocFiles for ACB extraction
                    ItocFiles.Add(afs2File.CueId, afs2File);
                } // foreach (string key in keys)
            } // if ((filesH > 0) || (filesL > 0))       


            return baseDirectory;
        }

        /*
        public CriCpkDirectory GetDirectoryForGtoc(FileStream fs, CriUtfTable cpkUtf, string BaseDirectoryName)
        {
            CriUtfTocFileInfo fileInfo = new CriUtfTocFileInfo();

            CriCpkDirectory baseDirectory = new CriCpkDirectory(this.SourceFileName, BaseDirectoryName, String.Empty);
            CriCpkDirectory newDirectory;
            CriCpkDirectory currentDirectory;
            int indexOfNewDirectory;

            // get Gdata UTF table (Directories)
            uint glink = (uint)CriUtfTable.GetOffsetForUtfFieldForRow(this.GtocUtf, 0, "Glink");
            
            ulong gDataOffset = (ulong)CriUtfTable.GetOffsetForUtfFieldForRow(this.GtocUtf, 0, "Gdata");
            CriUtfTable gData = new CriUtfTable();
            gData.Initialize(fs, (long)gDataOffset);

            // get directories
            int currentIndex = 0;
            currentDirectory = GetGtocDirectory(gData, currentIndex, this.SourceFileName, String.Empty);
            int next = (int)CriUtfTable.GetUtfFieldForRow(gData, currentIndex, "Next");
            baseDirectory.SubDirectoryArray.Add(currentDirectory);

            while (next != -1)
            { 
            
            }





            long trueTocBaseOffset;
            ulong contentOffset = (ulong)CriUtfTable.GetUtfFieldForRow(cpkUtf, 0, "ContentOffset");
            ushort align = (ushort)CriUtfTable.GetUtfFieldForRow(cpkUtf, 0, "Align");

            char[] separators = new char[] { '/', '\\' };
            ArrayList allDirectories = new ArrayList();



            CriCpkFile tempFile;
            Dictionary<string, ArrayList> fileListDictionary = new Dictionary<string, ArrayList>();
            string parentName;

            // loop over files to get unique dirs
            for (int i = 0; i < this.TocUtf.Rows.GetLength(0); i++)
            {
                fileInfo = GetUtfTocFileInfo(this.TocUtf, i);

                if (fileInfo.FileName != null)
                {
                    //---------------
                    // get directory
                    //---------------
                    if (!fileListDictionary.ContainsKey(fileInfo.DirName))
                    {
                        fileListDictionary.Add(fileInfo.DirName, new ArrayList());
                    }

                    //--------------
                    // create file
                    //--------------
                    // set true base offset, since UTF header starts at 0x10 of a container type
                    trueTocBaseOffset = this.TocUtf.BaseOffset - 0x10;

                    // get absolute offset
                    if (contentOffset < (ulong)trueTocBaseOffset)
                    {
                        fileInfo.FileOffset += contentOffset;
                    }
                    else
                    {
                        fileInfo.FileOffset += (ulong)trueTocBaseOffset;
                    }

                    if (fileInfo.FileOffset % align != 0)
                    {
                        fileInfo.FileOffset = MathUtil.RoundUpToByteAlignment(fileInfo.FileOffset, align);
                    }

                    parentName = BaseDirectoryName + Path.DirectorySeparatorChar + fileInfo.DirName;
                    parentName = parentName.Replace('/', Path.DirectorySeparatorChar);

                    tempFile = new CriCpkFile(parentName, this.SourceFileName, fileInfo.FileName, (long)fileInfo.FileOffset, this.VolumeBaseOffset,
                        (long)fileInfo.FileOffset, fileInfo.FileSize, fileInfo.ExtractSize);

                    // add to Dictionary
                    fileListDictionary[fileInfo.DirName].Add(tempFile);
                }
            }

            foreach (var path in fileListDictionary.Keys)
            {
                currentDirectory = baseDirectory;
                var pathItems = path.Split(separators);

                parentName = BaseDirectoryName;

                for (int i = 0; i < pathItems.Count(); i++)
                {
                    var item = pathItems[i];

                    var tmp = currentDirectory.SubDirectoryArray.Cast<CriCpkDirectory>().Where(x => x.DirectoryName.Equals(item));

                    if (tmp.Count() > 0)
                    {
                        currentDirectory = tmp.Single();
                    }
                    else
                    {
                        newDirectory = new CriCpkDirectory(this.SourceFileName, item, parentName);
                        indexOfNewDirectory = currentDirectory.SubDirectoryArray.Add(newDirectory);
                        currentDirectory = (CriCpkDirectory)currentDirectory.SubDirectoryArray[indexOfNewDirectory];
                    }

                    if (i == pathItems.GetUpperBound(0))
                    {
                        foreach (CriCpkFile f in fileListDictionary[path])
                        {
                            currentDirectory.FileArray.Add(f);
                        }
                    }

                    parentName += Path.DirectorySeparatorChar + item;
                }
            }

            return baseDirectory;
        }
        */

        private static ushort get_next_bits(Stream inFile, ref long offset, ref byte bit_pool, ref int bits_left,
            int bit_count)
        {
            ushort out_bits = 0;
            var num_bits_produced = 0;

            while (num_bits_produced < bit_count)
            {
                if (bits_left == 0)
                {
                    bit_pool = ParseFile.ReadByte(inFile, offset);
                    bits_left = 8;
                    offset--;
                }

                int bits_this_round;

                if (bits_left > bit_count - num_bits_produced)
                    bits_this_round = bit_count - num_bits_produced;
                else
                    bits_this_round = bits_left;

                out_bits <<= bits_this_round;
                out_bits |= (ushort) ((ushort) (bit_pool >> (bits_left - bits_this_round)) &
                                      ((1 << bits_this_round) - 1));


                bits_left -= bits_this_round;
                num_bits_produced += bits_this_round;
            }

            return out_bits;
        }

        public static long Uncompress(Stream inFile, long offset, long input_size, string outFile)
        {
            byte[] output_buffer;
            long bytes_output = 0;
            var magicBytes = ParseFile.ParseSimpleOffset(inFile, offset, CRILAYLA_SIGNATURE.GetLength(0));

            if (!ParseFile.CompareSegment(magicBytes, 0, CRILAYLA_SIGNATURE))
                throw new FormatException(string.Format("CRILAYLA Signature not found at offset: 0x{0}",
                    offset.ToString("X8")));

            if (!Directory.Exists(Path.GetDirectoryName(outFile)))
                Directory.CreateDirectory(Path.GetDirectoryName(outFile));

            var uncompressed_size = (long) ParseFile.ReadUintLE(inFile, offset + 8);
            var uncompressed_header_offset = (long) (offset + ParseFile.ReadUintLE(inFile, offset + 0x0C) + 0x10);

            if (uncompressed_header_offset + 0x100 != offset + input_size)
                throw new FormatException(string.Format(
                    "CRILAYLA: Uncompressed header size does not match expected size at offset: 0x{0}",
                    offset.ToString("X8")));

            output_buffer = new byte[uncompressed_size + 0x100];

            // write uncompressed header
            inFile.Position = uncompressed_header_offset;
            inFile.Read(output_buffer, 0, 0x100);

            // do the hocus pocus?
            var input_end = offset + input_size - 0x100 - 1;
            var input_offset = input_end;
            var output_end = 0x100 + uncompressed_size - 1;

            byte bit_pool = 0;
            var bits_left = 0;
            var vle_lens = new int[4] {2, 3, 5, 8};

            long backreference_offset;
            long backreference_length;

            int vle_level;
            int this_level;

            ushort temp;

            while (bytes_output < uncompressed_size)
                if (get_next_bits(inFile, ref input_offset, ref bit_pool, ref bits_left, 1) > 0)
                {
                    backreference_offset = output_end - bytes_output +
                                           get_next_bits(inFile, ref input_offset, ref bit_pool, ref bits_left, 13) + 3;
                    backreference_length = 3;

                    // decode variable length coding for length                        
                    for (vle_level = 0; vle_level < vle_lens.Length; vle_level++)
                    {
                        this_level = get_next_bits(inFile, ref input_offset, ref bit_pool, ref bits_left,
                            vle_lens[vle_level]);
                        backreference_length += this_level;

                        if (this_level != (1 << vle_lens[vle_level]) - 1) break;
                    }

                    if (vle_level == vle_lens.Length)
                        do
                        {
                            this_level = get_next_bits(inFile, ref input_offset, ref bit_pool, ref bits_left, 8);
                            backreference_length += this_level;
                        } while (this_level == 255);

                    //printf("0x%08lx backreference to 0x%lx, length 0x%lx\n", output_end-bytes_output, backreference_offset, backreference_length);
                    for (var i = 0; i < backreference_length; i++)
                    {
                        output_buffer[output_end - bytes_output] = output_buffer[backreference_offset--];
                        bytes_output++;
                    }
                }
                else
                {
                    // verbatim byte
                    temp = get_next_bits(inFile, ref input_offset, ref bit_pool, ref bits_left, 8);
                    output_buffer[output_end - bytes_output] = (byte) temp;

                    //printf("0x%08lx verbatim byte\n", output_end-bytes_output);
                    bytes_output++;
                }

            using (var outStream = File.Open(outFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                outStream.Write(output_buffer, 0, output_buffer.Length);
            }

            return 0x100 + bytes_output;
        }

        #endregion
    }

    public class CriCpkDirectory : IDirectoryStructure
    {
        public string SourceFilePath { set; get; }
        public string DirectoryName { set; get; }
        public string ParentDirectoryName { set; get; }

        public ArrayList SubDirectoryArray { set; get; }

        public IDirectoryStructure[] SubDirectories
        {
            set => SubDirectories = value;
            get
            {
                SubDirectoryArray.Sort();
                return (IDirectoryStructure[]) SubDirectoryArray.ToArray(typeof(CriCpkDirectory));
            }
        }

        public ArrayList FileArray { set; get; }

        public IFileStructure[] Files
        {
            set => Files = value;
            get
            {
                FileArray.Sort();
                return (IFileStructure[]) FileArray.ToArray(typeof(CriCpkFile));
            }
        }

        public CriCpkDirectory(string sourceFilePath, string directoryName, string parentDirectoryName)
        {
            SourceFilePath = sourceFilePath;
            SubDirectoryArray = new ArrayList();
            FileArray = new ArrayList();

            ParentDirectoryName = parentDirectoryName;
            DirectoryName = directoryName;
        }

        public int CompareTo(object obj)
        {
            if (obj is CriCpkDirectory)
            {
                var o = (CriCpkDirectory) obj;

                return DirectoryName.CompareTo(o.DirectoryName);
            }

            throw new ArgumentException("object is not a CriCpkDirectory");
        }

        public void Extract(ref Dictionary<string, FileStream> streamCache, string destinationFolder, bool extractAsRaw)
        {
            var fullDirectoryPath = Path.Combine(destinationFolder, Path.Combine(ParentDirectoryName, DirectoryName));

            // create directory
            if (!Directory.Exists(fullDirectoryPath)) Directory.CreateDirectory(fullDirectoryPath);

            foreach (CriCpkFile f in FileArray) f.Extract(ref streamCache, destinationFolder, extractAsRaw);

            foreach (CriCpkDirectory d in SubDirectoryArray)
                d.Extract(ref streamCache, destinationFolder, extractAsRaw);
        }
    }

    public class CriCpkFile : IFileStructure
    {
        public string ParentDirectoryName { set; get; }
        public string SourceFilePath { set; get; }
        public string FileName { set; get; }
        public long Offset { set; get; }

        public long VolumeBaseOffset { set; get; }
        public long Lba { set; get; }
        public long Size { set; get; }
        public bool IsRaw { set; get; }
        public CdSectorType FileMode { set; get; }
        public int NonRawSectorSize { set; get; }

        public DateTime FileDateTime { set; get; }

        public long ExtractSize { set; get; }

        public int CompareTo(object obj)
        {
            if (obj is CriCpkFile)
            {
                var o = (CriCpkFile) obj;

                return FileName.CompareTo(o.FileName);
            }

            throw new ArgumentException("object is not a CriCpkFile");
        }

        public CriCpkFile(string parentDirectoryName, string sourceFilePath,
            string fileName, long offset, long volumeBaseOffset, long lba, long size,
            long extractSize)
        {
            ParentDirectoryName = parentDirectoryName;
            SourceFilePath = sourceFilePath;
            FileName = fileName;
            Offset = offset;
            Size = size;
            ExtractSize = extractSize;
            FileDateTime = new DateTime();

            VolumeBaseOffset = volumeBaseOffset;
            Lba = lba;
            IsRaw = false;
            NonRawSectorSize = 0;
            FileMode = CdSectorType.Unknown;
        }

        public void Extract(ref Dictionary<string, FileStream> streamCache, string destinationFolder, bool extractAsRaw)
        {
            var destinationFile = Path.Combine(Path.Combine(destinationFolder, ParentDirectoryName), FileName);
            byte[] magicBytes;

            if (!streamCache.ContainsKey(SourceFilePath)) streamCache[SourceFilePath] = File.OpenRead(SourceFilePath);

            // check for CRILAYLA signature since file size is not always reliable, 
            //    this will be a little slower, but hopefully the stream caching will minimize
            //    the impact
            magicBytes = ParseFile.ParseSimpleOffset(streamCache[SourceFilePath], (long) Offset,
                CriCpkArchive.CRILAYLA_SIGNATURE.Length);

            if (ParseFile.CompareSegment(magicBytes, 0, CriCpkArchive.CRILAYLA_SIGNATURE))
                CriCpkArchive.Uncompress(streamCache[SourceFilePath], (long) Offset, Size, destinationFile);
            else
                ParseFile.ExtractChunkToFile64(streamCache[SourceFilePath], (ulong) Offset, (ulong) Size,
                    destinationFile, false, false);
        }
    }
}