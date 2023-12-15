using Common.Extensions;

using System;
using System.IO;

namespace Network.Synchronization
{
    public class SynchronizedDelegatedValue<TValue> : SynchronizedValue<TValue>
    {
        private Func<BinaryReader, TValue> updateReader;
        private Action<BinaryWriter, TValue> updateWriter;

        public SynchronizedDelegatedValue(Func<BinaryReader, TValue> updateReader, Action<BinaryWriter, TValue> updateWriter, TValue value = default)
        {
            if (updateReader is null)
                throw new ArgumentNullException(nameof(updateReader));

            if (updateWriter is null)
                throw new ArgumentNullException(nameof(updateWriter));

            this.updateReader = updateReader;
            this.updateWriter = updateWriter;

            SetValueNoUpdate(value);
        }

        public override void Update(BinaryReader reader)
        {
            base.Update(reader);
            var value = updateReader.Call(reader);
            SetValueNoUpdate(value);
        }

        public override void Update(BinaryWriter writer)
        {
            base.Update(writer);
            updateWriter.Call(writer, Value);
        }
    }
}