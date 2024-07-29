using aeroports;
using BLL.Shared;
using BLL.Shared.Cash;
using BLL.Shared.RabbitMessages;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class MainService : IMainService, IDisposable
    {        
        private readonly IConnection _connection;
        private readonly IModel _taskInfoModel;
        private readonly IModel _requestRepeatModel;        
        private readonly IMemoryCache _cache;
        private readonly ILogger<MainService> _logger;
        IComputingService _computingService;
        private const string exchange = "AirportExchange";

        public MainService(IRabbitMQService rabbitMQService, IMemoryCache memoryCach, ILogger<MainService> logger, IComputingService computingService)
        {
            _computingService = computingService;
            _cache = memoryCach;
            _logger = logger;
            _connection = rabbitMQService.CreateChannel();
            _taskInfoModel = _connection.CreateModel();
            _taskInfoModel.QueueDeclare(AppConstants.AeroportJobs_Queue, durable: true, exclusive: false, autoDelete: false);
            _taskInfoModel.ExchangeDeclare(exchange, ExchangeType.Fanout, durable: true, autoDelete: false);
            _taskInfoModel.QueueBind(AppConstants.AeroportJobs_Queue, exchange, string.Empty);
            
            _requestRepeatModel = _connection.CreateModel();
            _requestRepeatModel.QueueDeclare(AppConstants.Requests_Queue, durable: true, exclusive: false, autoDelete: false);
            _requestRepeatModel.ExchangeDeclare(exchange, ExchangeType.Fanout, durable: true, autoDelete: false);
            _requestRepeatModel.QueueBind(AppConstants.Requests_Queue, exchange, string.Empty);
        }

        #region publishers
        /// <summary>
        /// Создать задачу на новый реквест
        /// </summary>
        /// <param name="FromIATACode"></param>
        /// <param name="ToIATACode"></param>
        /// <returns></returns>
        public string Post(string FromIATACode, string ToIATACode)
        {
            IBasicProperties props = _taskInfoModel.CreateBasicProperties();         

            var aeroportJob = new AeroportsJob{ FromIATACode = FromIATACode, ToIATACode = ToIATACode, Id = (new Guid()).ToString()};
            _cache.Set(aeroportJob.Id, aeroportJob,
                    new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromDays(1)));

            var messageBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(aeroportJob));
            _taskInfoModel.BasicPublish(exchange: string.Empty,                
                routingKey: AppConstants.AeroportJobs_Queue,                
                basicProperties: props,
                body: messageBytes
                );
            return aeroportJob.Id;
        }

        public void PublishRequest(string IATACode, IModel model, string route, string dependedTask = null) { //надо отдельно?
            _cache.TryGetValue(IATACode, out AeroportsTask aeroportTask);

            if (aeroportTask == null)
            {
                aeroportTask = new AeroportsTask { IataCode = IATACode };
            }

            if (dependedTask != null) {
                if (aeroportTask.DependedJobs == null)
                {
                    aeroportTask.DependedJobs = new List<string> { dependedTask };
                }
                else {
                    aeroportTask.DependedJobs.Add(dependedTask);
                }
            }

            if (aeroportTask.Location == null)
            {
                PublishRequest(aeroportTask, model, route);
            }
        }

        public void PublishRequest(AeroportsTask aeroportsTask, IModel model, string route)
        {
            if (aeroportsTask == null) {
                throw new ArgumentNullException("aeroportsTask");
            }

            _cache.Set(aeroportsTask.IataCode, aeroportsTask,
                    new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromDays(1)));

            if (aeroportsTask.Location == null)
            {
                IBasicProperties props = model.CreateBasicProperties();
                
                var messageBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(aeroportsTask));
                _requestRepeatModel.BasicPublish(exchange: string.Empty,
                    routingKey: route,
                    basicProperties: props,
                    body: messageBytes
                    );
            }
        }
        #endregion

        #region receivers        
        public void RepeatedCallProcessing()
        {
            var consumer = new AsyncEventingBasicConsumer(_requestRepeatModel);
            consumer.Received += async (bc, ea) =>
            {
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                if (!string.IsNullOrEmpty(message))
                {
                    try
                    {
                        var iataTask = JsonConvert.DeserializeObject<AeroportsTask>(message);
                        var t = Task.Run(async delegate
                        {
                            await Task.Delay(TimeSpan.FromSeconds(iataTask.Timeout));
                            var aeroport = await CallAPI.GetAeroportAsync(iataTask.IataCode);
                            _cache.TryGetValue(iataTask.IataCode, out AeroportsTask savedAeroportsTask);
                            switch (aeroport.Result)
                            {
                                case TypeResult.Success:
                                    if (aeroport.Airport == null)
                                    {
                                        throw new NullReferenceException("Airport");
                                    }

                                    savedAeroportsTask.Location = aeroport.Airport.location;
                                    _cache.Set(iataTask.IataCode, savedAeroportsTask,
                                           new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromDays(1)));

                                    if (savedAeroportsTask?.DependedJobs != null)
                                    {
                                        for ( var i = savedAeroportsTask.DependedJobs.Count-1; i >=0; i--) {
                                            var dependJobId = savedAeroportsTask.DependedJobs[i];
                                            _cache.TryGetValue(dependJobId, out AeroportsJob aeroportsJob);
                                            var distance = _computingService.CountDistance(aeroportsJob);
                                            if (distance != null) {
                                                savedAeroportsTask.DependedJobs.RemoveAt(i);
                                                _cache.Set(iataTask.IataCode, savedAeroportsTask,
                                                    new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromDays(1)));
                                            }
                                        }                                        
                                    }                                    

                                    break;
                                case TypeResult.Failed:
                                    if (savedAeroportsTask == null) {
                                        savedAeroportsTask = new AeroportsTask { IataCode = iataTask.IataCode};
                                    }
                                    savedAeroportsTask.Timeout = (ushort)(savedAeroportsTask.Timeout + 3);
                                    _cache.Set(iataTask.IataCode, savedAeroportsTask,
                                           new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromDays(1)));
                                    PublishRequest(iataTask.IataCode, _requestRepeatModel, AppConstants.Requests_Queue);
                                    break;
                                case TypeResult.BadRequest: //Удалить все связанные задачи
                                    _cache.Remove(iataTask.IataCode);
                                    break;
                            }
                        });
                    }
                    catch (Newtonsoft.Json.JsonException)
                    {
                        _logger.LogError($"JSON Parse Error: '{message}'.");
                    }
                    catch (AlreadyClosedException)
                    {
                        _logger.LogInformation("RabbitMQ is closed!");
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(default, e, e.Message);
                        _requestRepeatModel.BasicNack(ea.DeliveryTag, false, false);
                    }
                    finally
                    {
                        _requestRepeatModel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    }
                }
            };

            _requestRepeatModel.BasicConsume(queue: AppConstants.Requests_Queue, autoAck: false, consumer: consumer);
        }

        public void TaskProcessing() {
            var consumer = new AsyncEventingBasicConsumer(_taskInfoModel);
            consumer.Received += async (bc, ea) =>
            {
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                if (!string.IsNullOrEmpty(message))
                {
                    try
                    {
                        var job = JsonConvert.DeserializeObject<AeroportsJob>(message);
                        {
                            if (job != null && job.Distance == null)
                            {
                                _cache.TryGetValue(job.FromIATACode, out AeroportsTask fromAeroportTask);
                                _cache.TryGetValue(job.ToIATACode, out AeroportsTask toAeroportTask);
                                if (fromAeroportTask?.Location != null && toAeroportTask?.Location != null)
                                {
                                    _computingService.CountDistance(job);
                                    return;
                                }

                                if (fromAeroportTask == null || fromAeroportTask.Location == null) { 
                                    PublishRequest(job.FromIATACode, _requestRepeatModel, AppConstants.Requests_Queue, job.Id);
                                }

                                if (toAeroportTask == null || toAeroportTask.Location == null)
                                {
                                    PublishRequest(job.ToIATACode, _requestRepeatModel, AppConstants.Requests_Queue, job.Id);
                                }
                            }
                        }
                    }
                    catch (Newtonsoft.Json.JsonException)
                    {
                        _logger.LogError($"JSON Parse Error: '{message}'.");
                    }
                    catch (AlreadyClosedException)
                    {
                        _logger.LogInformation("RabbitMQ is closed!");
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(default, e, e.Message);
                        _taskInfoModel.BasicNack(ea.DeliveryTag, false, false);
                    }
                    finally
                    {
                        _taskInfoModel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    }
                }
            };

            _taskInfoModel.BasicConsume(queue: AppConstants.AeroportJobs_Queue, autoAck: false, consumer: consumer);
        }
        #endregion

        public GetDistanceResultDto Get(string id)
        {
            if (!_cache.TryGetValue(id, out AeroportsJob aeroportsTask))
            {
                return new GetDistanceResultDto { isError = true, Rezult = "Задача не найдена" };
            }

            return new GetDistanceResultDto { isError = aeroportsTask.isError, Rezult = aeroportsTask.Distance.ToString()};
        }

        public void Dispose()
        {
            if (_taskInfoModel.IsOpen)
                _taskInfoModel.Close();            
            if (_requestRepeatModel.IsOpen)
                _requestRepeatModel.Close();
            if (_connection.IsOpen)
                _connection.Close();
        }
    }
}
