using dockerTest;
using dockerTest.Services;

/*
 * Windows Powershell 에서 테스트 명령 : Invoke-RestMethod -Uri "http://localhost:32775/v1/open" -Method Post -ContentType "application/json" -Body '{"requestendpoint":"http://127.0.0.1:50051"}'
 * ip와 포트는 모두 매핑된 외부 포트 기준으로 작성
 * 실 환경이 갖춰지면, 첫 주소는 API Gateway로, 두 번재는 GKE ingress 혹은 Load Balance 등의 외부 노출 값으로 사용.
 */

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc(options => { options.EnableDetailedErrors = true; }).AddJsonTranscoding();

builder.Services.AddControllers(); // REST API용 컨트롤러 서비스 추가

builder.WebHost.ConfigureKestrel(options =>
{
	// gRPC (HTTP/2) on 50051
	options.ListenAnyIP(50051, listenOptions =>
	{
		listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
	});

	// REST API (HTTP/1) on 5000
	options.ListenAnyIP(5000, listenOptions =>
	{
		listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1;
	});
});

var app = builder.Build();

// gRPC 서비스 매핑
app.MapGrpcService<GreeterService>();

// REST API 엔드포인트 매핑
app.MapControllers();

// 기본 HTTP GET 엔드포인트 (REST API 포트에서만 동작)
app.MapGet("/", () => "REST API is running. For gRPC, connect to port 50051.");

app.Run();