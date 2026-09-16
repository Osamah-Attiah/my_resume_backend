using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Resume.Infrastructure;

public sealed class ResumeDbContextFactory : IDesignTimeDbContextFactory<ResumeDbContext>
{
    public ResumeDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=54324;Database=my_resume;Username=resume_local;Password=resume_local_password";
        return new ResumeDbContext(new DbContextOptionsBuilder<ResumeDbContext>().UseNpgsql(connection).Options);
    }
}
