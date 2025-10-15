using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MicrDbChequeProcessingSystem.Data;

namespace MicrDbChequeProcessingSystem.Controllers;

public class AccountTypeController : Controller
{
    private readonly MicrDbContext _context;

    public AccountTypeController(MicrDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var accountTypes = await _context.AccountTypes
            .Include(a => a.CreatedByUser)
            .AsNoTracking()
            .OrderBy(a => a.AccountTypeName)
            .ToListAsync();

        return View(accountTypes);
    }
}
