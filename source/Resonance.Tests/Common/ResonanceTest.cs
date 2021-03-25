using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resonance.Logging;
using Resonance.Tests.Common.Logging;
using System;
using System.Collections.Generic;
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

        protected void Init()
        {
            TestLogger.Init(TestContext);
        }
    }
}
