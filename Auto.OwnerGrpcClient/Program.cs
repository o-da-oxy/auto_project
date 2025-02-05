﻿using Auto.OwnerGrpcService;
using Grpc.Net.Client;

using var channel = GrpcChannel.ForAddress("https://localhost:7258");
var grpcClient = new OwnerService.OwnerServiceClient(channel);
Console.WriteLine("Ready! Input registration: ");
while (true)
{
    var registration = Console.ReadLine();
    var request = new OwnerRequest()
    {
        Registration = registration
    };
    var reply = grpcClient.GetOwner(request);
    Console.WriteLine($"Owner:  {reply.FirstName}  {reply.LastName}  {reply.PhoneNumber}");
    Console.WriteLine("Input registration: ");
}