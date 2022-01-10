using Microsoft.Extensions.Logging;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Wrapper.UserRegistry;

namespace Telegram.Bot.Wrapper.TelegramBot;

public class TelegramBotWrapper: ITelegramBotWrapper
{
    private MenuItem _mainMenu = new ("MainMenu", null);
    private readonly ILogger<TelegramBotWrapper> _logger;
    private readonly IUserStateManager _userState;
    private readonly ITelegramBotClient _botClient;
    // private readonly IStringLocalizer<TelegramBotWrapper> _localizer;
    
    public TelegramBotWrapper(ITelegramBotClient botClient,
        IUserStateManager userState, 
        ILogger<TelegramBotWrapper> logger)
    {
        _botClient = botClient;
        _userState = userState;
        _logger = logger;
    }
    
    public void SetupMainMenu(MenuItem mainMenu)
    {
        _mainMenu = new MenuItem(mainMenu.Name, null)
        {
            UploadHandlerCallback = mainMenu.UploadHandlerCallback
        };
        _mainMenu.Children.AddRange(mainMenu.Children);
    }

    public async Task<bool> DrawMenu(string? text, UserProfile user)
    {
        await Task.CompletedTask;
        if (!_mainMenu.Children.Any())
            return false;
        if (text == null)
            // TODO обработать пустой текст
            return false;
        
        // Получаем актуальное меню, в котором сейчас находимся
        var actualMenuName = _userState.GetActualMenuName(user.Id);
        MenuItem? currentMenu = null;
        currentMenu = actualMenuName == "" ? _mainMenu : _mainMenu.FindMenu(text);
        
        if (currentMenu == null) 
        {
             _logger.LogError($"Failed to find menu with name {actualMenuName}");
             return false;
        }
        
        var subItem = currentMenu.FindSubMenu(text);
        if (subItem != null)
        {
            // if subItem.Params.IsMenuUpButton {
            //    currentMenu = subItem.Parent.Parent
            currentMenu = subItem;
        }
        else
        {
            _logger.LogError($"Failed to find menu with name {actualMenuName}");
            return false;
        }

        if (currentMenu.UploadHandlerCallback != null)
        {
            currentMenu.UploadHandlerCallback(currentMenu, user);
        }
        
        return true;
    }
    
    public Task DrawMainMenu(UserProfile user)
    {
        _userState.SetActualMenuName(user.Id, "");
        _mainMenu.UploadHandlerCallback?.Invoke(_mainMenu, user);
        return Task.CompletedTask;
    }

    private ReplyKeyboardMarkup RenderMenuItem(MenuItem menu)
    {
        var buttons = new List<KeyboardButton>();
        foreach (var child in menu.Children)
        {
            buttons.Add(new KeyboardButton(child.Name)
            {
                RequestContact = false,
            });
        }
        var res = new ReplyKeyboardMarkup(buttons.ToArray())
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };
        return res;
    }

    public async Task Send(UserProfile user, string text, MenuItem? menu = null)
    {
        if (menu != null)
        {
            var renderedMenu = RenderMenuItem(menu);
            await _botClient.SendTextMessageAsync(user.Id, text, ParseMode.Html,
                replyMarkup: renderedMenu);
        }
        else
        {
            await _botClient.SendTextMessageAsync(user.Id, text, ParseMode.Html);
        }
    }
}