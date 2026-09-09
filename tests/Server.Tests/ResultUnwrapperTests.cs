using NUnit.Framework;
using Rhino.AI.Server;

namespace Rhino.AI.Server.Tests;

[TestFixture]
public class ResultUnwrapperTests
{
    [Test]
    public async Task Null_unwraps_to_null()
    {
        object? result = await ResultUnwrapper.UnwrapAsync(null);
        Assert.That(result, Is.Null);
    }

    // Task.CompletedTask is actually a Task<VoidTaskResult> singleton at
    // runtime — the unwrapper must surface that as null, not leak the
    // VoidTaskResult sentinel struct.
    [Test]
    public async Task NonGeneric_Task_unwraps_to_null()
    {
        object? result = await ResultUnwrapper.UnwrapAsync(Task.CompletedTask);
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task Generic_Task_unwraps_to_inner_value()
    {
        object? result = await ResultUnwrapper.UnwrapAsync(Task.FromResult(42));
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public async Task NonGeneric_ValueTask_unwraps_to_null()
    {
        object? result = await ResultUnwrapper.UnwrapAsync(new ValueTask());
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task Generic_ValueTask_unwraps_to_inner_value()
    {
        object? result = await ResultUnwrapper.UnwrapAsync(new ValueTask<string>("done"));
        Assert.That(result, Is.EqualTo("done"));
    }

    [Test]
    public async Task Synchronous_value_passes_through()
    {
        object? result = await ResultUnwrapper.UnwrapAsync("plain");
        Assert.That(result, Is.EqualTo("plain"));
    }

    // A suspending async method returns an AsyncStateMachineBox, a Task<T> subclass, not a Task<T>.
    [Test]
    public async Task Suspending_async_Task_unwraps_to_inner_value()
    {
        object? result = await ResultUnwrapper.UnwrapAsync(SuspendThenReturnAsync("solved"));
        Assert.That(result, Is.EqualTo("solved"));
    }

    [Test]
    public async Task Suspending_async_void_Task_unwraps_to_null()
    {
        object? result = await ResultUnwrapper.UnwrapAsync(SuspendAsync());
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task Suspending_async_ValueTask_unwraps_to_inner_value()
    {
        object? result = await ResultUnwrapper.UnwrapAsync(SuspendThenReturnValueAsync(7));
        Assert.That(result, Is.EqualTo(7));
    }

    private static async Task<string> SuspendThenReturnAsync(string value)
    {
        await Task.Yield();
        return value;
    }

    private static async Task SuspendAsync() => await Task.Yield();

    private static async ValueTask<int> SuspendThenReturnValueAsync(int value)
    {
        await Task.Yield();
        return value;
    }
}
