// <copyright file="ChecksumUtil.cs" company="N/A">
// Copyright (c) 2008 All Rights Reserved
// </copyright>
// <author></author>
// <email></email>
// <date></date>
// <summary>Contains the ChecksumUtil class.</summary>

using System.IO;
using System.Security.Cryptography;

namespace VGMToolbox.util
{
    /// <summary>
    /// Class containing static functions related to checksum generation.
    /// </summary>
    public static class ChecksumUtil
    {
        /// <summary>
        /// Get the MD5 checksum of the input stream.
        /// </summary>
        /// <param name="stream">File Stream for which to generate the checksum.</param>
        /// <returns>String containing the hexidecimal representation of the MD5 of the input stream.</returns>
        public static string GetMd5OfFullFile(FileStream stream)
        {
            var hashMd5 = new MD5CryptoServiceProvider();

            stream.Seek(0, SeekOrigin.Begin);
            hashMd5.ComputeHash(stream);
            return ParseFile.ByteArrayToString(hashMd5.Hash);
        }

        public static byte[] GetSha1(byte[] dataBlock)
        {
            var sha1Hash = new SHA1CryptoServiceProvider();
            sha1Hash.ComputeHash(dataBlock);
            return sha1Hash.Hash;
        }

        /// <summary>
        /// Get the SHA1 checksum of the input stream.
        /// </summary>
        /// <param name="stream">File Stream for which to generate the checksum.</param>
        /// <returns>String containing the hexidecimal representation of the SHA1 of the input stream.</returns>
        public static string GetSha1OfFullFile(FileStream stream)
        {
            var sha1Hash = new SHA1CryptoServiceProvider();

            stream.Seek(0, SeekOrigin.Begin);
            sha1Hash.ComputeHash(stream);
            return ParseFile.ByteArrayToString(sha1Hash.Hash);
        }

        /// <summary>
        /// Get the SHA-512 checksum of the input stream.
        /// </summary>
        /// <param name="stream">File Stream for which to generate the checksum.</param>
        /// <returns>String containing the hexidecimal representation of the SHA-512 of the input stream.</returns>
        public static string GetSha512OfFullFile(FileStream stream)
        {
            var sha512 = new SHA512CryptoServiceProvider();

            stream.Seek(0, SeekOrigin.Begin);
            sha512.ComputeHash(stream);
            return ParseFile.ByteArrayToString(sha512.Hash);
        }
    }
}