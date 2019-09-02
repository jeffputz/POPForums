﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PopForums.Configuration;
using PopForums.Models;

namespace PopForums.Services
{
	public interface IReCaptchaService
	{
		Task<ReCaptchaResponse> VerifyToken(string token, string ip);
	}

	public class ReCaptchaService : IReCaptchaService
	{
		private readonly IConfig _config;
		private readonly ISecurityLogService _securityLogService;
		private const string VerifyUrl = "https://www.google.com/recaptcha/api/siteverify";

		public ReCaptchaService(IConfig config, ISecurityLogService securityLogService)
		{
			_config = config;
			_securityLogService = securityLogService;
		}

		public async Task<ReCaptchaResponse> VerifyToken(string token, string ip)
		{
			var values = new Dictionary<string, string>
			{
				{"secret", _config.ReCaptchaSecretKey},
				{"response", token},
				{"ip", ip}
			};
			HttpResponseMessage httpResult;
			using (var client = new HttpClient())
			{
				try
				{
					httpResult = await client.PostAsync(VerifyUrl, new FormUrlEncodedContent(values));
				}
				catch (HttpRequestException)
				{
					return new ReCaptchaResponse {IsSuccess = false, ErrorCodes = new[] { "HttpRequestException" } };
				}
			}
			var content = await httpResult.Content.ReadAsStringAsync();
			var verifyResult = JsonConvert.DeserializeObject<ReCaptchaResponse>(content);
			if (!verifyResult.IsSuccess)
				await _securityLogService.CreateLogEntry((User) null, null, ip, string.Empty, SecurityLogType.ReCaptchaFailed);
			return verifyResult;
		}
	}

	public class ReCaptchaResponse
	{
		[JsonProperty("success")]
		public bool IsSuccess { get; set; }
		[JsonProperty("challenge_ts")]
		public DateTime ChallengeTimeStamp { get; set; }
		[JsonProperty("hostname")]
		public string HostName { get; set; }
		[JsonProperty("error-codes")]
		public string[] ErrorCodes { get; set; }
	}
}