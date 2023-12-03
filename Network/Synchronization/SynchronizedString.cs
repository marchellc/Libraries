using Common.Extensions;

namespace Network.Synchronization
{
    public class SynchronizedString : SynchronizedDelegatedValue<string>
    {
        public SynchronizedString(string value = default) : base(
            br => br.ReadStringEx(),
            (bw, value) => bw.WriteString(value),
            value)
        { }
    }
}