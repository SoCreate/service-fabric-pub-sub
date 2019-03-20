using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PubSubDemo.SampleEvents;
using ServiceFabric.PubSubActors.Helpers;
using ServiceFabric.PubSubActors.State;

namespace PubSubDemo.Api.Controllers
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
        public async Task<ActionResult<Dictionary<string, List<QueueStats>>>> Get()
        {
            try
            {
                return await _brokerClient.GetBrokerStatsAsync();
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }

        // Publish {num} SampleEvents to the BrokerService
        // POST api/broker/publish/5
        [HttpPost("publish/{num}")]
        public async Task<string> Publish(int num)
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
                return $"Published {num} messages in {sw.ElapsedMilliseconds}ms";
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        // DELETE api/broker/queue/PubSubDemo.SampleEvents.SampleEvent_-746413431
        [HttpDelete("queue/{queueName}")]
        public async Task<string> Delete(string queueName)
        {
            try
            {
                await _brokerClient.UnsubscribeByQueueNameAsync(queueName);
                return "";
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }
     }
}
