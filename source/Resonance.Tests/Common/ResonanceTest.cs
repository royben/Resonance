﻿using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resonance.Adapters.InMemory;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Tests.Common
{
    [TestClass]
    public class ResonanceTest
    {
        private Serilog.Core.Logger _logger;

        private TestContext testContextInstance;
        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext
        {
            get { return testContextInstance; }
            set { testContextInstance = value; }
        }

        public bool IsRunningOnAzurePipelines { get; set; }

        /// <summary>
        /// Gets or sets the test logger.
        /// </summary>
        public Microsoft.Extensions.Logging.ILogger Logger { get; set; }

        [TestInitialize]
        public void Init()
        {
            InMemoryAdapter.DisposeAll();

            IsRunningOnAzurePipelines = bool.Parse(TestContext.Properties["IsFromAzure"].ToString());

            var loggerFactory = new LoggerFactory();
            var loggerConfiguration = new LoggerConfiguration();

            if (IsRunningOnAzurePipelines)
            {
                loggerConfiguration.MinimumLevel.Warning();
            }
            else
            {
                if (Debugger.IsAttached)
                {
                    loggerConfiguration.MinimumLevel.Information();
                }
                else
                {
                    loggerConfiguration.MinimumLevel.Information();
                }

                loggerConfiguration.WriteTo.Sink(new SerilogTestContextSink(TestContext));
                loggerConfiguration.WriteTo.Debug(Serilog.Events.LogEventLevel.Debug, "[{SourceContext}] [{Level}] [{Timestamp:HH:mm:ss.fff}]: {Message}{NewLine}{Exception}");
                loggerConfiguration.WriteTo.Seq("http://localhost:5341");
            }

            _logger = loggerConfiguration.CreateLogger();

            loggerFactory.AddSerilog(_logger);

            ResonanceGlobalSettings.Default.LoggerFactory = loggerFactory;

            StackTrace stackTrace = new StackTrace();
            var testName = stackTrace.GetFrame(1).GetMethod().Name;

            var logger = loggerFactory.CreateLogger(testName);
            logger.LogDebug("Starting Test...");

            Logger = logger;
        }

        [TestCleanup]
        public void Dispose()
        {
            _logger?.Dispose();
        }
    }
}
