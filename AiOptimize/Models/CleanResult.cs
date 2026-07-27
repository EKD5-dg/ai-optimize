namespace AiOptimize.Models;

public sealed class CleanResult
{
    public long BytesFreed { get; set; }
    public int FilesDeleted { get; set; }
    public int FilesSkipped { get; set; }
    public List<string> Notes { get; } = new();

    public void Merge(CleanResult other)
    {
        BytesFreed += other.BytesFreed;
        FilesDeleted += other.FilesDeleted;
        FilesSkipped += other.FilesSkipped;
        Notes.AddRange(other.Notes);
    }
}
