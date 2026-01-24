using GisProject.Interfaces;
using GisProject.Models;
using System.Collections.Generic;
using System.Linq;

namespace GisProject.Services
{
    public class VendorService : IVendorService
    {
        private readonly AppDbContext _context;

        public VendorService(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Vendor> GetVendors(double? minLat, double? maxLat, double? minLng, double? maxLng)
        {
            var query = _context.Vendors.AsQueryable();

            if (minLat.HasValue && maxLat.HasValue && minLng.HasValue && maxLng.HasValue)
            {
                query = query.Where(v =>
                    v.Lat >= minLat && v.Lat <= maxLat &&
                    v.Lng >= minLng && v.Lng <= maxLng);
            }

            return query.ToList();
        }

        public Vendor GetById(int id) => _context.Vendors.Find(id);

        public void AddVendor(Vendor vendor)
        {
            _context.Vendors.Add(vendor);
            _context.SaveChanges();
        }

        public bool UpdateStatus(int id, string status)
        {
            var vendor = _context.Vendors.Find(id);
            if (vendor == null) return false;

            vendor.Status = status;
            _context.SaveChanges();
            return true;
        }

        public bool DeleteVendor(int id)
        {
            var vendor = _context.Vendors.Find(id);
            if (vendor == null) return false;

            _context.Vendors.Remove(vendor);
            _context.SaveChanges();
            return true;
        }
    }
}