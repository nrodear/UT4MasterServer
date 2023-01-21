﻿using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using UT4MasterServer.Exceptions;
using UT4MasterServer.Models;

namespace UT4MasterServer.Controllers;

[ApiController, ApiExplorerSettings(IgnoreApi = true)]
[Route("api/errors")]
public class ErrorsController : ControllerBase
{
	private const string InternalServerError = "Internal server error occurred.";

	private readonly ILogger<ErrorsController> logger;

	public ErrorsController(ILogger<ErrorsController> logger)
	{
		this.logger = logger;
	}

	[HttpGet]
	public IActionResult Index()
	{
		var message = InternalServerError;
		var statusCode = 500;

		var exceptionHandlerFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
		var exception = exceptionHandlerFeature?.Error;

		logger.LogError(exception, InternalServerError);

		if (exception is not null)
		{
			switch (exception)
			{
				case InvalidEpicIDException invalidEpicIDException:
					return StatusCode(400, new ErrorResponse()
					{
						ErrorCode = invalidEpicIDException.ErrorCode,
						ErrorMessage = invalidEpicIDException.Message,
						MessageVars = new string[] { invalidEpicIDException.ID },
						NumericErrorCode = invalidEpicIDException.NumericErrorCode
					});
			}
		}

		return StatusCode(statusCode, message);
	}
}
