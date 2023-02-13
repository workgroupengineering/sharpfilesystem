﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.IO.Compression;

namespace SharpFileSystem.FileSystems
{
    public class NetZipArchiveFileSystem : IFileSystem
    {
        public ZipArchive ZipArchive { get; private set; }

        public bool IsReadOnly => false;

        public static NetZipArchiveFileSystem Open(Stream s)
        {
            return new NetZipArchiveFileSystem(new ZipArchive(s, ZipArchiveMode.Update, true));
        }
        
        public static NetZipArchiveFileSystem OpenReadOnly(Stream s)
        {
            return new NetZipArchiveFileSystem(new ZipArchive(s, ZipArchiveMode.Read, true));
        }

        public static NetZipArchiveFileSystem Create(Stream s)
        {
            return new NetZipArchiveFileSystem(new ZipArchive(s, ZipArchiveMode.Create, true));
        }

        private NetZipArchiveFileSystem(ZipArchive archive)
        {
            ZipArchive = archive;
        }
        public void Dispose()
        {
            ZipArchive.Dispose();
        }

        protected IEnumerable<ZipArchiveEntry> GetZipEntries()
        {
            return ZipArchive.Entries;
        }
        protected FileSystemPath ToPath(ZipArchiveEntry entry)
        {
            return FileSystemPath.Parse(FileSystemPath.DirectorySeparator + entry.FullName);
        }
        protected string ToEntryPath(FileSystemPath path)
        {
            // Remove heading '/' from path.
            return path.Path.TrimStart(FileSystemPath.DirectorySeparator);
        }

        protected ZipArchiveEntry ToEntry(FileSystemPath path)
        {
            return ZipArchive.GetEntry(ToEntryPath(path));
        }
        public ICollection<FileSystemPath> GetEntities(FileSystemPath path)
        {
            return GetZipEntries().Select(ToPath).Where(path.IsParentOf)
                .Select(entryPath => entryPath.ParentPath == path
                   ? entryPath
                   : path.AppendDirectory(entryPath.RemoveParent(path).GetDirectorySegments()[0])
                    )
                .Distinct()
                .ToList();
        }

        public bool Exists(FileSystemPath path)
        {
            if (path.IsFile)
                return ToEntry(path) != null;
            return GetZipEntries()
                .Select(ToPath)
                .Any(entryPath => entryPath.IsChildOf(path) || entryPath.Equals(path));
        }

        public Stream CreateFile(FileSystemPath path)
        {
            if (ZipArchive.Mode == ZipArchiveMode.Read)
                throw new InvalidOperationException("This is a read-only filesystem.");

            var zae = ZipArchive.CreateEntry(ToEntryPath(path));
            return zae.Open();
        }

        public Stream OpenFile(FileSystemPath path, FileAccess access)
        {
            if (access != FileAccess.Read)
                throw new InvalidOperationException("This is a read-only filesystem.");

            var zae = ZipArchive.GetEntry(ToEntryPath(path));
            return zae.Open();
        }

        public void CreateDirectory(FileSystemPath path)
        {
            if (ZipArchive.Mode == ZipArchiveMode.Read)
                throw new InvalidOperationException("This is a read-only filesystem.");

            ZipArchive.CreateEntry(ToEntryPath(path));
        }

        public void Delete(FileSystemPath path)
        {
            if (ZipArchive.Mode == ZipArchiveMode.Read)
                throw new InvalidOperationException("This is a read-only filesystem.");

            var zae = ZipArchive.GetEntry(ToEntryPath(path));
            zae.Delete();
        }
    }
}
