using System.Collections.Immutable;
using System.Windows.Documents;

namespace SchedulingSimulationOfMultichannelBatchProcessingSystem.Models;

public class MemoryAllocationTable
{
    private readonly Dictionary<string, (uint HeadAddress, uint size)> _jobMemoryBlocks = [];
    private readonly List<(uint HeadAddress, uint size)> _freeMemoryBlocks = [];

    /// <summary>
    /// 作业内存块。键为作业名称，值为元组，元组的第一个元素为内存块的起始地址，第二个元素为内存块的大小
    /// </summary>
    public ImmutableDictionary<string, (uint HeadAddress, uint size)> JobMemoryBlocks =>
        _jobMemoryBlocks.ToImmutableDictionary();

    /// <summary>
    /// 空闲内存块。元组的第一个元素为内存块的起始地址，第二个元素为内存块的大小
    /// </summary>
    public ImmutableList<(uint HeadAddress, uint size)> FreeMemoryBlocks => _freeMemoryBlocks.ToImmutableList();

    public MemoryAllocationTable()
    {
    }

    public MemoryAllocationTable(uint memorySize)
    {
        _freeMemoryBlocks.Add((0, memorySize));
    }

    public void Reset(uint memorySize)
    {
        _jobMemoryBlocks.Clear();
        _freeMemoryBlocks.Clear();
        _freeMemoryBlocks.Add((0, memorySize));
    }

    public void AllocateMemory(string jobName, uint size)
    {
        if (FreeMemoryBlocks.Count == 0)
        {
            throw new InvalidOperationException("内存不足");
        }

        var freeBlock = FreeMemoryBlocks.Where(b => b.size >= size)
            .MinBy(b => b.size);

        var remainingSize = freeBlock.size - size;
        var remainingBlock = (freeBlock.HeadAddress + size, remainingSize);

        _jobMemoryBlocks[jobName] = (freeBlock.HeadAddress, size);
        _freeMemoryBlocks.Remove(freeBlock);

        if (remainingSize > 0)
        {
            _freeMemoryBlocks.Add(remainingBlock);
        }
    }

    public void FreeMemory(string jobName)
    {
        var (headAddress, size) = JobMemoryBlocks[jobName];
        _jobMemoryBlocks.Remove(jobName);

        var adjacentBlocks = FreeMemoryBlocks
            .Where(b => b.HeadAddress + b.size == headAddress || headAddress + size == b.HeadAddress)
            .ToList();

        switch (adjacentBlocks.Count)
        {
            case 0:
                _freeMemoryBlocks.Add((headAddress, size));
                break;
            case 1:
            {
                var adjacentBlock = adjacentBlocks[0];
                _freeMemoryBlocks.Remove(adjacentBlock);
                _freeMemoryBlocks.Add((Math.Min(adjacentBlock.HeadAddress, headAddress), size + adjacentBlock.size));
                break;
            }
            default:
            {
                var firstBlock = adjacentBlocks[0];
                var secondBlock = adjacentBlocks[1];
                _freeMemoryBlocks.Remove(firstBlock);
                _freeMemoryBlocks.Remove(secondBlock);
                _freeMemoryBlocks.Add((Math.Min(firstBlock.HeadAddress, headAddress), size + firstBlock.size + secondBlock.size));
                break;
            }
        }
    }
}