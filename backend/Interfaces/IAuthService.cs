namespace GisProject.Interfaces
{
    public interface IAuthService
    {
        // 傳入帳密，驗證成功回傳 Token 字串，失敗回傳 null
        string Login(string username, string password);
    }
}