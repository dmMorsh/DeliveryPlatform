using Grpc.Net.Client;
using LocationTracking;
using Shared.Proto;
using Grpc.Core;

Console.WriteLine("LocationTrackingClient starting...");

var address = Environment.GetEnvironmentVariable("LT_ADDRESS") ?? "https://localhost:7255";
using var channel = GrpcChannel.ForAddress(address);
///////////////////////////////////////////////////////////////////////////////////////
var client1 = new OrderGrpc.OrderGrpcClient(channel);

var jwtToken =
    "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIwMTliNDYxZS0wOWM3LTdiNTUtOWVjNy00MDE2YzMwYmNjN2YiLCJlbWFpbCI6InRlc3RAdGVzdC5jb20iLCJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOiJDdXN0b21lciIsInRlbmFudF9pZCI6IjExMTExMTExLTExMTEtMTExMS0xMTExLTExMTExMTExMTExMSIsImV4cCI6MTc2NjQxODgxNCwiaXNzIjoiaWRlbnRpdHktc2VydmljZSIsImF1ZCI6InBsYXRmb3JtLWFwaSJ9.giDxqFmXYeXHmvOVg3Y6fggrxy0gxCNcBEAtSOrK4H4"
        
        .Trim().Replace("\r", "").Replace("\n", "");
if (jwtToken.StartsWith("\"") || jwtToken.EndsWith("\""))
{
    throw new Exception("TOKEN CONTAINS QUOTES");
}
var headers = new Metadata
{
    { "authorization", $"Bearer {jwtToken}" }
};

var response = await client1.CreateOrderAsync(new CreateOrderRequest
{
    CustomerId = "abc123",
    ToAddress = "ул. Пушкина, 10"
}, headers);

Console.WriteLine(response.OrderId);

return;/////////////////////////////////////////////////////////////

var client = new LocationTrackingService.LocationTrackingServiceClient(channel);
var courierId = Guid.NewGuid();
Console.WriteLine($"Using courier id: {courierId}");

// Unary call
try
{
    var unaryResp = await client.GetCourierLocationAsync(new GetLocationRequest { CourierId = courierId.ToString() });
    Console.WriteLine($"Unary response: courier={unaryResp.CourierId}, lat={unaryResp.Latitude}, lon={unaryResp.Longitude}, last={unaryResp.LastUpdateMs}");
}
catch (Exception ex)
{
    Console.WriteLine("Unary call failed: " + ex.Message);
}

// Streaming example: send two location updates and read acks
using var call = client.StreamLocation();
var responseReader = Task.Run(async () =>
{
    try
    {
        await foreach (var resp in call.ResponseStream.ReadAllAsync())
        {
            Console.WriteLine($"Ack: success={resp.Success}, msg={resp.Message}, courier={resp.CourierId}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Response reader ended: " + ex.Message);
    }
});

await call.RequestStream.WriteAsync(new UpdateLocationRequest { CourierId = courierId.ToString(), Latitude = 55.75, Longitude = 37.61, TimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() });
await Task.Delay(200);
await call.RequestStream.WriteAsync(new UpdateLocationRequest { CourierId = courierId.ToString(), Latitude = 55.751, Longitude = 37.611, TimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() });

await call.RequestStream.CompleteAsync();

// Give some time for responses to arrive
await Task.Delay(500);

Console.WriteLine("Client finished.");
