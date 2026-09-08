using EventHub.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventHub.Infrastructure.Persistence.Repositories
{
    public class UnitOfWork:IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            
            return await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
