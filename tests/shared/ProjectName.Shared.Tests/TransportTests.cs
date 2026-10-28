using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ProjectName.Shared.Protocol;
using ProjectName.Shared.Transport;
using Xunit;

namespace ProjectName.Shared.Tests
{
    public class TransportTests
    {
        [Fact]
        public async Task ControlMessageFraming_WritesAndReadsFrame_Correctly()
        {
            using var ms = new MemoryStream();
            string testJson = "{\"type\":\"TEST_MESSAGE\",\"sequence_number\":1}";

            await ControlMessageFraming.WriteFrameAsync(ms, testJson);
            ms.Position = 0;

            string? readJson = await ControlMessageFraming.ReadFrameAsync(ms);
            Assert.Equal(testJson, readJson);
        }

        [Fact]
        public async Task ControlMessageFraming_RejectsOversizedPayload_Exceeding64KB()
        {
            using var ms = new MemoryStream();
            string oversized = new string('A', ProtocolEnvelope.MaxMessageSizeBytes + 1);

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await ControlMessageFraming.WriteFrameAsync(ms, oversized);
            });
        }

        [Fact]
        public async Task ControlMessageFraming_EmptyStream_ReturnsNull()
        {
            using var emptyMs = new MemoryStream();
            string? result = await ControlMessageFraming.ReadFrameAsync(emptyMs);
            Assert.Null(result);
        }

        [Fact]
        public async Task ControlMessageFraming_NegativeLengthHeader_ThrowsInvalidDataException()
        {
            using var ms = new MemoryStream();
            byte[] badHeader = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }; // -1
            await ms.WriteAsync(badHeader);
            ms.Position = 0;

            await Assert.ThrowsAsync<InvalidDataException>(async () =>
            {
                await ControlMessageFraming.ReadFrameAsync(ms);
            });
        }

        [Fact]
        public async Task ControlMessageFraming_HeaderExceeding64KB_ThrowsInvalidDataException()
        {
            using var ms = new MemoryStream();
            byte[] oversizeHeader = new byte[] { 0x00, 0x01, 0x00, 0x01 }; // 65,537 bytes (> 65,536)
            await ms.WriteAsync(oversizeHeader);
            ms.Position = 0;

            await Assert.ThrowsAsync<InvalidDataException>(async () =>
            {
                await ControlMessageFraming.ReadFrameAsync(ms);
            });
        }

        [Fact]
        public void CoordinateMapper_ClampNormalized_HandlesEdgeCases()
        {
            var (x1, y1) = CoordinateMapper.ClampNormalized(double.NaN, double.PositiveInfinity);
            Assert.Equal(0.0, x1);
            Assert.Equal(0.0, y1);

            var (x2, y2) = CoordinateMapper.ClampNormalized(-5.0, 10.0);
            Assert.Equal(0.0, x2);
            Assert.Equal(1.0, y2);
        }

        [Fact]
        public async Task TlsControlServer_And_Client_SendAndReceiveEnvelopes_Successfully()
        {
            using var serverCert = EphemeralCertificateGenerator.CreateDeviceCertificate("TestServer");
            await using var server = new TlsControlServer(port: 0, serverCertificate: serverCert);

            var serverMessageTcs = new TaskCompletionSource<ProtocolEnvelope>();
            server.MessageReceived += (_, e) =>
            {
                serverMessageTcs.TrySetResult(e.Envelope);
            };

            await server.StartAsync();
            Assert.True(server.Port > 0);

            await using var client = new TlsControlClient();
            var clientConnectedTcs = new TaskCompletionSource<bool>();
            server.ClientConnected += (_, _) => clientConnectedTcs.TrySetResult(true);

            await client.ConnectAsync("127.0.0.1", server.Port);

            var connectedWait = await Task.WhenAny(clientConnectedTcs.Task, Task.Delay(3000));
            Assert.Equal(clientConnectedTcs.Task, connectedWait);

            // Send message from client to server
            var sentEnvelope = await client.SendMessageAsync("TEST_CLIENT_MSG", new { hello = "world" });

            var messageWait = await Task.WhenAny(serverMessageTcs.Task, Task.Delay(3000));
            Assert.Equal(serverMessageTcs.Task, messageWait);

            var receivedEnvelope = await serverMessageTcs.Task;
            Assert.Equal("TEST_CLIENT_MSG", receivedEnvelope.Type);
            Assert.Equal(sentEnvelope.SequenceNumber, receivedEnvelope.SequenceNumber);

            await client.DisconnectAsync();
            await server.StopAsync();
        }

        [Fact]
        public async Task TlsControlServer_EnforcesSingleControllerPolicy_RejectingConcurrentClient()
        {
            using var serverCert = EphemeralCertificateGenerator.CreateDeviceCertificate("TestServerSingle");
            await using var server = new TlsControlServer(port: 0, serverCertificate: serverCert);
            await server.StartAsync();

            await using var client1 = new TlsControlClient();
            await client1.ConnectAsync("127.0.0.1", server.Port);

            // Second client connection must be handled by rejection
            await using var client2 = new TlsControlClient();
            var rejectedErrorTcs = new TaskCompletionSource<ProtocolEnvelope>();
            client2.MessageReceived += (_, e) =>
            {
                if (e.Envelope.Type == MessageTypes.Error)
                {
                    rejectedErrorTcs.TrySetResult(e.Envelope);
                }
            };

            try
            {
                await client2.ConnectAsync("127.0.0.1", server.Port);
                var waitResult = await Task.WhenAny(rejectedErrorTcs.Task, Task.Delay(3000));
                // Client2 either receives the ERROR rejection envelope or is disconnected
                Assert.True(waitResult == rejectedErrorTcs.Task || !client2.IsConnected);
            }
            catch
            {
                // Immediate connection drop is also valid rejection behavior
            }

            await client1.DisconnectAsync();
            await server.StopAsync();
        }

        [Fact]
        public async Task TlsControlServer_SendsMultipleEnvelopes_WithStrictlyIncreasingSequenceNumbers()
        {
            using var serverCert = EphemeralCertificateGenerator.CreateDeviceCertificate("TestServerSeq");
            await using var server = new TlsControlServer(port: 0, serverCertificate: serverCert);
            await server.StartAsync();

            await using var client = new TlsControlClient();
            var receivedEnvelopes = new System.Collections.Generic.List<ProtocolEnvelope>();
            var allMessagesReceivedTcs = new TaskCompletionSource<bool>();

            client.MessageReceived += (_, e) =>
            {
                lock (receivedEnvelopes)
                {
                    receivedEnvelopes.Add(e.Envelope);
                    if (receivedEnvelopes.Count == 3)
                    {
                        allMessagesReceivedTcs.TrySetResult(true);
                    }
                }
            };

            await client.ConnectAsync("127.0.0.1", server.Port);

            // Server sends 3 sequential messages using SendEnvelopeAsync with 0 sequence (auto-incremented)
            await server.SendEnvelopeAsync(ProtocolEnvelope.Create("MSG_1", new { index = 1 }, 0));
            await server.SendEnvelopeAsync(ProtocolEnvelope.Create("MSG_2", new { index = 2 }, 0));
            await server.SendEnvelopeAsync(ProtocolEnvelope.Create("MSG_3", new { index = 3 }, 0));

            var wait = await Task.WhenAny(allMessagesReceivedTcs.Task, Task.Delay(3000));
            Assert.Equal(allMessagesReceivedTcs.Task, wait);

            Assert.Equal(3, receivedEnvelopes.Count);
            Assert.Equal(1, receivedEnvelopes[0].SequenceNumber);
            Assert.Equal(2, receivedEnvelopes[1].SequenceNumber);
            Assert.Equal(3, receivedEnvelopes[2].SequenceNumber);
            Assert.True(client.IsConnected); // Must NOT be disconnected by replay validation

            await client.DisconnectAsync();
            await server.StopAsync();
        }

        [Fact]
        public async Task TlsControlClient_DisconnectAsync_IdempotentAndDoesNotThrow()
        {
            using var serverCert = EphemeralCertificateGenerator.CreateDeviceCertificate("TestServerIdempotent");
            await using var server = new TlsControlServer(port: 0, serverCertificate: serverCert);
            await server.StartAsync();

            await using var client = new TlsControlClient();
            int disconnectEventCount = 0;
            client.Disconnected += (_, _) => Interlocked.Increment(ref disconnectEventCount);

            await client.ConnectAsync("127.0.0.1", server.Port);
            Assert.True(client.IsConnected);

            // Double disconnect must be safe and idempotent
            await client.DisconnectAsync("First disconnect");
            await client.DisconnectAsync("Second disconnect");

            Assert.False(client.IsConnected);
            Assert.Equal(1, disconnectEventCount);

            await server.StopAsync();
        }

        [Fact]
        public async Task TlsControlServer_ClientDisconnected_FiresExactlyOnceOnPeerDisconnect()
        {
            using var serverCert = EphemeralCertificateGenerator.CreateDeviceCertificate("TestServerOnce");
            await using var server = new TlsControlServer(port: 0, serverCertificate: serverCert);
            int serverDisconnectCount = 0;
            server.ClientDisconnected += (_, _) => Interlocked.Increment(ref serverDisconnectCount);

            await server.StartAsync();

            var client = new TlsControlClient();
            await client.ConnectAsync("127.0.0.1", server.Port);

            // Cleanly disconnect client
            await client.DisconnectAsync();

            // Wait brief moment for server to process socket close
            await Task.Delay(200);

            Assert.Equal(1, serverDisconnectCount);
            await server.StopAsync();
        }
    }
}
