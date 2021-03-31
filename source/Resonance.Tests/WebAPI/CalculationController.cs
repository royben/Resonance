using Resonance.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Resonance.Tests.WebAPI
{
    public class CalcController : ApiController
    {
        [HttpPost]
        public CalculateResponse Calculate(CalculateRequest request)
        {
            return new CalculateResponse() { Sum = request.A + request.B };
        }
    }
}
