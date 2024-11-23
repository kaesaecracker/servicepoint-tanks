using System.Buffers;

namespace TanksServer.Interactivity;

internal sealed class BufferPool : MemoryPool<byte>
{
    private readonly MemoryPool<byte> _actualPool = Shared;

    public override int MaxBufferSize => int.MaxValue;

    protected override void Dispose(bool disposing) { }

    public override IMemoryOwner<byte> Rent(int minBufferSize = -1)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(minBufferSize, 1);
        return new BufferPoolMemoryOwner(_actualPool.Rent(minBufferSize), minBufferSize);
    }

    private sealed class BufferPoolMemoryOwner(IMemoryOwner<byte> actualOwner, int wantedSize) : IMemoryOwner<byte>
    {
        public Memory<byte> Memory { get; } = actualOwner.Memory[..wantedSize];

        public void Dispose() => actualOwner.Dispose();
    }
}
