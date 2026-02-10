using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;
using Application.ShipmentServices.Queries;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Xunit;
using static Application.ShipmentServices.Queries.GetShipmentDetails;

namespace Tests.ShipmentServices.QueriesTests
{
    public class GetShipmentDetailsTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly GetShipmentDetailsHandler _handler;

        public GetShipmentDetailsTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _handler = new GetShipmentDetailsHandler(_unitOfWork);
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
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenShipmentNotFound()
        {
            // Arrange
            var query = new Query { Id = Guid.NewGuid() };
            
            _unitOfWork.Shipments.GetShipmentByIdAsync(query.Id)
                .ReturnsNull();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be($"Shipment with ID {query.Id} not found.");
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccess_WhenShipmentExists()
        {
            // Arrange
            var shipmentId = Guid.NewGuid();
            var shipment = new Shipment
            {
                Id = shipmentId,
                ReferenceNumber = "REF123",
                SenderName = "Alice",
                RecipientName = "Bob",
                State = ShipmentState.Created,
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                UpdatedAt = DateTime.UtcNow,
                ShipmentEvents = new List<ShipmentEvent>
                {
                    new ShipmentEvent
                    {
                        EventCode = "CREATED",
                        EventTime = DateTime.UtcNow.AddMinutes(-30),
                        CorrelationId = Guid.NewGuid().ToString()
                    },
                    new ShipmentEvent
                    {
                        EventCode = "LABEL_UPLOADED",
                        EventTime = DateTime.UtcNow,
                        CorrelationId = Guid.NewGuid().ToString()
                    }
                }
            };

            var query = new Query { Id = shipmentId };
            _unitOfWork.Shipments.GetShipmentByIdAsync(shipmentId).Returns(shipment);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Be("Shipment details retrieved successfully.");
            result.Value.Should().NotBeNull();
            result.Value.Id.Should().Be(shipmentId);
            result.Value.ReferenceNumber.Should().Be("REF123");
            result.Value.ShipmentEvents.Should().HaveCount(2);
            result.Value.LastStatus.EventCode.Should().Be("LABEL_UPLOADED");
        }
    }
}