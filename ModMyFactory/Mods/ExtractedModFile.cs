﻿using System;
using System.IO;
using System.Threading.Tasks;
using ModMyFactory.BaseTypes;
using ModMyFactory.IO;
using SharpCompress.Common;
using SharpCompress.Writers;
using SharpCompress.Writers.Zip;

namespace ModMyFactory.Mods
{
    public class ExtractedModFile : IModFile
    {
        DirectoryInfo _directory;

        public string FilePath => _directory.FullName;

        public ModInfo Info { get; }

        public Stream Thumbnail { get; }

        public bool IsExtracted => true;

        public void Delete()
        {
            Thumbnail.Dispose();
            _directory.Delete(true);
        }

        public async Task<IModFile> CopyToAsync(string destination)
        {
            var fullPath = Path.Combine(destination, _directory.Name);
            var newDir = await _directory.CopyToAsync(fullPath);
            var newThumbnail = ModFile.LoadThumbnail(newDir);
            return new ExtractedModFile(newDir, Info, newThumbnail);
        }

        public async Task MoveToAsync(string destination)
        {
            var fullPath = Path.Combine(destination, _directory.Name);
            _directory = await _directory.MoveToAsync(fullPath);
        }

        void PopulateZipArchive(ZipWriter writer, DirectoryInfo directory, string path)
        {
            foreach (var file in directory.EnumerateFiles())
                writer.Write(path + "/" + file.Name, file);

            foreach (var subDir in directory.EnumerateDirectories())
                PopulateZipArchive(writer, subDir, path + "/" + subDir.Name);
        }

        /// <summary>
        /// Packs this mod file.
        /// </summary>
        /// <param name="destination">The path to pack this mod file at, excluding the mod files name itself.</param>
        public async Task<ZippedModFile> PackAsync(string destination)
        {
            var newFile = new FileInfo(Path.Combine(destination, _directory.Name + ".zip"));
            if (!newFile.Directory.Exists) newFile.Directory.Create();
            
            await Task.Run(() =>
            {
                using (var fs = newFile.OpenWrite())
                {
                    using (var writer = new ZipWriter(fs, new ZipWriterOptions(CompressionType.Deflate)))
                        PopulateZipArchive(writer, _directory, _directory.Name);
                }
            });
            
            var newThumbnail = await ModFile.CopyThumbnailAsync(Thumbnail);
            return new ZippedModFile(newFile, Info, newThumbnail);
        }

        /// <summary>
        /// Packs this mod file in the same location.
        /// </summary>
        public async Task<ZippedModFile> PackAsync() => await PackAsync(_directory.Parent.FullName);

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                Thumbnail.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ExtractedModFile()
        {
            Dispose(false);
        }

        internal ExtractedModFile(DirectoryInfo directory, ModInfo info, Stream thumbnail)
        {
            _directory = directory;
            Info = info;
            Thumbnail = thumbnail;
        }


        /// <summary>
        /// Tries to load a mod file.
        /// </summary>
        /// <param name="directory">The directory to load.</param>
        public static async Task<(bool, ExtractedModFile)> TryLoadAsync(DirectoryInfo directory)
        {
            var infoFile = new FileInfo(Path.Combine(directory.FullName, "info.json"));
            if (!infoFile.Exists) return (false, null);

            ModInfo info;
            try
            {
                info = await ModInfo.FromFileAsync(infoFile);
            }
            catch
            {
                return (false, null);
            }

            var thumbnail = ModFile.LoadThumbnail(directory);
            return (true, new ExtractedModFile(directory, info, thumbnail));
        }

        /// <summary>
        /// Tries to load a mod file.
        /// </summary>
        /// <param name="path">The path to a directory to load.</param>
        public static async Task<(bool, ExtractedModFile)> TryLoadAsync(string path)
        {
            var dir = new DirectoryInfo(path);
            return await TryLoadAsync(dir);
        }

        /// <summary>
        /// Loads a mod file.
        /// </summary>
        /// <param name="directory">The directory to load.</param>
        public static async Task<ExtractedModFile> LoadAsync(DirectoryInfo directory)
        {
            (bool success, var result) = await TryLoadAsync(directory);
            if (!success) throw new InvalidPathException("The path does not point to a valid mod file.");
            return result;
        }

        /// <summary>
        /// Loads a mod file.
        /// </summary>
        /// <param name="path">The path to a directory to load.</param>
        public static async Task<ExtractedModFile> LoadAsync(string path)
        {
            var dir = new DirectoryInfo(path);
            return await LoadAsync(dir);
        }
    }
}