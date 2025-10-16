using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MicrDbChequeProcessingSystem.Data;
using MicrDbChequeProcessingSystem.Models;
using MicrDbChequeProcessingSystem.ViewModels;

namespace MicrDbChequeProcessingSystem.Controllers;

public class RegionController : Controller
{
    private readonly MicrDbContext _context;

    public RegionController(MicrDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var systemRegions = (await _context.RegionZones
                .Include(r => r.Banks)
                    .ThenInclude(b => b.BankBranches)
                .AsNoTracking()
                .OrderBy(r => r.RegionName)
                .ToListAsync())
            .Select(r => new RegionListItem
            {
                Name = r.RegionName,
                Source = "Core",
                Description = null,
                Created = (r.CreatedDate ?? DateTime.MinValue).ToString("dd MMM yyyy"),
                Banks = r.Banks.Count,
                Branches = r.Banks.Sum(b => b.BankBranches.Count)
            })
            .ToList();

        List<RegionListItem> customRegions = new();
        try
        {
            customRegions = await _context.RegionCustoms
                .AsNoTracking()
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new RegionListItem
                {
                    Name = r.RegionName,
                    Source = "Custom",
                    Description = r.Description,
                    Created = r.CreatedAt.ToLocalTime().ToString("dd MMM yyyy HH:mm"),
                    Banks = 0,
                    Branches = 0
                })
                .ToListAsync();
        }
        catch (Exception ex) when (IsMissingCustomTable(ex))
        {
            customRegions = new List<RegionListItem>();
        }

        var viewModel = new RegionIndexViewModel
        {
            Items = systemRegions
                .Concat(customRegions)
                .OrderByDescending(r => r.Source == "Custom")
                .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RegionCreateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Please provide the required details." });
        }

        var entry = new RegionCustom
        {
            RegionName = request.RegionName.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            _context.RegionCustoms.Add(entry);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsMissingCustomTable(ex))
        {
            return StatusCode(501, new { success = false, message = "Custom regions are not enabled in this database." });
        }
        catch (DbUpdateException)
        {
            return StatusCode(500, new { success = false, message = "We couldn't save the record. Please try again." });
        }

        return Json(new
        {
            success = true,
            data = new
            {
                regionName = entry.RegionName,
                description = entry.Description,
                source = "Custom",
                created = entry.CreatedAt.ToLocalTime().ToString("dd MMM yyyy HH:mm"),
                banks = 0,
                branches = 0
            }
        });
    }
    private static bool IsMissingCustomTable(Exception? exception)
    {
        while (exception is not null)
        {
            if (exception is SqlException sqlEx)
            {
                foreach (SqlError error in sqlEx.Errors)
                {
                    if (error.Number == 208)
                    {
                        return true;
                    }
                }
            }

            exception = exception.InnerException;
        }

        return false;
    }
}
