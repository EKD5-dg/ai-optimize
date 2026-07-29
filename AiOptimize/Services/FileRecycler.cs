using AiOptimize.Native;

namespace AiOptimize.Services;

/// <summary>把文件移入回收站（可恢复），而非永久删除。</summary>
public static class FileRecycler
{
    public static void MoveToRecycleBin(string path)
    {
        var op = new NativeMethods.SHFILEOPSTRUCT
        {
            wFunc = NativeMethods.FO_DELETE,
            pFrom = path + "\0", // 需要双空字符结尾（封送时自动补一个）
            fFlags = (ushort)(NativeMethods.FOF_ALLOWUNDO | NativeMethods.FOF_NOCONFIRMATION | NativeMethods.FOF_SILENT),
        };
        int result = NativeMethods.SHFileOperation(ref op);
        if (result != 0 || op.fAnyOperationsAborted)
            throw new InvalidOperationException($"文件未能移入回收站（代码 {result}）");
    }
}
