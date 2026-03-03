using Common.Shared.Events;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Order.API.OrderServices;

namespace Order.Api.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class OrderController : ControllerBase
{
    private readonly OrderService _orderService;
    private readonly IPublishEndpoint _publishEndpoint;

    public OrderController(OrderService orderService, IPublishEndpoint publishEndpoint)
    {
        _orderService = orderService;
        _publishEndpoint = publishEndpoint;
    }

    [HttpPost]
    public async Task<IActionResult> Create(OrderCreateRequestDto request, [FromQuery] string queryParam= "d")
    {
        var result = await _orderService.CreateAsync(request);

        #region Third-party api istek örneği

        //var httpClient = new HttpClient();

        //var response = await httpClient.GetAsync("https://jsonplaceholder.typicode.com/todos/1");

        //var content = await response.Content.ReadAsStringAsync(); 

        #endregion


        return new ObjectResult(result) { StatusCode = result.StatusCode };

        #region Exception örneği için hazırlandı.

        // var a = 10;
        // var b = 0;
        // var c = a / b;
        //
        // return Ok();

        #endregion
    }

    [HttpGet]
    public async Task<IActionResult> SendOrderCreatedEvent()
    {
        // Queue mesaj gondermeni burda test eleyirem, oder servicesinde qarisiqliq olmasin deye

        await _publishEndpoint.Publish(new OrderCreatedEvent() { OrderCode = Guid.NewGuid().ToString() });

        return Ok();
    }
}