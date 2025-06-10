using Google.Cloud.SecretManager.V1;
using System.IdentityModel.Tokens.Jwt;

using Grpc.Core;

using docker_test_sharp;

namespace dockerTest.Services
{
    public class GreeterService : docker_test_service.docker_test_serviceBase
    {
        private readonly ILogger<GreeterService> _logger;
        public GreeterService(ILogger<GreeterService> logger)
        {
            _logger = logger;
        }

		/// <summary>
		/// 테스트용 REST API 진입점입니다. 
		/// 클라이언트로부터 session_request를 받아, 지정된 gRPC endpoint로 communication 호출을 시도합니다.
		/// 통신 결과는 session_response의 Print 필드에 담아 반환합니다.
		/// </summary>
		/// <param name="_request">gRPC endpoint 주소를 포함한 session_request 객체</param>
		/// <param name="_context">gRPC 서버 호출 컨텍스트</param>
		/// <returns>통신 결과 메시지가 포함된 session_response 객체</returns>
		/// <remarks>
		/// 예외 발생 시 Print 필드에 "Communication Fail!!!" 메시지가 반환됩니다.
		/// </remarks>
		public override async Task<common_response> open(session_request _request, ServerCallContext _context)
		{
			common_response result = new common_response();
			try
			{
				var channel = Grpc.Net.Client.GrpcChannel.ForAddress(_request.Requestendpoint);
				docker_test_service.docker_test_serviceClient client = new docker_test_service.docker_test_serviceClient(channel);
				var request = new communication_request();
				request.Ip = "아무말";
				var response = await client.communicationAsync(request);
				result.Result = response.Result;
			}
			catch (Exception _excep)
			{
				result.Result = "Communication Fail!!!";
			}
			finally
			{

			}

			return result;
		}

		/// <summary>
		/// 클라이언트로부터 communication_request를 받아, Ip 필드를 포함한 간단한 응답 메시지를 반환합니다.
		/// 테스트 및 통신 확인용 엔드포인트입니다.
		/// </summary>
		/// <param name="_request">Ip 정보를 포함한 communication_request 객체</param>
		/// <param name="_context">gRPC 서버 호출 컨텍스트</param>
		/// <returns>Ip 정보가 포함된 결과 메시지를 담은 communication_response 객체</returns>
		/// <remarks>
		/// 입력된 Ip 값이 응답 메시지에 그대로 포함되어 반환됩니다.
		/// </remarks>
		public override async Task<common_response> communication(communication_request _request, ServerCallContext _context)
		{
			common_response result = new common_response();
			result.Result = $"It Comunicatied!!! {_request.Ip}";

			return result;
		}

		public override async Task<common_response> jwtcheck(session_gcp_request _request, ServerCallContext _context)
		{
			common_response result = new common_response();

			try
			{
				string jwtToken = null;
				if (_context.RequestHeaders != null)
				{
					var authHeader = _context.RequestHeaders.FirstOrDefault(h => h.Key == "authorization");
					if (authHeader != null && authHeader.Value.StartsWith("Bearer "))
					{
						jwtToken = authHeader.Value.Substring("Bearer ".Length);
					}
				}

				string nameClaim = null;
				string emailClaim = null;
				string subClaim = null;

				if (!string.IsNullOrEmpty(jwtToken))
				{
					var handler = new JwtSecurityTokenHandler();
					var jwt = handler.ReadJwtToken(jwtToken);
					nameClaim = jwt.Claims.FirstOrDefault(c => c.Type == "name")?.Value;
					emailClaim = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
					subClaim = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
				}

				// Null 처리
				nameClaim = string.IsNullOrEmpty(nameClaim) ? "Null" : nameClaim;
				emailClaim = string.IsNullOrEmpty(emailClaim) ? "Null" : emailClaim;
				subClaim = string.IsNullOrEmpty(subClaim) ? "Null" : subClaim;

				result.Result = $"name: {nameClaim}, email: {emailClaim}, sub: {subClaim}";
			}
			catch (Exception _excep)
			{
				result.Result = "JWT Check Fail!!!";
			}
			finally
			{

			}

			return result;
		}

		/// <summary>
		/// 클라이언트로부터 session_secret_request를 받아 Google Cloud Secret Manager에서 비밀 정보를 조회합니다.
		/// 요청에 포함된 프로젝트, 시크릿 이름, 버전 정보를 사용하여 시크릿 값을 검색합니다.
		/// </summary>
		/// <param name="_request">Secret Manager 접근에 필요한 프로젝트, 시크릿 이름, 버전 정보를 포함한 session_secret_request 객체</param>
		/// <param name="_context">gRPC 서버 호출 컨텍스트</param>
		/// <returns>조회된 시크릿 값 또는 오류 메시지를 담은 common_response 객체</returns>
		/// <remarks>
		/// 시크릿 조회 실패 시 "Secret Check Fail!!!" 메시지가 반환되며, 
		/// 시크릿 값이 없거나 비어있을 경우 "Null"이 반환됩니다.
		/// </remarks>
		public override async Task<common_response> secretcheck(session_secret_request _request, ServerCallContext _context)
		{
			common_response result = new common_response();

			try
			{
				string secretValue = null;
				if (!string.IsNullOrEmpty(_request.SecretManagerProject) &&
					!string.IsNullOrEmpty(_request.SecretManagerSecret) &&
					!string.IsNullOrEmpty(_request.SecretManagerVersion))
				{
					var client = await SecretManagerServiceClient.CreateAsync();
					var secretVersionName = new SecretVersionName(
						_request.SecretManagerProject,
						_request.SecretManagerSecret,
						_request.SecretManagerVersion
					);
					var secret = await client.AccessSecretVersionAsync(secretVersionName);
					secretValue = secret.Payload.Data.ToStringUtf8();
				}

				// Null 처리
				result.Result = string.IsNullOrEmpty(secretValue) ? "Null" : secretValue;
			}
			catch (Exception _excep)
			{
				result.Result = "Secret Check Fail!!!";
			}
			finally
			{

			}

			return result;
		}

	}
}
