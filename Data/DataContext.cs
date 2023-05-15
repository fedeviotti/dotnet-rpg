using dotnet_rpg.Models;
using Microsoft.EntityFrameworkCore;

namespace dotnet_rpg.Data;

public class DataContext : DbContext
{
    // expression body definition
    // read more https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/properties#expression-body-definitions
    public DbSet<Character> Characters => Set<Character>();
    
    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
        
    }
}