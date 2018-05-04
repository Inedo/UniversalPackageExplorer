using Inedo.UPack;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace UniversalPackageExplorer.UPack
{
    public sealed class UniversalPackage : INotifyPropertyChanged, IDisposable
    {
        private const string DefaultUPackJson = @"{""name"":""unnamed"",""version"":""1.0.0""}";

        public UniversalPackage() : this(CreateTempFile())
        {
            Debug.Assert(!this.Dirty);
            var file = this.Metadata.CreateFileAsync("upack.json").Result;
            using (var stream = file.OpenWrite())
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.WriteLine(DefaultUPackJson);
            }
            this.Dirty = false;
        }

        private UniversalPackage(FileStream tempFile)
        {
            this.tempFile = tempFile;
            this.Files = new FileCollection(this, false);
            this.Metadata = new FileCollection(this, true);
            this.manifest = new Lazy<UniversalPackageManifest>(() =>
            {
                var manifest = this.ReadManifestAsync().Result;
                manifest.PropertyChanged += async (s, e) =>
                {
                    await this.WriteManifestAsync(this.manifest.Value);
                };
                manifest.UnknownFields.CollectionChanged += async (s, e) =>
                {
                    await this.WriteManifestAsync(this.manifest.Value);
                };
                return manifest;
            });
        }

        internal static FileStream CreateTempFile()
        {
            return new FileStream(Path.GetTempFileName(), FileMode.Open, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.DeleteOnClose | FileOptions.RandomAccess);
        }

        public static async Task<UniversalPackage> CreateAsync(Stream stream)
        {
            var tempFile = CreateTempFile();

            try
            {
                await stream.CopyToAsync(tempFile);

                tempFile.Position = 0;
                byte[] hash;
                using (var sha1 = SHA1.Create())
                {
                    hash = sha1.ComputeHash(tempFile);
                }

                return new UniversalPackage(tempFile) { OriginalHash = new HexString(hash) };
            }
            catch
            {
                try { tempFile.Dispose(); } catch { }
                throw;
            }
        }

        public static async Task<UniversalPackage> CreateAsync(string fullName)
        {
            using (var stream = new FileStream(fullName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                return await CreateAsync(fullName, stream);
            }
        }

        public static async Task<UniversalPackage> CreateAsync(string fullName, Stream stream)
        {
            var tempFile = CreateTempFile();

            try
            {
                await stream.CopyToAsync(tempFile);

                tempFile.Position = 0;
                byte[] hash;
                using (var sha1 = SHA1.Create())
                {
                    hash = sha1.ComputeHash(tempFile);
                }

                return new UniversalPackage(tempFile) { FullName = fullName, OriginalHash = new HexString(hash) };
            }
            catch
            {
                try { tempFile.Dispose(); } catch { }
                throw;
            }
        }

        public string WindowTitlePrefix
        {
            get
            {
                return (this.Dirty ? "*" : "") + (this.IsTemporary ? "(unsaved)" : Path.GetFileName(this.FullName));
            }
        }

        private string fullName;
        public string FullName
        {
            get => this.fullName;
            set
            {
                var wasTemp = this.IsTemporary;

                this.fullName = value;

                if (wasTemp != this.IsTemporary)
                {
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsTemporary)));
                }

                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FullName)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WindowTitlePrefix)));
            }
        }
        private bool dirty = false;
        public bool Dirty
        {
            get => this.dirty;
            internal set
            {
                if (value != this.dirty)
                {
                    this.dirty = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Dirty)));
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WindowTitlePrefix)));
                }
            }
        }
        internal HexString? OriginalHash { get; private set; }
        private Lazy<UniversalPackageManifest> manifest;
        public UniversalPackageManifest Manifest => this.manifest.Value;
        public FileCollection Metadata { get; }
        public FileCollection Files { get; }

        public bool IsTemporary => string.IsNullOrEmpty(this.FullName) || Path.GetFullPath(this.FullName).StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

        private readonly FileStream tempFile;
        internal ZipArchive OpenRead()
        {
            if (this.tempFile.Length == 0)
            {
                return new ZipArchive(new MemoryStream(), ZipArchiveMode.Update, false);
            }

            this.tempFile.Position = 0;
            return new ZipArchive(this.tempFile, ZipArchiveMode.Read, true);
        }
        internal ZipArchive OpenWrite()
        {
            this.tempFile.Position = 0;
            return new ZipArchive(this.tempFile, ZipArchiveMode.Update, true);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task SaveAsync()
        {
            using (var output = new FileStream(this.FullName, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                this.tempFile.Position = 0;
                await this.tempFile.CopyToAsync(output);
            }

            this.Dirty = false;
        }

        internal async Task<FileStream> CopyToTempAsync()
        {
            var tempFile = CreateTempFile();
            try
            {
                this.tempFile.Position = 0;
                await this.tempFile.CopyToAsync(tempFile);
                tempFile.Position = 0;
            }
            catch
            {
                try { tempFile.Dispose(); } catch { }
                throw;
            }

            return tempFile;
        }

        public void Dispose()
        {
            this.tempFile.Dispose();
        }

        public async Task<UniversalPackageManifest> ReadManifestAsync()
        {
            if (!this.Metadata.TryGetValue("upack.json", out var file))
            {
                file = await this.Metadata.CreateFileAsync("upack.json");
            }

            if (file.Length == 0)
            {
                using (var stream = file.OpenWrite())
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    await writer.WriteLineAsync(DefaultUPackJson);
                }
            }

            using (var stream = file.OpenRead())
            using (var reader = new StreamReader(stream, new UTF8Encoding(false)))
            {
                return JsonConvert.DeserializeObject<UniversalPackageManifest>(await reader.ReadToEndAsync());
            }
        }

        public async Task WriteManifestAsync(UniversalPackageManifest manifest)
        {
            if (!this.Metadata.TryGetValue("upack.json", out var file))
            {
                file = await this.Metadata.CreateFileAsync("upack.json");
            }

            using (var stream = file.OpenWrite())
            {
                stream.SetLength(0);
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, true))
                {
                    await writer.WriteLineAsync(JsonConvert.SerializeObject(manifest));
                }
            }
        }

        public sealed class FileCollection : IReadOnlyList<UniversalPackageFile>, INotifyCollectionChanged
        {
            internal readonly UniversalPackage package;
            private readonly bool isMetadata;
            private readonly SortedList<string, UniversalPackageFile> files;
            public SubTreeCollection Root { get; }
            internal string Prefix => this.isMetadata ? "" : "package/";

            internal FileCollection(UniversalPackage package, bool isMetadata)
            {
                this.package = package;
                this.isMetadata = isMetadata;

                using (var zip = this.package.OpenRead())
                {
                    var files = from entry in zip.Entries
                                let name = CleanPath(entry.FullName)
                                where name.StartsWith("package/", StringComparison.OrdinalIgnoreCase) != this.isMetadata
                                select new UniversalPackageFile(this, name.Substring(this.Prefix.Length));
                    this.files = new SortedList<string, UniversalPackageFile>(files.ToDictionary(f => f.FullName), StringComparer.OrdinalIgnoreCase);
                }

                var directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (!this.isMetadata)
                {
                    directories.Add(string.Empty);
                }
                foreach (var file in this.files.Values)
                {
                    var i = file.FullName.LastIndexOf('/');
                    while (i != -1)
                    {
                        directories.Add(file.FullName.Substring(0, i) + "/");
                        i = file.FullName.LastIndexOf('/', i - 1);
                    }
                }

                foreach (var dir in directories)
                {
                    if (!this.ContainsKey(dir))
                    {
                        this.files.Add(dir, new UniversalPackageFile(this, dir));
                    }
                }

                this.files.Remove(string.Empty);

                this.Root = new SubTreeCollection(this, string.Empty);
            }

            public UniversalPackageFile this[int index]
            {
                get
                {
                    return this.files.Values[index];
                }
            }
            public UniversalPackageFile this[string key]
            {
                get
                {
                    if (!this.TryGetValue(key, out var file))
                    {
                        throw new KeyNotFoundException();
                    }

                    return file;
                }
            }
            internal UniversalPackageFile GetFileOrDirectory(string key)
            {
                key = CleanPath(key).TrimEnd('/');

                if (this.TryGetValue(key, out var file))
                {
                    return file;
                }

                if (this.TryGetValue(key + "/", out var directory))
                {
                    return directory;
                }

                throw new KeyNotFoundException();
            }
            public int Count => this.files.Count;

            public event NotifyCollectionChangedEventHandler CollectionChanged;

            public bool ContainsKey(string key)
            {
                return this.TryGetValue(key, out var rubbish);
            }

            public IEnumerator<UniversalPackageFile> GetEnumerator() => this.files.Values.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

            public bool TryGetValue(string key, out UniversalPackageFile value)
            {
                if (key == null)
                {
                    value = null;
                    return false;
                }

                return this.files.TryGetValue(CleanPath(key), out value);
            }

            private static readonly Regex CleanSlashesRegex = new Regex(@"\s*[\\/]+\s*", RegexOptions.Compiled);
            internal static string CleanPath(string fullName)
            {
                if (fullName == null)
                {
                    throw new ArgumentNullException(nameof(fullName));
                }

                if (fullName == string.Empty)
                {
                    return string.Empty;
                }

                fullName = CleanSlashesRegex.Replace(fullName, "/").TrimStart('/');

                if (string.IsNullOrEmpty(fullName))
                {
                    throw new ArgumentNullException(nameof(fullName));
                }

                return fullName;
            }

            private void OnCollectionChanged(NotifyCollectionChangedEventArgs e, string movedFrom = null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    this.package.Dirty = true;
                    this.CollectionChanged?.Invoke(this, e);
                    this.Root.OnCollectionChanged(e, movedFrom);
                    foreach (var f in this.files.Values)
                    {
                        f.Children?.OnCollectionChanged(e, movedFrom);
                    }
                });
            }

            private void AddWithParents(UniversalPackageFile file)
            {
                this.files.Add(file.FullName, file);

                var added = new List<UniversalPackageFile> { file };
                int firstIndex = this.files.IndexOfKey(file.FullName);

                var zip = new Lazy<ZipArchive>(() => this.package.OpenWrite());
                try
                {
                    int lastSlash = file.FullName.LastIndexOf('/', file.FullName.Length - 2);
                    while (lastSlash != -1)
                    {
                        var parentName = file.FullName.Substring(0, lastSlash + 1);
                        if (this.files.ContainsKey(parentName))
                        {
                            break;
                        }

                        zip.Value.CreateEntry(this.Prefix + parentName, CompressionLevel.NoCompression);

                        var parent = new UniversalPackageFile(this, parentName);
                        this.files.Add(parentName, parent);
                        added.Add(parent);
                        lastSlash = file.FullName.LastIndexOf('/', lastSlash - 1);
                    }
                }
                finally
                {
                    if (zip.IsValueCreated)
                    {
                        zip.Value.Dispose();
                    }
                }

                added.Reverse();
                this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, added, firstIndex));
            }

            internal void CheckPath(string fullName)
            {
                if (!fullName.EndsWith("/", StringComparison.OrdinalIgnoreCase))
                {
                    if (this.ContainsKey(fullName + "/"))
                    {
                        throw new ArgumentException("Cannot create a file using the same name as a directory.", nameof(fullName));
                    }
                }

                int lastSlash = fullName.LastIndexOf('/');
                while (lastSlash != -1)
                {
                    if (this.ContainsKey(fullName.Substring(0, lastSlash)))
                    {
                        throw new ArgumentException("Cannot create a directory using the same name as a file.", nameof(fullName));
                    }

                    lastSlash = fullName.LastIndexOf('/', lastSlash - 1);
                }
            }

            public Task<UniversalPackageFile> CreateFileAsync(string fullName)
            {
                fullName = CleanPath(fullName);

                if (this.isMetadata && fullName.StartsWith("package/", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("Cannot create a metadata file with a name starting with 'package/'.", nameof(fullName));
                }

                if (fullName.EndsWith("/", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("Cannot create a file using a directory name.", nameof(fullName));
                }

                if (this.ContainsKey(fullName))
                {
                    throw new ArgumentException("A file with that name already exists.", nameof(fullName));
                }

                this.CheckPath(fullName);

                if (this.isMetadata && fullName.Equals("package", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("Cannot create a metadata file with the path 'package'.", nameof(fullName));
                }

                using (var zip = this.package.OpenWrite())
                {
                    zip.CreateEntry(this.Prefix + fullName, CompressionLevel.Optimal);
                }

                var file = new UniversalPackageFile(this, fullName);
                this.AddWithParents(file);

                return Task.FromResult(file);
            }

            public Task<UniversalPackageFile> CreateDirectoryAsync(string fullName)
            {
                fullName = CleanPath(fullName + "/");

                if (this.TryGetValue(fullName, out var directory))
                {
                    return Task.FromResult(directory);
                }

                this.CheckPath(fullName);

                if (this.isMetadata && fullName.StartsWith("package/", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("Cannot create a metadata directory with a name starting with 'package/'.", nameof(fullName));
                }

                using (var zip = this.package.OpenWrite())
                {
                    zip.CreateEntry(this.Prefix + fullName, CompressionLevel.NoCompression);
                }

                directory = new UniversalPackageFile(this, fullName);
                this.AddWithParents(directory);

                return Task.FromResult(directory);
            }

            internal async Task RenameAsync(UniversalPackageFile file, string newName)
            {
                newName = CleanPath(newName);

                if (this.isMetadata && newName.StartsWith("package/", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("Cannot create a metadata file with a name starting with 'package/'.", nameof(newName));
                }

                if (file.FullName.EndsWith("/", StringComparison.OrdinalIgnoreCase))
                {
                    if (!newName.EndsWith("/", StringComparison.OrdinalIgnoreCase))
                    {
                        newName += "/";
                    }

                    if (this.ContainsKey(newName))
                    {
                        throw new ArgumentException("A directory with that name already exists.", nameof(newName));
                    }

                    await this.RenameTreeAsync(file.FullName, newName);
                    return;
                }

                if (newName.EndsWith("/", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("Cannot rename a file to a directory.", nameof(newName));
                }

                if (this.ContainsKey(newName))
                {
                    throw new ArgumentException("A file with that name already exists.", nameof(newName));
                }

                var oldName = file.FullName;
                int oldIndex = this.files.IndexOfKey(oldName);
                this.files.RemoveAt(oldIndex);

                using (var zip = this.package.OpenWrite())
                {
                    await file.RenameToAsync(newName, zip.GetEntry(this.Prefix + oldName), zip.CreateEntry(this.Prefix + newName, CompressionLevel.Optimal));
                }

                this.files.Add(newName, file);

                int newIndex = this.files.IndexOfKey(newName);
                this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, file, newIndex, oldIndex), oldName);
            }

            private async Task RenameTreeAsync(string oldName, string newName)
            {
                int oldIndex = this.files.IndexOfKey(oldName);
                var moved = this.files.Values.Skip(oldIndex).TakeWhile(f => f.FullName.StartsWith(oldName, StringComparison.OrdinalIgnoreCase)).ToArray();

                foreach (var f in moved)
                {
                    this.files.RemoveAt(oldIndex);
                }

                using (var zip = this.package.OpenWrite())
                {
                    foreach (var f in moved)
                    {
                        var fullName = newName + f.FullName.Substring(oldName.Length);
                        await f.RenameToAsync(fullName, zip.GetEntry(this.Prefix + f.FullName), zip.CreateEntry(this.Prefix + fullName, fullName.EndsWith("/", StringComparison.OrdinalIgnoreCase) ? CompressionLevel.NoCompression : CompressionLevel.Optimal));
                        this.files.Add(fullName, f);
                    }
                }

                int newIndex = this.files.IndexOfKey(newName);
                this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, moved, newIndex, oldIndex), oldName);
            }

            internal Task DeleteAsync(UniversalPackageFile file)
            {
                if (file.FullName.EndsWith("/", StringComparison.OrdinalIgnoreCase))
                {
                    return this.DeleteTreeAsync(file.FullName);
                }

                var index = this.files.IndexOfKey(file.FullName);

                this.files.RemoveAt(index);
                file.DeleteInternal();

                this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, file, index));

                return Task.FromResult<object>(null);
            }

            private Task DeleteTreeAsync(string prefix)
            {
                int startIndex = this.files.IndexOfKey(prefix);
                var removed = this.files.Values.Skip(startIndex).TakeWhile(f => f.FullName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToArray();

                foreach (var f in removed)
                {
                    this.files.RemoveAt(startIndex);
                    f.DeleteInternal();
                }

                this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removed, startIndex));

                return Task.FromResult<object>(null);
            }

            public sealed class SubTreeCollection : IReadOnlyList<UniversalPackageFile>, INotifyCollectionChanged
            {
                private readonly FileCollection collection;
                private readonly string prefix;

                internal SubTreeCollection(FileCollection collection, string prefix)
                {
                    this.collection = collection;
                    this.prefix = prefix;
                }

                public event NotifyCollectionChangedEventHandler CollectionChanged;
                internal void OnCollectionChanged(NotifyCollectionChangedEventArgs e, string movedFrom)
                {
                    switch (e.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
                            add();
                            break;
                        case NotifyCollectionChangedAction.Remove:
                            remove();
                            break;
                        case NotifyCollectionChangedAction.Replace:
                            throw new NotImplementedException();
                        case NotifyCollectionChangedAction.Move:
                            move();
                            break;
                        case NotifyCollectionChangedAction.Reset:
                            throw new NotImplementedException();
                    }

                    void add()
                    {
                        var added = e.NewItems.Cast<UniversalPackageFile>().Where(IsDirectChild);
                        foreach (var a in added)
                        {
                            this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, a, indexOf(a)));
                        }
                    }

                    void remove()
                    {
                        var removed = e.OldItems.Cast<UniversalPackageFile>().Where(IsDirectChild);
                        foreach (var r in removed)
                        {
                            this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, r, indexOfName(r.FullName, e.OldItems[0])));
                        }
                    }

                    void move()
                    {
                        if (e.NewItems.Count != 1 || movedFrom == null)
                        {
                            if (e.NewItems.Cast<UniversalPackageFile>().Any(IsDirectChild) || movedFrom == null || IsDirectChild(movedFrom))
                            {
                                // TODO
                                this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                            }
                        }
                        else if (IsDirectChild(movedFrom) && IsDirectChild((UniversalPackageFile)e.NewItems[0]))
                        {
                            this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, e.NewItems[0], indexOf(e.NewItems[0]), indexOfName(movedFrom, e.NewItems[0])));
                        }
                        else if (IsDirectChild(movedFrom))
                        {
                            this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, e.NewItems[0], indexOfName(movedFrom, e.NewItems[0])));
                        }
                        else if (IsDirectChild((UniversalPackageFile)e.NewItems[0]))
                        {
                            this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, e.NewItems[0], indexOf(e.NewItems[0])));
                        }
                    }

                    int indexOfName(string fullName, object item) => this.TakeWhile(f => StringComparer.OrdinalIgnoreCase.Compare(f.FullName, fullName) < 0).Except(new[] { item }).Count();
                    int indexOf(object item) => this.TakeWhile(f => f != item).Except(new[] { item }).Count();
                }

                private IEnumerable<UniversalPackageFile> GetEnumerable() => this.collection.files.Values.Where(IsDirectChild);

                private bool IsDirectChild(UniversalPackageFile file)
                {
                    return IsDirectChild(file.FullName);
                }

                private bool IsDirectChild(string fullName)
                {
                    if (!fullName.StartsWith(this.prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    if (fullName.Length <= this.prefix.Length)
                    {
                        return false;
                    }

                    int i = fullName.IndexOf('/', this.prefix.Length);
                    return i == -1 || i == fullName.Length - 1;
                }

                public UniversalPackageFile this[int index] => this.GetEnumerable().ElementAt(index);
                public UniversalPackageFile this[string name] => this.collection.GetFileOrDirectory(this.prefix + name);
                public int Count => this.GetEnumerable().Count();

                public IEnumerator<UniversalPackageFile> GetEnumerator() => this.GetEnumerable().GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
            }
        }
    }
}