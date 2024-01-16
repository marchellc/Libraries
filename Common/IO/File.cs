using Common.Pooling.Pools;

using System.Collections.Generic;
using System.IO;

namespace Common.IO
{
    public class File
    {
        private File(FileInfo info)
            => Info = info;

        public File(string path) : this(new FileInfo(path)) { }

        public FileInfo Info { get; private set; }

        public bool Exists => Info.Exists;

        public void Create()
            => Info.Create().Close();

        public void CreateIfMissing()
        {
            if (!Exists)
                Create();
        }

        public static byte[] Read(string path)
        {
            using (var fs = System.IO.File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (var br = new BinaryReader(fs))
            {
                var list = ListPool<byte>.Shared.Rent();
                var exc = default(EndOfStreamException);

                while (exc != null)
                {
                    try
                    {
                        list.Add(br.ReadByte());
                    }
                    catch (EndOfStreamException endEx)
                    {
                        exc = endEx;
                    }
                }

                return ListPool<byte>.Shared.ToArrayReturn(list);
            }
        }

        public static string[] ReadLines(string path)
        {
            using (var fs = System.IO.File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs))
            {
                var lines = ListPool<string>.Shared.Rent();

                string line = null;

                while ((line = sr.ReadLine()) != null)
                    lines.Add(line);

                return ListPool<string>.Shared.ToArrayReturn(lines);
            }
        }

        public static void Write(string path, string value)
        {
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);

            using (var fs = System.IO.File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (var sw = new StreamWriter(fs))
                sw.Write(value);
        }

        public static void Write(string path, IEnumerable<string> lines)
        {
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);

            using (var fs = System.IO.File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (var sw = new StreamWriter(fs))
            {
                foreach (var line in lines)
                    sw.WriteLine(line);
            }
        }

        public static void Write(string path, IEnumerable<byte> bytes)
        {
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);

            using (var fs = System.IO.File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (var bw = new BinaryWriter(fs))
            {
                foreach (var b in bytes)
                    bw.Write(b);
            }
        }

        public static void Append(string path, string value)
        {
            using (var fs = System.IO.File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (var sw = new StreamWriter(fs))
                sw.Write(value);
        }

        public static void Append(string path, IEnumerable<string> lines)
        {
            using (var fs = System.IO.File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (var sw = new StreamWriter(fs))
            {
                foreach (var line in lines)
                    sw.WriteLine(line);
            }
        }

        public static void Append(string path, IEnumerable<byte> bytes)
        {
            using (var fs = System.IO.File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (var bw = new BinaryWriter(fs))
            {
                foreach (var b in bytes)
                    bw.Write(b);
            }
        }
    }
}