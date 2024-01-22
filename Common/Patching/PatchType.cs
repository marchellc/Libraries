namespace Common.Patching
{
    public enum PatchType
    {
        All,

        Prefix,
        Postfix,

        Transpiler,

        Finalizer,

        Reverse,
    }
}