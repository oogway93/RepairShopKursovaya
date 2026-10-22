using System;
using System.IO;
using System.Xml.Serialization;
using RepairShop.Core.Models;

namespace RepairShop.Core.Storage
{
    /// <summary>Абстракция хранилища данных (шаблон «Репозиторий»).</summary>
    public interface IDataStore
    {
        bool Exists { get; }
        ShopData Load();
        void Save(ShopData data);
    }

    /// <summary>Хранение данных в XML-файле.</summary>
    public class XmlDataStore : IDataStore
    {
        private readonly string _path;
        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(ShopData));

        public XmlDataStore(string path)
        {
            _path = path;
        }

        public bool Exists
        {
            get { return File.Exists(_path); }
        }

        public ShopData Load()
        {
            if (!File.Exists(_path))
                return new ShopData();
            using (var stream = File.OpenRead(_path))
            {
                return (ShopData)Serializer.Deserialize(stream);
            }
        }

        public void Save(ShopData data)
        {
            string dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            // Сначала пишем во временный файл, чтобы не потерять данные при сбое.
            string tmp = _path + ".tmp";
            using (var stream = File.Create(tmp))
            {
                Serializer.Serialize(stream, data);
            }
            if (File.Exists(_path))
                File.Delete(_path);
            File.Move(tmp, _path);
        }
    }

    /// <summary>Хранилище в памяти (для модульных тестов).</summary>
    public class MemoryDataStore : IDataStore
    {
        private ShopData _data;

        public bool Exists
        {
            get { return _data != null; }
        }

        public ShopData Load()
        {
            return _data ?? new ShopData();
        }

        public void Save(ShopData data)
        {
            _data = data;
        }
    }
}
