using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SoCreate.ServiceFabric.PubSub;
using SoCreate.ServiceFabric.PubSubDemo.SampleEvents;
using SoCreate.ServiceFabric.PubSub.State;

namespace SoCreate.ServiceFabric.PubSubDemo.Api.Controllers
{
    [Route("api/[controller]")]
    public class BrokerController : Controller
    {
        private readonly IBrokerClient _brokerClient;

        public BrokerController(IBrokerClient brokerClient)
        {
            _brokerClient = brokerClient;
        }

        // GET api/broker/stats
        [HttpGet("stats")]
        public async Task<ActionResult<Dictionary<string, IEnumerable<QueueStats>>>> Get([FromQuery] BrokerStatsQueryParams queryParams)
        {
            try
            {
                return (await _brokerClient.GetBrokerStatsAsync())
                    .Where(item => queryParams.MessageType == null || item.Key.Contains(queryParams.MessageType))
                    .ToDictionary(item => item.Key, item => item.Value.Where(i => i.Time > queryParams.FromTime &&
                        (queryParams.ServiceName == null || i.ServiceName.Contains(queryParams.ServiceName))));
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }

        // Publish {num} SampleEvents to the BrokerService
        // POST api/broker/publish/5
        [HttpPost("publish/{num}")]
        public async Task<IActionResult> Publish(int num)
        {
            try
            {
                var tasks = new List<Task>(num);
                Stopwatch sw = Stopwatch.StartNew();
                for (var i = 1; i <= num; i++)
                {
                    tasks.Add(_brokerClient.PublishMessageAsync(new SampleEvent
                    {
                        Message = $"SampleEvent #{i}"
                    }));
                }

                await Task.WhenAll(tasks);
                sw.Stop();
                return Ok($"Published {num} messages in {sw.ElapsedMilliseconds}ms");
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        // Publish {num} SampleUnorderedEvents to the BrokerService
        // POST api/broker/unordered/publish/5
        [HttpPost("unordered/publish/{num}")]
        public async Task<IActionResult> PublishUnordered(int num)
        {
            try
            {
                var tasks = new List<Task>(num);
                Stopwatch sw = Stopwatch.StartNew();
                for (var i = 1; i <= num; i++)
                {
                    tasks.Add(_brokerClient.PublishMessageAsync(new SampleUnorderedEvent
                    {
                        Message = $"SampleUnorderedEvent #{i}"
                    }));
                }

                await Task.WhenAll(tasks);
                sw.Stop();
                return Ok($"Published {num} messages in {sw.ElapsedMilliseconds}ms");
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        // Publish the given event to the BrokerService.
        // Provide the message body in json and the Message-Type (FullName of message type) and Assembly-Name in headers.
        // POST api/broker/publish
        [HttpPost("publish")]
        public async Task<IActionResult> Publish([FromBody] object messageBody)
        {
            try
            {
                var json = JsonConvert.SerializeObject(messageBody);
                if (!Request.Headers.TryGetValue("Message-Type", out var typeString))
                {
                    return BadRequest("Message-Type header is required");
                }
                if (!Request.Headers.TryGetValue("Assembly-Name", out var assemblyName))
                {
                    assemblyName = typeString.ToString().Remove(typeString.ToString().LastIndexOf('.'));
                }

                var type = Assembly.Load(assemblyName).GetType(typeString, false);
                if (type == null)
                {
                    return BadRequest($"Unable to find type {typeString} in assembly {assemblyName}");
                }

                await _brokerClient.PublishMessageAsync(JsonConvert.DeserializeObject(json, type));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }

            return Ok();
        }

        // DELETE api/broker/queue/SoCreate.ServiceFabric.PubSubDemo.SampleEvents.SampleEvent_-746413431
        [HttpDelete("queue/{queueName}")]
        public async Task<IActionResult> Delete(string queueName)
        {
            try
            {
                await _brokerClient.UnsubscribeByQueueNameAsync(queueName);
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
     }

    public class BrokerStatsQueryParams
    {
        public DateTime FromTime { get; set; }
        public string ServiceName { get; set; }
        public string MessageType { get; set; }
    }
}
