using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Collections.Generic;
using Coflnet.Sky.Commands.Shared;
using Coflnet.Sky.Commands.MC;
using Coflnet.Sky.ModCommands.Services;

namespace Coflnet.Sky.ModCommands.Controllers
{
    /// <summary>
    /// Main Controller handling tracking
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class StatsController : ControllerBase
    {
        private FlipperService flipperService;
        private CounterService counterService;
        /// <summary>
        /// Creates a new instance of <see cref="StatsController"/>
        /// </summary>
        public StatsController(FlipperService flipperService, CounterService counterService)
        {
            this.flipperService = flipperService;
            this.counterService = counterService;
        }

        /// <summary>
        /// Indicates status of service, should be 200 (OK)
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("/status")]
        public Task TrackFlip()
        {
            return Task.CompletedTask;
        }
        [HttpGet]
        [Route("/users")]
        public IEnumerable<string> GetConnectedUserIds()
        {
            return flipperService.Connections.Select(c => {
                try
                {
                    return c.Connection.UserId;
                }
                catch (Exception e)
                {
                    return e.Message;
                }
            });
        }
        [HttpGet]
        [Route("/counter/{id}/{name}")]
        public async Task<long> GetCounter(string id, string name)
        {
            return await counterService.GetCount(id, name);
        }

        [HttpDelete]
        [Route("/users/{userId}")]
        public void KickUser(string userId)
        {
            var user = flipperService.Connections.FirstOrDefault(c => c.Connection.UserId == userId);
            if (user != null)
            {
                (user.Connection as MinecraftSocket).Close();
            }
        }
    }
}
