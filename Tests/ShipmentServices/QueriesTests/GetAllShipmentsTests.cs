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
using static Application.ShipmentServices.Queries.GetAllShipments;

namespace Tests.ShipmentServices.QueriesTests
{
    public class GetAllShipmentsTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly GetAllShipmentsHandler _handler;
        public GetAllShipmentsTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _handler = new GetAllShipmentsHandler(_unitOfWork);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenValidationFails()
        {
            // Arrange
            var invalidQuery = new Query
            {
                Page = 0
            };

            // Act
            var result = await _handler.Handle(invalidQuery, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Validation failed");
            result.Errors.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenShipmentsAreNull()
        {
            // Arrange
            var query = new Query();

            _unitOfWork.Shipments.GetAllShipmentsAsync(query.Page, query.PageSize, query.State)
                .ReturnsNull();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Shipment list is null.");
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccess_WhenShipmentsExist()
        {
            // Arrange
            var query = new Query();
            var shipments = new List<Shipment>
            {
                new Shipment
                {
                    ReferenceNumber = "REF001",
                    SenderName = "Alice",
                    RecipientName = "Bob",
                    State = ShipmentState.Created,
                    CreatedAt = DateTime.UtcNow
                },
                new Shipment
                {
                    ReferenceNumber = "REF002",
                    SenderName = "Charlie",
                    RecipientName = "Dave",
                    State = ShipmentState.Created,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _unitOfWork.Shipments.GetAllShipmentsAsync(query.Page, query.PageSize, query.State)
                .Returns(shipments);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Be("Shipments retrieved successfully.");
            result.Value.TotalCount.Should().Be(2);
            result.Value.Items.First().ReferenceNumber.Should().Be("REF001");
        }

    }
}