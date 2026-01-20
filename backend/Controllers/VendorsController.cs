using GisProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GisProject.Controllers
{
    [ApiController]
    [Route("api/vendors")]
    public class VendorsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VendorsController(AppDbContext context)
        {
            _context = context;
            // 關鍵：確保資料庫檔案與資料表已建立 (若不存在會自動產生)
            _context.Database.EnsureCreated();
        }

        // [GET] 取得所有資料
        [HttpGet]
        public IActionResult Get([FromQuery] double? minLat, [FromQuery] double? maxLat, [FromQuery] double? minLng, [FromQuery] double? maxLng)
        {
            var query = _context.Vendors.AsQueryable();

            // 只有在四個參數都有傳的情況下才過濾
            if (minLat.HasValue && maxLat.HasValue && minLng.HasValue && maxLng.HasValue)
            {
                query = query.Where(v =>
                    v.Lat >= minLat && v.Lat <= maxLat &&
                    v.Lng >= minLng && v.Lng <= maxLng);
            }
            // 如果沒傳參數（例如剛進網頁時），就會跳過 if，直接回傳所有的 List

            return Ok(query.ToList());
        }

        [HttpPost]
        public IActionResult Post([FromBody] Vendor newVendor)
        {
            if (newVendor == null)
            {
                return BadRequest("資料不可為空");
            }

            // 將資料加入資料庫上下文
            _context.Vendors.Add(newVendor);

            // 儲存變更到 gisdata.db
            _context.SaveChanges();

            // 回傳 201 Created，並附上新生成的 ID
            return CreatedAtAction(nameof(Get), new { id = newVendor.Id }, newVendor);
        }

        // [PATCH] 修改狀態
        [HttpPatch("{id}")]
        public IActionResult Patch(int id, [FromBody] StatusUpdate model)
        {
            var vendor = _context.Vendors.Find(id);
            if (vendor == null) return NotFound();

            vendor.Status = model.Status;
            _context.SaveChanges();
            return Ok(vendor);
        }

        // [DELETE] 刪除設施
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var vendor = _context.Vendors.Find(id);
            if (vendor == null) return NotFound();

            _context.Vendors.Remove(vendor);
            _context.SaveChanges();
            return NoContent();
        }

        [HttpGet("bounds")]
        public IActionResult GetByBounds([FromQuery] double minLat, [FromQuery] double maxLat, [FromQuery] double minLng, [FromQuery] double maxLng)
        {
            // 只撈取畫面上看得到的點，這在資料量大時極快
            var visibleVendors = _context.Vendors
                .Where(v => v.Lat >= minLat && v.Lat <= maxLat && v.Lng >= minLng && v.Lng <= maxLng)
                .ToList();
            return Ok(visibleVendors);
        }

    }
    // 輔助用的資料模型
    public class StatusUpdate { public string Status { get; set; } }
}