using System.Diagnostics;
using System.Net;
using Common.Shared.DTOs;
using Common.Shared.Events;
using MassTransit;
using OpenTelemetry.Shared;
using Order.API.Models;
using Order.API.RedisServices;
using Order.API.StockServices;

namespace Order.API.OrderServices;

public class OrderService
{
    private readonly AppDbContext _context;
    private readonly RedisService _redisService;
    private readonly StockService _stockService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<OrderService> _logger;

    public OrderService(AppDbContext context, StockService stockService, RedisService redisService,
        IPublishEndpoint publishEndpoint, ILogger<OrderService> logger)
    {
        _context = context;
        _stockService = stockService;
        _redisService = redisService;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<ResponseDto<OrderCreateResponseDto>> CreateAsync(OrderCreateRequestDto request)
    {
        _logger.LogInformation("Creating new order request from user {@userId}", request.UserId);
        // _logger.LogInformation("Testim {@aaaa}", request.Items.Count); // {} bunun icinde verdiyimiz paramlar elk dusur field's kimi
        // _logger.LogInformation("Testim {bbbb}", request.Items.Count);
        _logger.LogInformation("Testim1 {@eeee}", request); //@ isaresi obyektlerde olanda icindeki fieldleri de goturu structurd log eleyir
        _logger.LogInformation("Testim2 {dddd}", request);
        // using (var redisActivity =
        //        ActivitySourceProvider.Source.StartActivity("RedisStringSetGet")) 
        //     //bunu bele yazmaya bilerik, onsuz hazir insturment qosmusuq sadece ornekdi bu da, elave nese data yazmaq istesek
        // {
        //     // redis için örnek kod
        //     await _redisService.GetDb(0).StringSetAsync("userId", request.UserId);
        //
        //     redisActivity.SetTag("userId", request.UserId);
        //
        //     var redisUserId = _redisService.GetDb(0).StringGetAsync("UserId");
        // }

        await _redisService.GetDb(0).StringSetAsync("userId", request.UserId);

        var redisUserId = _redisService.GetDb(0).StringGetAsync("UserId");

        Activity.Current?.SetTag("Asp.Net Core(instrumentation) tag1",
            "Asp.Net Core(instrumentation) tag value"); // hazirki olan activitye ( boyuk ehtimal aspnet insturment'in activitisine) data yazacaq
        using var
            activity = ActivitySourceProvider.Source
                .StartActivity(); //oz activity sourcemiz specifik veziyetler ucun amma bu child span kimi dusecek, cunki aspnet insturment requesti evvelceden trace etmeye baslayir
        activity?.AddEvent(new ActivityEvent("Sipariş süreci başladı."));


        activity?.SetBaggage("userId", request.UserId.ToString()); //dasinacaq data otl headerine qoyuruq
        var newOrder = new Order
        {
            Created = DateTime.Now,
            OrderCode = Guid.NewGuid().ToString(),
            Status = OrderStatus.Success,
            UserId = request.UserId,
            Items = request.Items.Select(x => new OrderItem
            {
                Count = x.Count,
                ProductId = x.ProductId,
                UnitPrice = x.UnitPrice
            }).ToList()
        };

        _context.Orders.Add(newOrder);
        await _context.SaveChangesAsync();

        // await _publishEndpoint.Publish(new OrderCreatedEvent()
        // {
        //     OrderCode =  newOrder.OrderCode
        // });

        StockCheckAndPaymentProcessRequestDto stockRequest = new();

        stockRequest.OrderCode = newOrder.OrderCode;
        stockRequest.OrderItems = request.Items;

        var (isSuccess, failMessage) = await _stockService.CheckStockAndPaymentStartAsync(stockRequest);

        if (!isSuccess)
            return ResponseDto<OrderCreateResponseDto>.Fail(HttpStatusCode.InternalServerError.GetHashCode(),
                failMessage!);

        activity?.AddEvent(new ActivityEvent("Sipariş süreci tamamlandı."));

        return ResponseDto<OrderCreateResponseDto>.Success(HttpStatusCode.OK.GetHashCode(),
            new OrderCreateResponseDto { Id = newOrder.Id });
    }
}