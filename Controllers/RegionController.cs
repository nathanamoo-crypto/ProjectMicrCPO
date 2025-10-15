using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MicrDbChequeProcessingSystem.Data;

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
        var regions = await _context.RegionZones
            .Include(r => r.CreatedByUser)
            .Include(r => r.Banks)
                .ThenInclude(b => b.BankBranches)
            .AsNoTracking()
            .OrderBy(r => r.RegionName)
            .ToListAsync();

        return View(regions);
    }
}
