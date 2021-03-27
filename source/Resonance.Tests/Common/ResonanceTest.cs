using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resonance.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Tests.Common
{
    [TestClass]
    public class ResonanceTest
    {
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

        /// <summary>
        /// Gets the default log manager.
        /// </summary>
        public LogManager LogManager
        {
            get { return LogManager.Default; }
        }

        public bool IsRunningOnAzurePipelines { get; set; }

        protected void Init()
        {
            IsRunningOnAzurePipelines = bool.Parse(TestContext.Properties["IsFromAzure"].ToString());
            LogManager.Default.NewLog += Default_NewLog;
        }

        private void Default_NewLog(object sender, LogItemBase e)
        {
            if (IsRunningOnAzurePipelines && e.Level == LogLevel.Debug) return;

            TestContext.WriteLine(e.ToString());
            Debug.WriteLine(e.ToString());
        }
    }
}
