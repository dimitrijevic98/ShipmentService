using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;
using Application.ShipmentServices.Commands;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;
using Xunit;

namespace Tests.ShipmentServices.CommandsTests
{
    public class UploadLabelTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBlobService _blobService;
        private readonly IServiceBus _serviceBus;
        private readonly ILogger<UploadLabelHandler> _logger;
        private readonly IDbContextTransaction _transaction;
        private readonly UploadLabelHandler _handler;

        public UploadLabelTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _blobService = Substitute.For<IBlobService>();
            _serviceBus = Substitute.For<IServiceBus>();
            _logger = Substitute.For<ILogger<UploadLabelHandler>>();
            _transaction = Substitute.For<IDbContextTransaction>();
            _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>())
                       .Returns(_transaction);

            _handler = new UploadLabelHandler(
                _unitOfWork,
                _blobService,
                _serviceBus,
                _logger);
        }

        private UploadLabel CreateValidCommand(Guid shipmentId)
        {
            return new UploadLabel
            {
                ShipmentId = shipmentId,
                FileName = "label.pdf",
                ContentType = "application/pdf",
                FileStream = new MemoryStream(new byte[] { 1, 2, 3 }),
                FileSize = 3
            };
        }

        private Shipment CreateValidShipment(Guid id, ShipmentState state)
        {
            return new Shipment
            {
                Id = id,
                State = state,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ShipmentEvents =
                {
                    new ShipmentEvent
                    {
                        EventCode = "CREATED",
                        CorrelationId = Guid.NewGuid().ToString()
                    }
                }
            };
        }

        [Fact]
        public async Task Handle_InvalidValidation_ShouldReturnFailure()
        {
            // Arrange
            var command = new UploadLabel
            {
                ShipmentId = Guid.Empty,
                FileName = "",
                ContentType = "",
                FileSize = 0,
                FileStream = new MemoryStream()
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Invalid command parameters");
            result.Errors.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Handle_ShipmentNotFound_ShouldReturnFailure()
        {
            // arrange
            _unitOfWork.Shipments
                .GetShipmentByIdAsync(Arg.Any<Guid>())
                .ReturnsNull();

            var command = CreateValidCommand(Guid.NewGuid());

            // act
            var result = await _handler.Handle(command, CancellationToken.None);

            // assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Shipment not found.");
        }

        [Fact]
        public async Task Handle_ShipmentStateInvalid_ShouldReturnFailure()
        {
            // arrange
            var shipment = CreateValidShipment(Guid.NewGuid(), ShipmentState.LabelUploaded);

            _unitOfWork.Shipments
                .GetShipmentByIdAsync(Arg.Any<Guid>())
                .Returns(shipment);

            var command = CreateValidCommand(shipment.Id);

            // act
            var result = await _handler.Handle(command, CancellationToken.None);

            // assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Label can be uploaded only for Created shipments");
        }

        [Fact]
        public async Task Handle_WhenUploadFails_ShouldRollbackAndDeleteBlob()
        {
            // arrange
            var shipment = CreateValidShipment(Guid.NewGuid(), ShipmentState.Created);

            _unitOfWork.Shipments
                .GetShipmentByIdAsync(Arg.Any<Guid>())
                .Returns(shipment);

            _blobService
                .UploadAsync(Arg.Any<string>(),
                            Arg.Any<Stream>(),
                            Arg.Any<string>())
                .Throws(new Exception("Upload failed"));

            var command = CreateValidCommand(shipment.Id);

            // act
            Func<Task> act = async () =>
                await _handler.Handle(command, CancellationToken.None);

            // assert
            await act.Should().ThrowAsync<Exception>();

            await _transaction.Received(1)
                .RollbackAsync(Arg.Any<CancellationToken>());

            await _blobService.Received(1)
                .DeleteIfExistsAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task Handle_SaveChangesFails_ShouldRollbackAndThrow()
        {
            // Arrange
            var shipment = CreateValidShipment(Guid.NewGuid(), ShipmentState.Created);

            _unitOfWork.Shipments
                .GetShipmentByIdAsync(shipment.Id)
                .Returns(shipment);

            _unitOfWork
                .SaveChangesAsync(Arg.Any<CancellationToken>())
                .ThrowsAsync(new Exception("Db changes failed"));

            var command = CreateValidCommand(shipment.Id);

            // Act
            Func<Task> act = async () =>
                await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>();

            await _transaction.Received(1)
                .RollbackAsync(Arg.Any<CancellationToken>());

            await _blobService.Received(1)
                .DeleteIfExistsAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task Handle_PublishFails_ShouldRollbackAndDeleteBlob()
        {
            // Arrange
            var shipment = CreateValidShipment(Guid.NewGuid(), ShipmentState.Created);

            _unitOfWork.Shipments
                .GetShipmentByIdAsync(shipment.Id)
                .Returns(shipment);

            _unitOfWork
                .SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(1);

            _serviceBus
                .PublishAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>())
                .Throws(new Exception("Publish failed"));

            var command = CreateValidCommand(shipment.Id);

            // Act
            Func<Task> act = async () =>
                await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<Exception>();

            await _transaction.Received(1)
                .RollbackAsync(Arg.Any<CancellationToken>());

            await _blobService.Received(1)
                .DeleteIfExistsAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldUploadAndCommit()
        {
            // arrange
            var shipment = CreateValidShipment(Guid.NewGuid(), ShipmentState.Created);

            _unitOfWork.Shipments
                .GetShipmentByIdAsync(Arg.Any<Guid>())
                .Returns(shipment);

            _unitOfWork
                .SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(1);

            var command = CreateValidCommand(shipment.Id);

            // act
            var result = await _handler.Handle(command, CancellationToken.None);

            // assert
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Be("Label uploaded successfully.");

            await _blobService.Received(1)
                .UploadAsync(Arg.Any<string>(),
                            Arg.Any<Stream>(),
                            Arg.Any<string>());

            await _unitOfWork.Received(1)
                .SaveChangesAsync(Arg.Any<CancellationToken>());

            await _serviceBus.Received(1)
                .PublishAsync(Arg.Any<Guid>(),
                            Arg.Any<string>(),
                            Arg.Any<string>());

            await _transaction.Received(1)
                .CommitAsync(Arg.Any<CancellationToken>());
        }

    
    }
}