using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;
using Application.ShipmentServices.Queries;
using Domain.Entities;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Xunit;
using static Application.ShipmentServices.Queries.GetShipmentEvents;

namespace Tests.ShipmentServices.QueriesTests
{
    public class GetShipmentEventsTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly GetShipmentEventsHandler _handler;

        public GetShipmentEventsTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _handler = new GetShipmentEventsHandler(_unitOfWork);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenValidationFails()
        {
            // Arrange
            var invalidQuery = new Query
            {
                Id = Guid.Empty
            };

            // Act
            var result = await _handler.Handle(invalidQuery, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Validation failed");
            result.Errors.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenShipmentEventsAreNull()
        {
            // Arrange
            var query = new Query { Id = Guid.NewGuid() };
            
            _unitOfWork.ShipmentEvents.GetShipmentEventsByShipmentIdAsync(query.Id)
                .ReturnsNull();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Shipment events not found.");
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccess_WhenShipmentEventsExist()
        {
            // Arrange
            var shipmentId = Guid.NewGuid();
            var events = new List<ShipmentEvent>
            {
                new ShipmentEvent
                {
                    EventCode = "CREATED",
                    EventTime = DateTime.UtcNow.AddMinutes(-10),
                    CorrelationId = Guid.NewGuid().ToString()
                },
                new ShipmentEvent
                {
                    EventCode = "LABEL_UPLOADED",
                    EventTime = DateTime.UtcNow,
                    CorrelationId = Guid.NewGuid().ToString()
                }
            };

            var query = new Query { Id = shipmentId };
            _unitOfWork.ShipmentEvents.GetShipmentEventsByShipmentIdAsync(shipmentId).Returns(events);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Be("Shipment events retrieved successfully.");
            result.Value.Should().HaveCount(2);
            result.Value.First().EventCode.Should().Be("CREATED");
            result.Value.Last().EventCode.Should().Be("LABEL_UPLOADED");
        }
    }
}