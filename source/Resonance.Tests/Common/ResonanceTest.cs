using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resonance.Adapters.InMemory;
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
        public ResonanceLogManager Log
        {
            get { return ResonanceLogManager.Default; }
        }

        public bool IsRunningOnAzurePipelines { get; set; }

        public void Init()
        {
            InMemoryAdapter.DisposeAll();

            IsRunningOnAzurePipelines = bool.Parse(TestContext.Properties["IsFromAzure"].ToString());

            if (IsRunningOnAzurePipelines)
            {
                Log.LogLevel = ResonanceLogLevel.Info;
                ResonanceGlobalSettings.Default.DisableHandShake = true;
            }
            else
            {
                if (Debugger.IsAttached)
                {
                    Log.LogLevel = ResonanceLogLevel.Debug;
                }
                else
                {
                    Log.LogLevel = ResonanceLogLevel.Info;
                }
            }

            ResonanceLogManager.Default.LogItemAvailable -= Default_LogItemAvailable;
            ResonanceLogManager.Default.LogItemAvailable += Default_LogItemAvailable;
        }

        private void Default_LogItemAvailable(object sender, ResonanceLogItemAvailableEventArgs e)
        {
            TestContext.WriteLine(e.LogItem.Message);
            Debug.WriteLine(e.LogItem.Message);
        }
    }
}
