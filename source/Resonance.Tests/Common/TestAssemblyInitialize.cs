using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;

namespace Resonance.Tests.Common
{
    /// <summary>
    /// Assembly wide test setup.
    /// </summary>
    [TestClass]
    public static class TestAssemblyInitialize
    {
        /// <summary>
        /// Raises the thread pool floor before any test runs.
        /// </summary>
        /// <remarks>
        /// Several tests deliberately block a handler thread - the RPC attribute timeout
        /// tests sleep inside a service method, and incoming messages are dispatched on
        /// thread pool threads. The pool only injects roughly one new thread per second
        /// once its minimum is exhausted, so a couple of consecutive blocking tests can
        /// stall the whole run for minutes on a machine with few cores, which is what a
        /// two core build agent looks like.
        ///
        /// Raising the minimum removes that injection delay. It does not paper over a
        /// library defect: the transporter no longer occupies a pool thread per pending
        /// message, which was the real cause of starvation.
        /// </remarks>
        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            ThreadPool.GetMinThreads(out int workerThreads, out int completionPortThreads);

            int desiredWorkers = Math.Max(workerThreads, 64);
            int desiredCompletionPorts = Math.Max(completionPortThreads, 64);

            ThreadPool.SetMinThreads(desiredWorkers, desiredCompletionPorts);
        }
    }
}
