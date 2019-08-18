using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using RSocket;

namespace Demo.AspNetCore.RSocket.RSocket
{
    internal class EchoServer : RSocketServer
    {
        public EchoServer(IRSocketTransport transport, RSocketOptions options = default, int echoes = 2)
            : base(transport, options)
        {
            // Request/Response
            Respond(
                request => request,                                 // requestTransform
                request => AsyncEnumerable.Repeat(request, echoes), // producer
                result => result                                    // resultTransform
            );

            // Request/Stream
            Stream(
                request => request,                                 // requestTransform
                request => AsyncEnumerable.Repeat(request, echoes), // producer
                result => result                                    // resultTransform
            );
        }
    }
}
