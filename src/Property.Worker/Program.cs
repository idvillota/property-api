using Property.Application;
using Property.Application.Common.Options;
using Property.Infrastructure;
using Property.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<OutboxDispatchHostedService>();

var host = builder.Build();
host.Run();
