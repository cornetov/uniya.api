using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

using Uniya.Core;
using Uniya.Connectors.Sqlite;
using Uniya.Web.Models;

namespace Uniya.Web.Controllers;

/// <summary>CRUD data.</summary>
[ApiController]
[Route("api/data")]
[Produces("application/json")]
public class DataController : ControllerBase
{
    //public static IUser user = XProxy.Get<IUser>();
    //private readonly IUserService _userService;
    private XProvider _provider;

    private async Task<XProvider> GetProvider()
    {
        // TODO: select local database using settings
        _provider ??= await XProvider.LocalDatabase(new SqliteLocal());
        return _provider;
    }

    /// <summary></summary>
    /// <param name="mask"></param>
    /// <returns></returns>
    [HttpGet()]
    public async Task<IReadOnlyList<string>> Get([FromQuery] string mask)
    {
        var privider = await GetProvider();
        if (string.IsNullOrEmpty(mask))
        {
            return await privider.GetDatabases();
        }
        return await privider.GetDatabases(mask);
    }
    /// <summary></summary>
    /// <param name="database"></param>
    /// <param name="mask"></param>
    /// <returns></returns>
    [HttpGet("{database}")]
    public async Task<IReadOnlyList<string>> Get(string database, [FromQuery] string mask)
    {
        var privider = await GetProvider();
        //var iq = HttpContext.Request.Query["mask"];
        //if (iq.Count == 0)
        //{
        //    return await privider.GetTables(database);
        //}
        //return await privider.GetTables(database, iq.ToString());
        if (string.IsNullOrEmpty(mask))
        {
            return await privider.GetTables(database);
        }
        return await privider.GetTables(database, mask);
    }

    /*
    [HttpGet("{database}/{table}")]
    public async Task Get(string database, string table)
    {
        return await _context.Products.ToListAsync();
    }

    [HttpGet("{database}/{table}/{id}")]
    public async Task Get(string database, string table, int id)
    {
        var product = await _context.Products.FirstOrDefaultAsync(m => m.Id == id);
        if (product == null)
            return NotFound();
        return Ok(product);

    }

    [HttpPost("{database}/{table}")]
    public async Task Post(string database, string table, XEntity entity)
    {
        _context.Add(product);
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpPut("{database}/{table}")]
    public async Task Put(string database, string table, XEntity entity)
    {
        if (productData == null || productData.Id == 0)
            return BadRequest();

        var product = await _context.Products.FindAsync(productData.Id);
        if (product == null)
            return NotFound();
        product.Name = productData.Name;
        product.Description = productData.Description;
        product.Price = productData.Price;
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("{database}/{table}/{id}")]
    public async Task Delete(string database, string table, int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();
        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return Ok();
    }
    */
}