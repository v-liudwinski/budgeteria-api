using Budgeteria.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Budgeteria.Api.Data;

public class BudgeteriaDbContext(DbContextOptions<BudgeteriaDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<FamilyPlan> Plans => Set<FamilyPlan>();
    public DbSet<PlanCategory> Categories => Set<PlanCategory>();
    public DbSet<FinancialGoal> Goals => Set<FinancialGoal>();
    public DbSet<FamilyMember> Members => Set<FamilyMember>();
    public DbSet<Expense> Expenses => Set<Expense>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Email).HasMaxLength(256);
            e.Property(u => u.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<FamilyPlan>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasMany(p => p.Categories).WithOne(c => c.Plan).HasForeignKey(c => c.PlanId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(p => p.Goals).WithOne(g => g.Plan).HasForeignKey(g => g.PlanId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(p => p.Members).WithOne(m => m.Plan).HasForeignKey(m => m.PlanId).OnDelete(DeleteBehavior.Cascade);
            e.Property(p => p.MonthlyIncome).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<PlanCategory>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.MonthlyLimit).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<FinancialGoal>(e =>
        {
            e.HasKey(g => g.Id);
            e.Property(g => g.TargetAmount).HasColumnType("decimal(18,2)");
            e.Property(g => g.CurrentAmount).HasColumnType("decimal(18,2)");
            e.Property(g => g.MonthlyContribution).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<FamilyMember>(e =>
        {
            e.HasKey(m => m.Id);
            e.HasIndex(m => m.InviteToken).IsUnique().HasFilter("\"InviteToken\" IS NOT NULL");
        });

        modelBuilder.Entity<Expense>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            e.HasIndex(x => x.PlanId);
            e.HasIndex(x => x.CategoryId);
        });
    }
}
