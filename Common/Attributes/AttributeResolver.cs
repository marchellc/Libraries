using System;

namespace Common.Attributes
{
    public class AttributeResolver : Attribute
    {
        public AttributeCache Cache { get; internal set; }

        public virtual void OnResolved() { }
        public virtual void OnRemoved() { }
    }
}