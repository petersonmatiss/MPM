using Mpm.Data;
using Mpm.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mpm.Services;

public interface IProjectService
{
    Task<IEnumerable<Project>> GetAllAsync();
    Task<Project?> GetByIdAsync(int id);
    Task<Project> CreateAsync(Project project);
    Task<Project> UpdateAsync(Project project);
    Task DeleteAsync(int id);
    Task<IEnumerable<Project>> GetByCustomerAsync(int customerId);
}

public class ProjectService : IProjectService
{
    private readonly MpmDbContext _context;

    public ProjectService(MpmDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Project>> GetAllAsync()
    {
        return await _context.Projects
            .Include(p => p.Customer)
            .OrderBy(p => p.Code)
            .ToListAsync();
    }

    public async Task<Project?> GetByIdAsync(int id)
    {
        return await _context.Projects
            .Include(p => p.Customer)
            .Include(p => p.BillOfMaterials)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Project> CreateAsync(Project project)
    {
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        return project;
    }

    public async Task<Project> UpdateAsync(Project project)
    {
        _context.Projects.Update(project);
        await _context.SaveChangesAsync();
        return project;
    }

    public async Task DeleteAsync(int id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project != null)
        {
            project.Status = Domain.ProjectStatus.Cancelled;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Project>> GetByCustomerAsync(int customerId)
    {
        return await _context.Projects
            .Where(p => p.CustomerId == customerId)
            .Include(p => p.Customer)
            .OrderBy(p => p.Code)
            .ToListAsync();
    }
}