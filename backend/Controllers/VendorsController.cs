using GisProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GisProject.Interfaces;
using GisProject.Services;
using Microsoft.AspNetCore.Authorization;

namespace GisProject.Controllers
{
    [ApiController]
    [Route("api/vendors")]
    public class VendorsController : ControllerBase
    {
        private readonly IVendorService _vendorService;

        public VendorsController(IVendorService vendorService)
        {
            _vendorService = vendorService;
        }

        [HttpGet]
        public IActionResult Get([FromQuery] double? minLat, [FromQuery] double? maxLat, [FromQuery] double? minLng, [FromQuery] double? maxLng)
        {
            return Ok(_vendorService.GetVendors(minLat, maxLat, minLng, maxLng));
        }

        [Authorize]
        [HttpPost]
        public IActionResult Post([FromBody] Vendor newVendor)
        {
            if (newVendor == null) return BadRequest();
            _vendorService.AddVendor(newVendor);
            return CreatedAtAction(nameof(Get), new { id = newVendor.Id }, newVendor);
        }

        [Authorize]
        [HttpPatch("{id}")]
        public IActionResult Patch(int id, [FromBody] StatusUpdate model)
        {
            var success = _vendorService.UpdateStatus(id, model.Status);
            return success ? Ok() : NotFound();
        }

        [Authorize]
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var success = _vendorService.DeleteVendor(id);
            return success ? NoContent() : NotFound();
        }
    }
    // 輔助用的資料模型
    public class StatusUpdate { public string Status { get; set; } }
}