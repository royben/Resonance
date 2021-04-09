using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resonance.Adapters.InMemory;
using Resonance.Messages;
using Resonance.Tests.Common;
using Resonance.Transporters;
using System;
using System.Threading;
using System.Threading.Tasks;
using Resonance.ExtensionMethods;
using Resonance.HandShake;
using Resonance.Cryptography;

namespace Resonance.Tests
{
    [TestClass]
    [TestCategory("HandShake")]
    public class HandShake_TST : ResonanceTest
    {
        [TestMethod]
        public void HandShake_Transcoder_Encoding_Decoding()
        {
            Init();

            ResonanceHandShakeMessage handshake = new ResonanceHandShakeMessage();
            handshake.ClientId = 4;
            handshake.EncryptionPublicKey = "1234";
            handshake.RequireEncryption = true;
            handshake.SymmetricPassword = "password";
            handshake.Type = ResonanceHandShakeMessageType.Response;

            IResonanceHandShakeTranscoder transcoder = new ResonanceDefaultHandShakeTranscoder();
            byte[] data = transcoder.Encode(handshake);

            ResonanceHandShakeMessage decoded = transcoder.Decode(data);

            Assert.AreEqual(handshake.ToJsonString(), decoded.ToJsonString());
        }

        [TestMethod]
        public void HandShake_Negotiator_Provides_Symmetric_Password_Oneway()
        {
            Init();

            for (int i = 0; i < 10; i++)
            {
                InMemoryAdapter adapter1 = new InMemoryAdapter("TST");
                InMemoryAdapter adapter2 = new InMemoryAdapter("TST");

                IResonanceHandShakeNegotiator negotiator1 = new ResonanceDefaultHandShakeNegotiator();
                IResonanceHandShakeNegotiator negotiator2 = new ResonanceDefaultHandShakeNegotiator();

                String symmetricPassword1 = "1";
                String symmetricPassword2 = "2";

                negotiator1.WriteHandShake += (x, e) =>
                {
                    adapter1.Write(e.Data);
                };

                negotiator2.WriteHandShake += (x, e) =>
                {
                    adapter2.Write(e.Data);
                };

                negotiator1.SymmetricPasswordAvailable += (x, e) =>
                {
                    symmetricPassword1 = e.SymmetricPassword;
                };

                negotiator2.SymmetricPasswordAvailable += (x, e) =>
                {
                    symmetricPassword2 = e.SymmetricPassword;
                };

                adapter1.Connect().GetAwaiter().GetResult();
                adapter2.Connect().GetAwaiter().GetResult();

                adapter1.DataAvailable += (x, e) =>
                {
                    negotiator1.HandShakeMessageDataReceived(e.Data);
                };

                adapter2.DataAvailable += (x, e) =>
                {
                    negotiator2.HandShakeMessageDataReceived(e.Data);
                };

                negotiator1.Reset(true, new RSACryptographyProvider());
                negotiator2.Reset(true, new RSACryptographyProvider());

                negotiator1.BeginHandShake();

                Thread.Sleep(100);

                Assert.IsTrue(negotiator1.State == ResonanceHandShakeState.Completed);
                Assert.IsTrue(negotiator2.State == ResonanceHandShakeState.Completed);
                Assert.AreEqual(symmetricPassword1, symmetricPassword2);

                Guid.Parse(symmetricPassword1);
                Guid.Parse(symmetricPassword2);

                adapter1.Dispose();
                adapter2.Dispose(); 
            }
        }

        [TestMethod]
        public void HandShake_Negotiator_Provides_Symmetric_Password_Twoway()
        {
            Init();

            for (int i = 0; i < 10; i++)
            {
                InMemoryAdapter adapter1 = new InMemoryAdapter("TST");
                InMemoryAdapter adapter2 = new InMemoryAdapter("TST");

                IResonanceHandShakeNegotiator negotiator1 = new ResonanceDefaultHandShakeNegotiator();
                IResonanceHandShakeNegotiator negotiator2 = new ResonanceDefaultHandShakeNegotiator();

                String symmetricPassword1 = "1";
                String symmetricPassword2 = "2";

                negotiator1.WriteHandShake += (x, e) =>
                {
                    adapter1.Write(e.Data);
                };

                negotiator2.WriteHandShake += (x, e) =>
                {
                    adapter2.Write(e.Data);
                };

                negotiator1.SymmetricPasswordAvailable += (x, e) =>
                {
                    symmetricPassword1 = e.SymmetricPassword;
                };

                negotiator2.SymmetricPasswordAvailable += (x, e) =>
                {
                    symmetricPassword2 = e.SymmetricPassword;
                };

                adapter1.Connect().GetAwaiter().GetResult();
                adapter2.Connect().GetAwaiter().GetResult();

                adapter1.DataAvailable += (x, e) =>
                {
                    negotiator1.HandShakeMessageDataReceived(e.Data);
                };

                adapter2.DataAvailable += (x, e) =>
                {
                    negotiator2.HandShakeMessageDataReceived(e.Data);
                };

                negotiator1.Reset(true, new RSACryptographyProvider());
                negotiator2.Reset(true, new RSACryptographyProvider());

                negotiator1.BeginHandShakeAsync();
                negotiator2.BeginHandShakeAsync();

                Thread.Sleep(100);

                Assert.IsTrue(negotiator1.State == ResonanceHandShakeState.Completed);
                Assert.IsTrue(negotiator2.State == ResonanceHandShakeState.Completed);
                Assert.AreEqual(symmetricPassword1, symmetricPassword2);

                Guid.Parse(symmetricPassword1);
                Guid.Parse(symmetricPassword2);

                adapter1.Dispose();
                adapter2.Dispose(); 
            }
        }

        [TestMethod]
        public void HandShake_Negotiator_Does_Not_Provide_Symmetric_Password_Oneway()
        {
            Init();

            for (int i = 0; i < 10; i++)
            {
                InMemoryAdapter adapter1 = new InMemoryAdapter("TST");
                InMemoryAdapter adapter2 = new InMemoryAdapter("TST");

                IResonanceHandShakeNegotiator negotiator1 = new ResonanceDefaultHandShakeNegotiator();
                IResonanceHandShakeNegotiator negotiator2 = new ResonanceDefaultHandShakeNegotiator();

                String symmetricPassword1 = "1";
                String symmetricPassword2 = "2";

                negotiator1.WriteHandShake += (x, e) =>
                {
                    adapter1.Write(e.Data);
                };

                negotiator2.WriteHandShake += (x, e) =>
                {
                    adapter2.Write(e.Data);
                };

                negotiator1.SymmetricPasswordAvailable += (x, e) =>
                {
                    Assert.Fail();
                    symmetricPassword1 = e.SymmetricPassword;
                };

                negotiator2.SymmetricPasswordAvailable += (x, e) =>
                {
                    Assert.Fail();
                    symmetricPassword2 = e.SymmetricPassword;
                };

                adapter1.Connect().GetAwaiter().GetResult();
                adapter2.Connect().GetAwaiter().GetResult();

                adapter1.DataAvailable += (x, e) =>
                {
                    negotiator1.HandShakeMessageDataReceived(e.Data);
                };

                adapter2.DataAvailable += (x, e) =>
                {
                    negotiator2.HandShakeMessageDataReceived(e.Data);
                };

                negotiator1.Reset(i % 2 == 0, new RSACryptographyProvider());
                negotiator2.Reset(i % 2 != 0, new RSACryptographyProvider());

                negotiator1.BeginHandShake();

                Assert.IsTrue(negotiator1.State == ResonanceHandShakeState.Completed);
                Assert.IsTrue(negotiator2.State == ResonanceHandShakeState.Completed);
                Assert.AreEqual(symmetricPassword1, "1");
                Assert.AreEqual(symmetricPassword2, "2");

                adapter1.Dispose();
                adapter2.Dispose(); 
            }
        }

        [TestMethod]
        public void HandShake_Negotiator_Does_Not_Provide_Symmetric_Password_Twoway()
        {
            Init();

            for (int i = 0; i < 10; i++)
            {
                InMemoryAdapter adapter1 = new InMemoryAdapter("TST");
                InMemoryAdapter adapter2 = new InMemoryAdapter("TST");

                IResonanceHandShakeNegotiator negotiator1 = new ResonanceDefaultHandShakeNegotiator();
                IResonanceHandShakeNegotiator negotiator2 = new ResonanceDefaultHandShakeNegotiator();

                String symmetricPassword1 = "1";
                String symmetricPassword2 = "2";

                negotiator1.WriteHandShake += (x, e) =>
                {
                    adapter1.Write(e.Data);
                };

                negotiator2.WriteHandShake += (x, e) =>
                {
                    adapter2.Write(e.Data);
                };

                negotiator1.SymmetricPasswordAvailable += (x, e) =>
                {
                    Assert.Fail();
                    symmetricPassword1 = e.SymmetricPassword;
                };

                negotiator2.SymmetricPasswordAvailable += (x, e) =>
                {
                    Assert.Fail();
                    symmetricPassword2 = e.SymmetricPassword;
                };

                adapter1.Connect().GetAwaiter().GetResult();
                adapter2.Connect().GetAwaiter().GetResult();

                adapter1.DataAvailable += (x, e) =>
                {
                    negotiator1.HandShakeMessageDataReceived(e.Data);
                };

                adapter2.DataAvailable += (x, e) =>
                {
                    negotiator2.HandShakeMessageDataReceived(e.Data);
                };

                negotiator1.Reset(i % 2 == 0, new RSACryptographyProvider());
                negotiator2.Reset(i % 2 != 0, new RSACryptographyProvider());

                negotiator1.BeginHandShakeAsync();
                negotiator2.BeginHandShakeAsync();

                Thread.Sleep(100);

                Assert.IsTrue(negotiator1.State == ResonanceHandShakeState.Completed);
                Assert.IsTrue(negotiator2.State == ResonanceHandShakeState.Completed);
                Assert.AreEqual(symmetricPassword1, "1");
                Assert.AreEqual(symmetricPassword2, "2");

                adapter1.Dispose();
                adapter2.Dispose();
            }
        }

        [TestMethod]
        public async Task Handshake_With_Different_Encryption_Configuration_And_Conditions()
        {
            Init();

            for (int i = 0; i < 10; i++)
            {
                ResonanceJsonTransporter t1 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));
                ResonanceJsonTransporter t2 = new ResonanceJsonTransporter(new InMemoryAdapter("TST"));

                t1.CryptographyConfiguration.Enabled = i % 2 == 0;
                t2.CryptographyConfiguration.Enabled = i % 3 == 0;

                await t1.Connect();
                await t2.Connect();

                t2.RequestReceived += async (s, e) =>
                {
                    CalculateRequest receivedRequest = e.Request.Message as CalculateRequest;
                    await t2.SendResponse(new CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Request.Token);
                };

                t1.RequestReceived += async (s, e) =>
                {
                    CalculateRequest receivedRequest = e.Request.Message as CalculateRequest;
                    await t1.SendResponse(new CalculateResponse() { Sum = receivedRequest.A + receivedRequest.B }, e.Request.Token);
                };

                var request = new CalculateRequest() { A = 10, B = 15 };

                CalculateResponse response1 = null;
                CalculateResponse response2 = null;

                if (i % 2 == 0)
                {
                    Task.Factory.StartNew(() =>
                    {
                        response1 = t2.SendRequest<CalculateRequest, CalculateResponse>(request).GetAwaiter().GetResult();
                    }).GetAwaiter().GetResult();

                    Task.Factory.StartNew(() =>
                    {
                        response2 = t1.SendRequest<CalculateRequest, CalculateResponse>(request).GetAwaiter().GetResult();
                    }).GetAwaiter().GetResult();
                }
                else
                {
                    response1 = await t2.SendRequest<CalculateRequest, CalculateResponse>(request);
                    response2 = await t1.SendRequest<CalculateRequest, CalculateResponse>(request);
                }

                Thread.Sleep(1000);

                Assert.IsNotNull(response1);
                Assert.IsNotNull(response2);

                if (t1.CryptographyConfiguration.Enabled && t2.CryptographyConfiguration.Enabled)
                {
                    Assert.IsTrue(t1.IsChannelSecure);
                    Assert.IsTrue(t2.IsChannelSecure);
                }
                else
                {
                    Assert.IsFalse(t1.IsChannelSecure);
                    Assert.IsFalse(t2.IsChannelSecure);
                }

                await t1.DisposeAsync(true);
                await t2.DisposeAsync(true);

                Assert.IsNotNull(response1);
                Assert.IsNotNull(response2);
                Assert.AreEqual(response1.Sum, request.A + request.B);
                Assert.AreEqual(response2.Sum, request.A + request.B);
            }
        }
    }
}
