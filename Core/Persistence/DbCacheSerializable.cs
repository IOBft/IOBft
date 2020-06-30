using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using LevelDB;

namespace ETModel
{
    public interface ISerializable
    {
        void Serialize(BinaryWriter writer);
        void Deserialize(BinaryReader reader);
    }

    // �����Σ�Ч�ʾ�Ȼû��json�� , 100���߶�Ҫ10�룬 json 400�߶�1��
    public class DbCacheSerializable<TValue> where TValue : ISerializable , new()
    {
        private readonly DB db;
        private readonly ReadOptions options;
        private readonly WriteBatch  batch;
        private readonly string      prefix;
        private readonly DbUndo      undos;

        Dictionary<string,TValue> currDic = new Dictionary<string, TValue>();

        public class Slice
        {
            public TValue obj;
        }

        public DbCacheSerializable(DB db, ReadOptions options, WriteBatch batch, DbUndo undos, string prefix)
        {
            this.db = db;
            this.options = options ?? DbSnapshot.ReadOptions_Default;
            this.batch   = batch;
            this.prefix  = prefix;
            this.undos   = undos;
        }

        public void Add(string key, TValue value)
        {
            Type type = value.GetType();

            // ���ݻ���
            string old = db.Get($"{prefix}___{key}", options);
            if (undos!=null)
            {
                batch?.Put($"{prefix}___{key}_undo_{undos.height}", old??"");
                undos.keys.Add($"{prefix}___{key}");
            }
            currDic.Remove(key);
            currDic.Add(key,value);

            batch?.Put($"{prefix}___{key}", Base58.Encode(Serialize(value)) );
        }

        public void Delete(string key)
        {
            batch?.Delete($"{prefix}___{key}");
        }

        public TValue Get(string key)
        {
            TValue currValue = default(TValue);
            if (currDic.TryGetValue(key, out currValue))
                return currValue;

            string value = db.Get($"{prefix}___{key}", options);
            if(value!=null)
                return Deserialize(Base58.Decode(value));

            return default(TValue);
        }

        /// <summary>
        /// ���������л�Ϊ���������� 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public byte[] Serialize(TValue obj)
        {
            MemoryStream stream = new MemoryStream();
            var bw = new BinaryWriter(stream);
            obj.Serialize(bw);
            byte[] data = stream.ToArray();
            stream.Close();
            return data;
        }

        /// <summary>
        /// �����������ݷ����л�
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public TValue Deserialize(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            var br = new BinaryReader(stream);
            var obj = new TValue();
            obj.Deserialize(br);
            stream.Close();
            return obj;
        }


    }
}
