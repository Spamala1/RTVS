﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.R.Host.Broker.Interpreters;
using Microsoft.R.Host.Broker.Pipes;
using Microsoft.R.Host.Broker.Security;
using Microsoft.R.Host.Protocol;

namespace Microsoft.R.Host.Broker.Sessions {
    [Authorize(Policy = Policies.RUser)]
    [Route("/sessions")]
    public class SessionsController : Controller {
        private readonly InterpreterManager _interpManager;
        private readonly SessionManager _sessionManager;

        public SessionsController(InterpreterManager interpManager, SessionManager sessionManager) {
            _interpManager = interpManager;
            _sessionManager = sessionManager;
        }

        [HttpGet]
        public Task<IEnumerable<SessionInfo>> GetAsync() => Task.FromResult(_sessionManager.GetSessions(User.Identity).Select(s => s.Info));

        [HttpPut("{id}")]
        public Task<IActionResult> PutAsync(string id, [FromBody] SessionCreateRequest request) {
            if (!_interpManager.Interpreters.Any()) {
                return Task.FromResult<IActionResult>(new ApiErrorResult(HttpContext.Response, BrokerApiError.NoRInterpreters));
            }

            string profilePath = User.FindFirst(Claims.RUserProfileDir)?.Value;

            Interpreter interp;
            if (!string.IsNullOrEmpty(request.InterpreterId)) {
                interp = _interpManager.Interpreters.FirstOrDefault(ip => ip.Id == request.InterpreterId);
                if (interp == null) {
                    return Task.FromResult<IActionResult>(new ApiErrorResult(HttpContext.Response, BrokerApiError.InterpreterNotFound));
                }
            } else {
                interp = _interpManager.Interpreters.First();
            }

            try {
                var session = _sessionManager.CreateSession(User.Identity, id, interp, User, profilePath, request.CommandLineArguments);
                return Task.FromResult<IActionResult>(new ObjectResult(session.Info));
            } catch (Exception ex) {
                return Task.FromResult<IActionResult>(new ApiErrorResult(HttpContext.Response, BrokerApiError.UnableToStartRHost, ex.Message));
            }
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(string id) {
            var session = _sessionManager.GetSession(User.Identity, id);
            if (session == null) {
                return NotFound();
            }

            try {
                session.KillHost();
            } catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException) {
                return new ApiErrorResult(HttpContext.Response, BrokerApiError.UnableToTerminateRHost, ex.Message);
            } finally {
                session.State = SessionState.Terminated;
            }

            return Ok();
        }

        [HttpGet("{id}/pipe")]
        public IActionResult GetPipe(string id) {
            var session = _sessionManager.GetSession(User.Identity, id);
            if (session?.Process?.HasExited ?? true) {
                return NotFound();
            }

            IMessagePipeEnd pipe;
            try {
                pipe = session.ConnectClient();
            } catch (InvalidOperationException) {
                return new ApiErrorResult(HttpContext.Response, BrokerApiError.PipeAlreadyConnected);
            }

            return new WebSocketPipeAction(id, pipe);
        }
    }
}
