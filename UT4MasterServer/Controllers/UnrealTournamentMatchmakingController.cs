﻿#define USE_LOCALHOST_TEST

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using UT4MasterServer.Authentication;
using UT4MasterServer.Models;
using UT4MasterServer.Other;
using UT4MasterServer.Services;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace UT4MasterServer.Controllers;

/// <summary>
/// ut-public-service-prod10.ol.epicgames.com
/// </summary>
[ApiController]
[Route("ut/api/matchmaking")]
[AuthorizeBearer]
[Produces("application/json")]
public class UnrealTournamentMatchmakingController : JsonAPIController
{
	private readonly ILogger<SessionController> logger;
	private readonly MatchmakingService matchmakingService;
	private const int MAX_READ_SIZE = 1024 * 4;

	public UnrealTournamentMatchmakingController(ILogger<SessionController> logger, MatchmakingService matchmakingService)
	{
		this.logger = logger;
		this.matchmakingService = matchmakingService;

	}

	#region Endpoints for Game Servers

	[HttpPost("session")]
	public async Task<IActionResult> StartGameServer()
	{
		if (User.Identity is not EpicUserIdentity user)
			return Unauthorized();

		var options = new JsonSerializerOptions() { Converters = { new EpicIDJsonConverter(), new GameServerAttributesJsonConverter() } };
		var body = await Request.BodyReader.ReadAsStringAsync(MAX_READ_SIZE);
		var server = JsonSerializer.Deserialize<GameServer>(body, options);
		if (server == null)
			return BadRequest();

		server.SessionID = user.Session.ID;
		server.ID = EpicID.GenerateNew();
		server.LastUpdated = DateTime.UtcNow;

		server.ServerAddress = GetClientIPString();
		if (ipAddress == null)
		{
			// TODO: wtf!? why can this be null???
			logger.LogCritical($"Could not determine ip address of remote GameServer, this issue needs to be resolved!");
			return StatusCode(StatusCodes.Status500InternalServerError);
		}
		if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
		{
			// ipv6 does not seem to work
			logger.LogWarning($"GameServer is connecting from ipv6 address ({ipAddress})! mapping to ipv4...");
			ipAddress = ipAddress.MapToIPv4();
		}
		server.ServerAddress = ipAddress.ToString();
		server.Started = false;

		// TODO: figure out trusted keys & determine trust level
		server.Attributes.Set("UT_SERVERTRUSTLEVEL_i", (int)GameServerTrust.Untrusted);

		await matchmakingService.Add(server);

		return Json(server.ToJson(false));
	}

	[HttpPut("session/{id}")]
	public async Task<IActionResult> UpdateGameServer(string id)
	{
		if (User.Identity is not EpicUserIdentity user)
			return Unauthorized();

		var options = new JsonSerializerOptions() { Converters = { new EpicIDJsonConverter(), new GameServerAttributesJsonConverter() } };
		var body = await Request.BodyReader.ReadAsStringAsync(MAX_READ_SIZE);
		var update = JsonSerializer.Deserialize<GameServer>(body, options);
		if (update == null)
			return BadRequest();

		var old = await matchmakingService.Get(user.Session.ID, update.ID);
		if (old == null)
			return BadRequest();

		old.Update(update);

		await matchmakingService.Update(old);

		return Json(old.ToJson(false));
	}

	[HttpPost("session/{id}/start")]
	public async Task<IActionResult> NotifyGameServerIsReady(string id)
	{
		return await ChangeGameServerStarted(id, true);
	}

	[HttpPost("session/{id}/stop")]
	public async Task<IActionResult> NotifyGameServerHasStopped(string id)
	{
		return await ChangeGameServerStarted(id, false);
	}

	[HttpPost("session/{id}/heartbeat")]
	public async Task<IActionResult> GameServerHeartbeat(string id)
	{
		if (User.Identity is not EpicUserIdentity user)
			return Unauthorized();

		var server = await matchmakingService.Get(user.Session.ID, EpicID.FromString(id));
		if (server == null)
			return UnknownSessionId(id);

		server.LastUpdated = DateTime.UtcNow;
		await matchmakingService.Update(server);

		return NoContent();
	}

	[HttpPost("session/{id}/players")]
	public async Task<IActionResult> UpdateGameServerPlayers(string id)
	{
		if (User.Identity is not EpicUserIdentity user)
			return Unauthorized();

		var options = new JsonSerializerOptions() { Converters = { new EpicIDJsonConverter(), new GameServerAttributesJsonConverter() } };
		var serverOnlyWithPlayers = JsonSerializer.Deserialize<GameServer>(await Request.BodyReader.ReadAsStringAsync(MAX_READ_SIZE), options);

		if (serverOnlyWithPlayers == null)
			return NoContent();

		var serverID = EpicID.FromString(id);

		var old = await matchmakingService.Get(user.Session.ID, serverID);
		if (old == null)
			return NoContent();

		old.PublicPlayers = serverOnlyWithPlayers.PublicPlayers;
		old.PrivatePlayers = serverOnlyWithPlayers.PrivatePlayers;

		await matchmakingService.Update(old);

		return Json(old.ToJson(false));
	}

	[HttpDelete("session/{id}/players")]
	public async Task<IActionResult> RemovePlayer(string id)
	{
		if (User.Identity is not EpicUserIdentity user)
			return Unauthorized();

		var options = new JsonSerializerOptions() { Converters = { new EpicIDJsonConverter(), new GameServerAttributesJsonConverter() } };
		var players = JsonSerializer.Deserialize<EpicID[]>(await Request.BodyReader.ReadAsStringAsync(MAX_READ_SIZE), options);
		if (players == null)
			return BadRequest();

		var server = await matchmakingService.Get(user.Session.ID, EpicID.FromString(id));
		if (server == null)
			return BadRequest();

		foreach (var player in players)
		{
			server.PublicPlayers.Remove(player);
			server.PrivatePlayers.Remove(player);
		}

		return Json(server.ToJson(false));
	}

	#endregion

	#region Endpoints for Clients

	[AllowAnonymous]
	[HttpPost("session/matchMakingRequest")]
	public async Task<IActionResult> ListGameServers([FromBody] GameServerFilter filter)
	{
		if (User.Identity is not EpicUserIdentity)
		{
			logger.LogInformation($"'{Request.HttpContext.Connection.RemoteIpAddress}' accessed GameServer list without authentication");
		}

		var servers = await matchmakingService.List(filter);

		//var list = new GameServer[]
		//{
		//	new GameServer("we", "[DS]dallastn-22938", "192.223.24.243"),
		//	new GameServer("cracked", "[DS]dallastn-22938", "192.223.24.243"),
		//	new GameServer("the", "[DS]dallastn-22938", "192.223.24.243"),
		//	new GameServer("code", "[DS]dallastn-22938", "192.223.24.243"),
		//	new GameServer("and", "[DS]dallastn-22938", "192.223.24.243"),
		//	new GameServer("entered", "[DS]dallastn-22938", "192.223.24.243"),
		//	new GameServer("the", "[DS]dallastn-22938", "192.223.24.243"), // does not show, due to duplicate data
		//	new GameServer("matrix", "[DS]dallastn-22938", "192.223.24.243"),
		//};

		var arr = new JArray();
		foreach (var server in servers)
		{
#if DEBUG && USE_LOCALHOST_TEST
			server.ServerAddress = "127.0.0.1";
#endif

			arr.Add(server.ToJson(true));
		}

		return Json(arr);
	}

	/// <summary>
	/// This action is for convenience of users to be able to easily retrieve a list of hubs and/or servers.
	/// </summary>
	/// <param name="showHubs">whether to return hubs</param>
	/// <param name="showServers">whether to return servers</param>
	/// <returns></returns>
	[AllowAnonymous]
	[HttpGet("session/matchMakingRequest")]
	public async Task<IActionResult> ListGameServers([FromQuery] bool? showHubs, [FromQuery] bool? showServers)
	{
		if (User.Identity is not EpicUserIdentity)
		{
			logger.LogInformation($"'{Request.HttpContext.Connection.RemoteIpAddress}' accessed GameServer list without authentication");
		}

		// TODO: implement query filters
		return await ListGameServers(new GameServerFilter());
	}

	[HttpPost("session/{id}/join")]
	public IActionResult PlayerJoinGameServer(string id, [FromQuery(Name = "accountId")] string accountID)
	{
		if (User.Identity is not EpicUserIdentity user)
			return Unauthorized();

		EpicID eid = EpicID.FromString(id);

		// TODO: Return UnknownSessionId(id);

		// TODO: we should verify that specific user has joined specific GameServer
		//       instead of just relying on GameServer blindly believing that user
		//       really is who he says he is.
		//       Then we can probably deny user's entry to GameServer by not sending data
		//       in QueryProfile request (just a guess).

		return NoContent(); // correct response
	}

	#endregion

	[NonAction]
	private NotFoundObjectResult UnknownSessionId(string id)
	{
		return NotFound(new ErrorResponse
		{
			ErrorCode = "errors.com.epicgames.modules.matchmaking.unknown_session",
			ErrorMessage = $"unknown session id {id}",
			MessageVars = new[] { id },
			NumericErrorCode = 12101,
			OriginatingService = "utservice",
			Intent = "prod10",
		});
	}

	[NonAction]
	private async Task<IActionResult> ChangeGameServerStarted(string id, bool started)
	{
		if (User.Identity is not EpicUserIdentity user)
			return Unauthorized();

		var serverID = EpicID.FromString(id);

		var server = await matchmakingService.Get(user.Session.ID, serverID);
		if (server == null)
			return UnknownSessionId(id);

		server.Started = started;

		// Don't immediately remove it, let it become stale.
		await matchmakingService.Update(server);

		return NoContent();
	}
}
