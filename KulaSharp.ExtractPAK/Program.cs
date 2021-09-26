using System;
using System.IO;
using System.Collections.Generic;

namespace KulaSharp.ExtractPAK
{
    class Program
    {
        struct CompressedFile
        {
            public int offset, size, nameOffset;
        }

        static void Main(string[] args)
        {
            if (args.Length < 1) Environment.Exit(0);
            List<CompressedFile> compressedFiles = new List<CompressedFile>();
            byte[] pakFile = File.ReadAllBytes(args[0]);
            int currentOffset = 0, compressedFileCount = ReadBytes(pakFile, currentOffset, 4);
            for (int i = 0; i < compressedFileCount; i++)
            {
                CompressedFile currentFile = new CompressedFile();
                currentOffset += 4;
                currentFile.offset = ReadBytes(pakFile, currentOffset, 4);
                currentOffset += 4;
                currentFile.size = ReadBytes(pakFile, currentOffset, 4);
                compressedFiles.Add(currentFile);
            }

            for (int i = 0; i < compressedFileCount; i++)
            {
                CompressedFile currentFile = compressedFiles[i];
                currentOffset += 4;
                currentFile.nameOffset = ReadBytes(pakFile, currentOffset, 4);
                compressedFiles[i] = currentFile;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(args[0]) + '\\' + Path.GetFileName(args[0]).Split('.')[0]);

            for (int i = 0; i < compressedFileCount; i++)
            {
                CompressedFile currentFile = compressedFiles[i];
                currentOffset = currentFile.nameOffset;
                string fileName = "";
                while (true)
                {
                    byte character = pakFile[currentOffset];
                    if (character == 10 || character == 0) break;
                    else fileName += Convert.ToChar(character);
                    currentOffset += 1;
                }
                byte[] compressedFileBytes = new byte[currentFile.size];
                currentOffset = currentFile.offset;
                for (int j = 0; j < currentFile.size; j++)
                {
                    compressedFileBytes[j] = pakFile[currentOffset];
                    currentOffset++;
                }
                File.WriteAllBytes(Path.GetDirectoryName(args[0]) + '\\' + Path.GetFileName(args[0]).Split('.')[0] + '\\' + fileName, Ionic.Zlib.ZlibStream.UncompressBuffer(compressedFileBytes));
            }
        }

        public static int ReadBytes(byte[] buffer, int offset, int size)
        {
            byte[] bytes = new byte[size];
            for (int i = 0; i < size; i++) bytes[i] = buffer[offset + i];
            return BitConverter.ToInt32(bytes, 0);
        }
    }
}
