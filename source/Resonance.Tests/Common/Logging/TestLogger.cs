using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resonance.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Tests.Common.Logging
{
    public static class TestLogger
    {
        private static TestContext _context;

        public static void Init(TestContext context)
        {
            _context = context;
            LogManager.Default.NewLog += Default_NewLog;
        }

        private static void Default_NewLog(object sender, LogItemBase e)
        {
            _context.WriteLine(e.ToString());
        }
    }
}
