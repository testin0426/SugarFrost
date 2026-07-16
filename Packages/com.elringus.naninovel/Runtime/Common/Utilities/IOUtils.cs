using System.IO;
using UnityEngine;

namespace Naninovel
{
    public static class IOUtils
    {
        /// <summary>
        /// Write the text content to a file at specified path and ensures the existing file at the
        /// target path (if any) is not corrupted in the process, in case the app crashes while writing.
        /// </summary>
        public static void WriteTextAtomic (string filePath, string text)
        {
            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, text);
                return;
            }
            var tempPath = $"{filePath}.tmp";
            File.WriteAllText(tempPath, text);
            File.Replace(tempPath, filePath, null);
        }

        /// <summary>
        /// Reads a file with the specified path using async or sync IO depending on the platform.
        /// </summary>
        public static async Awaitable<byte[]> ReadFile (string filePath)
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer) return File.ReadAllBytes(filePath);
            await using var fileStream = File.OpenRead(filePath);
            var fileData = new byte[fileStream.Length];
            // ReSharper disable once MustUseReturnValue
            await fileStream.ReadAsync(fileData, 0, (int)fileStream.Length);
            return fileData;
        }

        /// <summary>
        /// Writes a file's data to the specified path using async or sync IO depending on the platform.
        /// </summary>
        public static async Awaitable WriteFile (string filePath, byte[] fileData)
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer) File.WriteAllBytes(filePath, fileData);
            else
            {
                await using var fileStream = File.OpenWrite(filePath);
                await fileStream.WriteAsync(fileData, 0, fileData.Length);
            }
            WebGLSyncFs();
        }

        /// <summary>
        /// Reads a text file with the specified path using async or sync IO depending on the platform.
        /// </summary>
        public static async Awaitable<string> ReadTextFile (string filePath)
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer) return File.ReadAllText(filePath);
            using var stream = File.OpenText(filePath);
            return await stream.ReadToEndAsync();
        }

        /// <summary>
        /// Writes a text file's data to the specified path using async or sync IO depending on the platform.
        /// </summary>
        public static async Awaitable WriteTextFile (string filePath, string fileText)
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer) File.WriteAllText(filePath, fileText);
            else
            {
                await using var stream = File.CreateText(filePath);
                await stream.WriteAsync(fileText);
            }
            WebGLSyncFs();
        }

        /// <summary>
        /// Deletes file at the specified path. Will insure for correct IO on specific platforms.
        /// </summary>
        public static void DeleteFile (string filePath)
        {
            File.Delete(filePath);
            WebGLSyncFs();
        }

        /// <summary>
        /// Moves a file from <paramref name="sourceFilePath"/> to <paramref name="destFilePath"/>.
        /// Will overwrite the <paramref name="destFilePath"/> in case it exists.
        /// </summary>
        public static void MoveFile (string sourceFilePath, string destFilePath)
        {
            File.Delete(destFilePath);
            File.Move(sourceFilePath, destFilePath);
            WebGLSyncFs();
        }

        /// <summary>
        /// Creates a new directory at the specified path. Will insure for correct IO on specific platforms.
        /// </summary>
        public static void CreateDirectory (string path)
        {
            Directory.CreateDirectory(path);
            WebGLSyncFs();
        }

        /// <summary>
        /// Deletes directory at the specified path. Will insure for correct IO on specific platforms.
        /// </summary>
        public static void DeleteDirectory (string path, bool recursive)
        {
            Directory.Delete(path, recursive);
            WebGLSyncFs();
        }

        /// <summary>
        /// Ensures all the directories in the specified path exist and creates them when missing.
        /// Both file and directory paths are supported, the actual item don't have to exist.
        /// </summary>
        public static void EnsureDirectories (string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) CreateDirectory(dir);
        }

        /// <summary>
        /// Copies the contents of a directory and all the sub-directories under the specified source path
        /// into a directory with the specified dest path. Overwrites the existing files on collision.
        /// </summary>
        public static void CopyDirectory (string sourceDir, string destDir)
        {
            if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                if (File.Exists(destFile)) File.Delete(destFile);
                File.Copy(file, destFile);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
                CopyDirectory(dir, Path.Combine(destDir, Path.GetFileName(dir)));
        }

        /// <summary>
        /// Flush cached file writes to IndexedDB on WebGL.
        /// https://forum.unity.com/threads/webgl-filesystem.294358/#post-1940712
        /// </summary>
        public static void WebGLSyncFs ()
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            WebUtils.SyncFs();
            #endif
        }
    }
}
