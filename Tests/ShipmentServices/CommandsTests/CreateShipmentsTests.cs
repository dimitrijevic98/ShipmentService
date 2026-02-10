using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;
using Application.ShipmentServices.Commands;
using Domain.Entities;
using Domain.Enums;
using Moq;
using Xunit;
using NSubstitute;
using FluentAssertions;
using System.Threading;
using Application.Models;

namespace Tests.ShipmentServices.CommandsTests
{
    public class CreateShipmentsTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly CreateShipmentHandler _handler;

        public CreateShipmentsTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            
            _handler = new CreateShipmentHandler(_unitOfWork);
        }

        [Fact]
        public async Task Handle_InvalidValidation_ReturnsFailure()
        {
            // Arrange
            var command = new CreateShipment
            {
                ReferenceNumber = "", // invalid
                SenderName = "",
                RecipientName = ""  
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Validation failed");
            result.Errors.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Handle_ReferenceNumberAlreadyExists_ReturnsFailure()
        {
            // Arrange
            var command = new CreateShipment
            {
                ReferenceNumber = "REF123",
                SenderName = "Marko",
                RecipientName = "Petar"
            };

            _unitOfWork.Shipments.ShipmentRefNumExists(command.ReferenceNumber).Returns(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be($"Shipment with reference number {command.ReferenceNumber} already exists.");
        }

        [Fact]
        public async Task Handle_SaveChangesFails_ThrowsException()
        {
            // Arrange
            var command = new CreateShipment
            {
                ReferenceNumber = "REF123",
                SenderName = "Marko",
                RecipientName = "Petar"
            };

            _unitOfWork.Shipments.ShipmentRefNumExists(command.ReferenceNumber).Returns(false);

            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(0);

            // Act & Assert
            var act = async () => await _handler.Handle(command, CancellationToken.None);
            
            await act.Should().ThrowAsync<Exception>().WithMessage("Failed to create shipment.");
        }

        [Fact]
        public async Task Handle_ValidRequest_CreatesShipmentSuccessfully()
        {
            // Arrange
            var command = new CreateShipment
            {
                ReferenceNumber = "REF123",
                SenderName = "Marko",
                RecipientName = "Petar"
            };

            _unitOfWork.Shipments.ShipmentRefNumExists(command.ReferenceNumber).Returns(false);

            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Be("Shipment created successfully.");
        }
    }
}