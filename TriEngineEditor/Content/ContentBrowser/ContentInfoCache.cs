﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriEngineEditor.Utilities;

namespace TriEngineEditor.Content
{
    static class ContentInfoCache
    {
        private static readonly object _lock = new object();
        private static string _cacheFilePath = string.Empty;
        private static bool _isDirty;
        private static readonly Dictionary<string, ContentInfo> _contentInfoCache = new Dictionary<string, ContentInfo>();

        public static ContentInfo Add(string file)
        {
            lock (_lock)
            {
                var fileInfo = new FileInfo(file);
                Debug.Assert(!fileInfo.IsDirectory());

                if (!_contentInfoCache.ContainsKey(file) || _contentInfoCache[file].DateModified.IsOlder(fileInfo.LastWriteTime))
                {
                    var info = AssetRegistry.GetAssetInfo(file) ?? Asset.GetAssetInfo(file);
                    Debug.Assert(info != null);
                    _contentInfoCache[file] = new ContentInfo(file, info.Icon);
                    _isDirty = true;
                }

                Debug.Assert(_contentInfoCache.ContainsKey(file));
                return _contentInfoCache[file];
            }
        }

        public static void Reset(string projectPath)
        {
            lock (_lock)
            {
                if (!string.IsNullOrEmpty(_cacheFilePath) && _isDirty)
                {
                    SaveInfoCache();
                    _cacheFilePath = string.Empty;
                    _contentInfoCache.Clear();
                    _isDirty = false;
                }

                if (!string.IsNullOrEmpty(projectPath))
                {
                    Debug.Assert(Directory.Exists(projectPath));
                    _cacheFilePath = $@"{projectPath}.TriEngine\ContentInfoCache.bin";
                    LoadInfoCache();
                }
            }
        }

        public static void Save() => Reset(string.Empty);

        private static void SaveInfoCache()
        {
            try
            {
                using var writer = new BinaryWriter(File.Open(_cacheFilePath, FileMode.Create, FileAccess.Write));
                writer.Write(_contentInfoCache.Keys.Count);
                foreach (var key in _contentInfoCache.Keys)
                {
                    var info = _contentInfoCache[key];

                    writer.Write(key);
                    writer.Write(info.DateModified.ToBinary());
                    writer.Write(info.Icon.Length);
                    writer.Write(info.Icon);
                }
                _isDirty = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Logger.Log(MessageType.Warning, "Failed to save content info cache.");
            }
        }

        private static void LoadInfoCache()
        {
            if (!File.Exists(_cacheFilePath)) return;

            try
            {
                using var reader = new BinaryReader(File.Open(_cacheFilePath, FileMode.Open, FileAccess.Read));
                var numEntries = reader.ReadInt32();
                _contentInfoCache.Clear();

                for (var i = 0; i < numEntries; i++)
                {
                    var assetFile = reader.ReadString();
                    var date = DateTime.FromBinary(reader.ReadInt64());
                    var iconSize = reader.ReadInt32();
                    var icon = reader.ReadBytes(iconSize);

                    // Cache only the files that still exist.
                    if (File.Exists(assetFile))
                    {
                        _contentInfoCache[assetFile] = new ContentInfo(assetFile, icon, null, date);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Logger.Log(MessageType.Warning, "Failed to load content info cache. Cache will be rebuilt.");
                _contentInfoCache.Clear();
            }
        }
    }
}
