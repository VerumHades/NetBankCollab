using Moq;
using NetBank.Common;
using Xunit;
using NetBank.Common.Structures.Buffering;
using NetBank.Services.Implementations.DoubleBufferedAccountService;

public class FlushOnSwapDoubleBufferTests
{
    private readonly Mock<IFactory<ICaptureBuffer>> _mockFactory;
    private readonly Mock<IProcessor<ICaptureBuffer>> _mockProcessor;
    private readonly Mock<ICaptureBuffer> _bufferA;
    private readonly Mock<ICaptureBuffer> _bufferB;

    public FlushOnSwapDoubleBufferTests()
    {
        _mockFactory = new Mock<IFactory<ICaptureBuffer>>();
        _mockProcessor = new Mock<IProcessor<ICaptureBuffer>>();
        
        _bufferA = new Mock<ICaptureBuffer>();
        _bufferB = new Mock<ICaptureBuffer>();

        _mockFactory.SetupSequence(f => f.Create())
            .Returns(_bufferA.Object)
            .Returns(_bufferB.Object);
    }
    
    [Fact]
    public async Task TrySwap_WhenAlreadySwapping_ShouldReturnFalse()
    {
        // This TCS allows us to control exactly when the Flush finishes
        var flushCompletionSource = new TaskCompletionSource<bool>();

        _mockProcessor
            .Setup(p => p.Flush(It.IsAny<ICaptureBuffer>(), It.IsAny<CancellationToken>()))
            .Returns(flushCompletionSource.Task);

        var buffer = new FlushOnSwapDoubleBuffer<ICaptureBuffer>(_mockFactory.Object, _mockProcessor.Object);

        // Act
        // 1. Start the first swap. It will "hang" at the Flush call.
        Task<bool> firstSwapTask = buffer.TrySwap();

        // 2. Attempt a second swap while the first is still processing.
        bool secondSwapResult = await buffer.TrySwap();

        // 3. Complete the first swap so we can await it safely.
        flushCompletionSource.SetResult(true);
        bool firstSwapResult = await firstSwapTask;

        // Assert
        Assert.True(firstSwapResult, "The first swap should have succeeded.");
        Assert.False(secondSwapResult, "The second swap should have failed because a swap was already in progress.");
        
        // Verify the processor was only called once
        _mockProcessor.Verify(p => p.Flush(It.IsAny<ICaptureBuffer>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TrySwap_PassesCancellationTokenToProcessor()
    {
        // Arrange
        var sut = new FlushOnSwapDoubleBuffer<ICaptureBuffer>(_mockFactory.Object, _mockProcessor.Object);
        
        // Act
        await sut.TrySwap();

        // Assert
        // Verify that Flush is called with A CancellationToken (not CancellationToken.None)
        _mockProcessor.Verify(p => p.Flush(
            It.IsAny<ICaptureBuffer>(), 
            It.Is<CancellationToken>(t => t != CancellationToken.None)), 
            Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_SignalsCancellationToActiveFlush()
    {
        // Arrange
        var tcs = new TaskCompletionSource();
        CancellationToken capturedToken = default;

        // Capture the token so we can inspect its state
        _mockProcessor.Setup(p => p.Flush(It.IsAny<ICaptureBuffer>(), It.IsAny<CancellationToken>()))
            .Callback<ICaptureBuffer, CancellationToken>((buf, token) => capturedToken = token)
            .Returns(tcs.Task);

        var sut = new FlushOnSwapDoubleBuffer<ICaptureBuffer>(_mockFactory.Object, _mockProcessor.Object);
        
        // Start the swap (which hangs on the TaskCompletionSource)
        var swapTask = sut.TrySwap();

        // Act
        var disposeTask = sut.DisposeAsync();

        // Assert
        Assert.True(capturedToken.IsCancellationRequested, "The token passed to Flush should be cancelled upon Dispose.");

        // Cleanup: Complete the hung tasks
        tcs.SetResult();
        await swapTask;
        await disposeTask;
    }

    [Fact]
    public async Task TrySwap_ProcessorThrows_ReleasesLockSuccessfully()
    {
        // Arrange
        _mockProcessor.Setup(p => p.Flush(It.IsAny<ICaptureBuffer>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database Error"));

        var sut = new FlushOnSwapDoubleBuffer<ICaptureBuffer>(_mockFactory.Object, _mockProcessor.Object);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => sut.TrySwap());
        
        // Re-setup processor to succeed
        _mockProcessor.Setup(p => p.Flush(It.IsAny<ICaptureBuffer>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        bool secondAttempt = await sut.TrySwap();
        Assert.True(secondAttempt);
    }

    [Fact]
    public async Task DisposeAsync_ClearsBothBuffersInFinallyBlock()
    {
        // Arrange
        var sut = new FlushOnSwapDoubleBuffer<ICaptureBuffer>(_mockFactory.Object, _mockProcessor.Object);

        // Act
        await sut.DisposeAsync();

        // Assert
        _bufferA.Verify(b => b.Clear(), Times.Once);
        _bufferB.Verify(b => b.Clear(), Times.Once);
    }
}