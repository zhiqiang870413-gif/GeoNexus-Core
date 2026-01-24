using GisProject.Models;

namespace GisProject.Interfaces
{
    public interface IVendorService
    {
        // 取得所有或過濾後的設施
        IEnumerable<Vendor> GetVendors(double? minLat, double? maxLat, double? minLng, double? maxLng);
        
        // 取得特定 ID 設施
        Vendor GetById(int id);
        
        // 新增設施
        void AddVendor(Vendor vendor);
        
        // 更新狀態
        bool UpdateStatus(int id, string status);
        
        // 刪除設施
        bool DeleteVendor(int id);
    }
}