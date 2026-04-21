namespace RecursiveParsing;

static class Ext
{
    extension<T>(ReadOnlyMemory<T> rom)
    where T : struct
    {
        public T? First
        {
            get
            {
                if (rom.IsEmpty)
                    return null;
                return rom.Span[0];
            }
        }
    }
    extension<T>(ReadOnlyMemory<T>)
    {
        public static ReadOnlyMemory<T> operator ++(ReadOnlyMemory<T> rom)
        => rom += 1;
        public static ReadOnlyMemory<T> operator +(ReadOnlyMemory<T> rom, Index offset)
        => rom[offset..];
    }
}
