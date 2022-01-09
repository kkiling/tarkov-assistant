namespace Tarkov.Assistant.Telegram.Bot.UserRegistry;

public class UserStateManager: IUserStateManager
{
    private string _menuName = "";
    public void SetActualMenuName(long userId, string menuName)
    {
        _menuName = menuName;
    }

    public string GetActualMenuName(long userId)
    {
        return _menuName;
    }
}