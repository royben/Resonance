using Microsoft.Owin.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Tests.WebAPI
{
    public class CalculationWebApiService : ResonanceObject, IDisposable
    {
        private IDisposable _service;

        public String Address { get; private set; }

        public CalculationWebApiService(String address)
        {
            Address = address;
        }

        public void Start()
        {
            // Start OWIN host 
            _service = WebApp.Start<Startup>(url: Address);
            LogManager.Log($"WebAPI service started ({Address})...");
        }

        public void Dispose()
        {
            _service?.Dispose();
        }
    }
}
