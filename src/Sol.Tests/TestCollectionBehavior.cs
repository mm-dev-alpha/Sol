using Xunit;

// Disable parallel test execution because several unit tests manipulate shared static process state
// (such as WeakReferenceMessenger.Default and AppLog).
[assembly: CollectionBehavior(DisableTestParallelization = true)]
