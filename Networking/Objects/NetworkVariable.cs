using Common.Extensions;

using Networking.Data;
using Networking.Utilities;

using System;
using System.Reflection;
using System.Threading;

namespace Networking.Objects
{
    public class NetworkVariable
    {
        public object prevValue;
        public bool isSyncRequested;

        public readonly Type varType;
        public readonly MemberInfo member;
        public readonly NetworkManager manager;
        public readonly Timer timer;

        public readonly object reference;

        public readonly int parentId;
        public readonly int id;

        public readonly Action<Writer, object> writer;
        public readonly Func<Reader, object> reader;

        public NetworkVariable(Type variableType, MemberInfo member, object reference, int id, int parentId, NetworkManager manager)
        {
            this.varType = variableType;
            this.member = member;
            this.reference = reference;
            this.parentId = parentId;
            this.id = id;
            this.manager = manager;

            this.writer = TypeLoader.GetWriter(varType);
            this.reader = TypeLoader.GetReader(varType);

            if (this.writer is null || this.reader is null)
                throw new InvalidOperationException($"Invalid variable type");

            this.timer = new Timer(_ => Update(), null, 100, 200);
        }

        public void SetValue(Reader reader)
            => SetValue(this.reader(reader));

        public void SetValue(object value)
        {
            if (member is FieldInfo field)
                field.SetValueFast(reference, value);
            else if (member is PropertyInfo prop)
                prop.SetValueFast(reference, value);
            else
                throw new InvalidOperationException($"invalid member type");
        }

        public void GetValue(Writer writer)
            => this.writer(writer, GetValue());

        public object GetValue()
        {
            if (member is FieldInfo field)
                return field.GetValueFast<object>(reference);
            else if (member is PropertyInfo prop)
                return prop.GetValueFast<object>(reference);
            else
                throw new InvalidOperationException($"invalid member type");
        }

        public void Dispose()
        {
            timer?.Dispose();
        }

        public void Update()
        {
            if (isSyncRequested)
                return;

            var newValue = GetValue();

            if ((prevValue is null && newValue != null)
                || (prevValue != null && newValue is null)
                || (prevValue != newValue))
            {
                manager.Synchronize(this);
                isSyncRequested = true;
            }
        }
    }
}