using Microsoft.EntityFrameworkCore;

namespace Sahno.Infrastructure.Persistence;

public sealed class SahnoDbContext(DbContextOptions<SahnoDbContext> options)
    : DbContext(options);
