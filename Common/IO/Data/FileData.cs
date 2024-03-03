namespace Common.IO.Data
{
    public struct FileData : IData
    {
        public string Name;
        public string Extension;
        public string FullName;
        public string Path;

        public byte[] Data;

        public FileData(string filePath)
        {
            Name = System.IO.Path.GetFileNameWithoutExtension(filePath);
            Extension = System.IO.Path.GetExtension(filePath);
            Path = System.IO.Path.GetFullPath(filePath);
            Data = File.Read(filePath);
            FullName = $"{Name}.{Extension}";
        }

        public FileData(string name, string extension, byte[] data)
        {
            Name = name;
            Extension = extension;
            Data = data;
            FullName = $"{name}.{extension}";
            Path = FullName;
        }

        public void Deserialize(DataReader reader)
        {
            Data = reader.ReadBytes();
            Name = reader.ReadString();
            Path = reader.ReadString();
            FullName = reader.ReadString();
            Extension = reader.ReadString();
        }

        public void Serialize(DataWriter writer)
        {
            writer.WriteBytes(Data);
            writer.WriteString(Name);
            writer.WriteString(Path);
            writer.WriteString(FullName);
            writer.WriteString(Extension);
        }
    }
}