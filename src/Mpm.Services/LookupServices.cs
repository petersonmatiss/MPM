using Mpm.Data;
using Mpm.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mpm.Services;

public interface ISteelGradeService
{
    Task<IEnumerable<SteelGrade>> GetAllActiveAsync();
    Task<SteelGrade?> GetByIdAsync(int id);
    Task<SteelGrade> CreateAsync(SteelGrade steelGrade);
    Task<SteelGrade> UpdateAsync(SteelGrade steelGrade);
    Task DeleteAsync(int id);
}

public interface IProfileTypeService
{
    Task<IEnumerable<ProfileType>> GetAllActiveAsync();
    Task<ProfileType?> GetByIdAsync(int id);
    Task<ProfileType> CreateAsync(ProfileType profileType);
    Task<ProfileType> UpdateAsync(ProfileType profileType);
    Task DeleteAsync(int id);
}

public class SteelGradeService : ISteelGradeService
{
    private readonly MpmDbContext _context;

    public SteelGradeService(MpmDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<SteelGrade>> GetAllActiveAsync()
    {
        return await _context.SteelGrades
            .Where(sg => sg.IsActive)
            .OrderBy(sg => sg.Code)
            .ToListAsync();
    }

    public async Task<SteelGrade?> GetByIdAsync(int id)
    {
        return await _context.SteelGrades.FindAsync(id);
    }

    public async Task<SteelGrade> CreateAsync(SteelGrade steelGrade)
    {
        if (string.IsNullOrEmpty(steelGrade.Code))
        {
            throw new InvalidOperationException("Steel grade code is required.");
        }

        if (string.IsNullOrEmpty(steelGrade.Name))
        {
            throw new InvalidOperationException("Steel grade name is required.");
        }

        // Check for duplicate code
        var existing = await _context.SteelGrades
            .FirstOrDefaultAsync(sg => sg.Code == steelGrade.Code);
        
        if (existing != null)
        {
            throw new InvalidOperationException($"A steel grade with code '{steelGrade.Code}' already exists.");
        }

        _context.SteelGrades.Add(steelGrade);
        await _context.SaveChangesAsync();
        return steelGrade;
    }

    public async Task<SteelGrade> UpdateAsync(SteelGrade steelGrade)
    {
        if (string.IsNullOrEmpty(steelGrade.Code))
        {
            throw new InvalidOperationException("Steel grade code is required.");
        }

        if (string.IsNullOrEmpty(steelGrade.Name))
        {
            throw new InvalidOperationException("Steel grade name is required.");
        }

        // Check for duplicate code excluding current steel grade
        var existing = await _context.SteelGrades
            .FirstOrDefaultAsync(sg => sg.Code == steelGrade.Code && sg.Id != steelGrade.Id);
        
        if (existing != null)
        {
            throw new InvalidOperationException($"A steel grade with code '{steelGrade.Code}' already exists.");
        }

        _context.SteelGrades.Update(steelGrade);
        await _context.SaveChangesAsync();
        return steelGrade;
    }

    public async Task DeleteAsync(int id)
    {
        var steelGrade = await _context.SteelGrades.FindAsync(id);
        if (steelGrade != null)
        {
            // Check if steel grade is being used
            var hasProfiles = await _context.Profiles
                .AnyAsync(p => p.SteelGradeId == id);
            
            if (hasProfiles)
            {
                throw new InvalidOperationException("Cannot delete steel grade that is being used by profiles.");
            }

            steelGrade.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
    }
}

public class ProfileTypeService : IProfileTypeService
{
    private readonly MpmDbContext _context;

    public ProfileTypeService(MpmDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ProfileType>> GetAllActiveAsync()
    {
        return await _context.ProfileTypes
            .Where(pt => pt.IsActive)
            .OrderBy(pt => pt.Code)
            .ToListAsync();
    }

    public async Task<ProfileType?> GetByIdAsync(int id)
    {
        return await _context.ProfileTypes.FindAsync(id);
    }

    public async Task<ProfileType> CreateAsync(ProfileType profileType)
    {
        if (string.IsNullOrEmpty(profileType.Code))
        {
            throw new InvalidOperationException("Profile type code is required.");
        }

        if (string.IsNullOrEmpty(profileType.Name))
        {
            throw new InvalidOperationException("Profile type name is required.");
        }

        // Check for duplicate code
        var existing = await _context.ProfileTypes
            .FirstOrDefaultAsync(pt => pt.Code == profileType.Code);
        
        if (existing != null)
        {
            throw new InvalidOperationException($"A profile type with code '{profileType.Code}' already exists.");
        }

        _context.ProfileTypes.Add(profileType);
        await _context.SaveChangesAsync();
        return profileType;
    }

    public async Task<ProfileType> UpdateAsync(ProfileType profileType)
    {
        if (string.IsNullOrEmpty(profileType.Code))
        {
            throw new InvalidOperationException("Profile type code is required.");
        }

        if (string.IsNullOrEmpty(profileType.Name))
        {
            throw new InvalidOperationException("Profile type name is required.");
        }

        // Check for duplicate code excluding current profile type
        var existing = await _context.ProfileTypes
            .FirstOrDefaultAsync(pt => pt.Code == profileType.Code && pt.Id != profileType.Id);
        
        if (existing != null)
        {
            throw new InvalidOperationException($"A profile type with code '{profileType.Code}' already exists.");
        }

        _context.ProfileTypes.Update(profileType);
        await _context.SaveChangesAsync();
        return profileType;
    }

    public async Task DeleteAsync(int id)
    {
        var profileType = await _context.ProfileTypes.FindAsync(id);
        if (profileType != null)
        {
            // Check if profile type is being used
            var hasProfiles = await _context.Profiles
                .AnyAsync(p => p.ProfileTypeId == id);
            
            if (hasProfiles)
            {
                throw new InvalidOperationException("Cannot delete profile type that is being used by profiles.");
            }

            profileType.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
    }
}