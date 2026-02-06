using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IShipmentRepository? _shipmentRepository;
        private IShipmentEventRepository? _shipmentEventRepository;
        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }
        public IShipmentRepository Shipments => _shipmentRepository ??= new ShipmentRepository(_context);

        public IShipmentEventRepository ShipmentEvents => _shipmentEventRepository ??= new ShipmentEventRepository(_context);

        public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
        {
            return await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
    }
}