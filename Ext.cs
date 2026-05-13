using System.Diagnostics;
using EBNFParser.Phases.Tokenize;
using Microsoft.Build.Utilities;

namespace EBNFParser;

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

    extension(Index index)
    {
        public (int l, int c) GetPos(ReadOnlySpan<char> text)
        {
            var before = text[..index];
            var nls = before.Count('\n');
            var offset = index.GetOffset(text.Length) - before.LastIndexOf('\n');
            return (nls + 1, offset);
        }
    }

    private delegate void LogLevel(
            string? subcategory,
            string? errorCode,
            string? helpKeyword,
            string? file,
            int lineNumber,
            int columnNumber,
            int endLineNumber,
            int endColumnNumber,
            string message,
            params object[] messageArgs);

    extension(TaskLoggingHelper log)
    {
        public void LogErrorFromEbnfException(EBNFException ex, ReadOnlySpan<char> input, string filePath)
        {
            var (l, c) = ex.Range.Start.GetPos(input);
            ((LogLevel)(ex.Level switch
            {
                EBNFException.ExceptionLevel.Warning => log.LogWarning,
                EBNFException.ExceptionLevel.Error => log.LogError,
                _ => throw new UnreachableException(),
            }))(subcategory: ex.SubCategory,
                errorCode: ex.ErrorCode,
                helpKeyword: null,
                file: filePath,
                lineNumber: l,
                columnNumber: c,
                endLineNumber: 0,
                endColumnNumber: 0,
                message: ex.Message);
        }
    }
}
