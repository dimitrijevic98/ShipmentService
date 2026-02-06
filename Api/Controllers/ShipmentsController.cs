using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Models;
using Application.ShipmentServices.Commands;
using Application.ShipmentServices.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    public class ShipmentsController : BaseController
    {
        [HttpPost("create-shipment")]
        public async Task<ActionResult<Result<Guid>>> CreateShipment([FromBody] CreateShipment command)
        {
            return Ok(await Mediator.Send(command));
        }

        [HttpPost("{id}/label")]
        public async Task<ActionResult<Result<Guid>>> UploadLabel(Guid id, IFormFile file)
        {
            var command = new UploadLabel
            {
                ShipmentId = id,
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileStream = file.OpenReadStream(),
                FileSize = file.Length

            };

            return Ok(await Mediator.Send(command));
        } 

        [HttpGet]
        public async  Task<ActionResult<Result<PagedResult<ShipmentDTO>>>> GetAllShipments()
        {
            return await Mediator.Send(new GetAllShipments.Query());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Result<ShipmentDetailsDTO>>> GetShipmentDetails(Guid id)
        {
            return await Mediator.Send(new GetShipmentDetails.Query{ Id = id });
        }

        [HttpGet("{id}/events")]
        public async Task<ActionResult<Result<List<ShipmentEventDTO>>>> GetShipmentEvents(Guid id)
        {
            return await Mediator.Send(new GetShipmentEvents.Query{ Id = id });
        }
    }
}