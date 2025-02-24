using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OlymPOS;


public class UserSettings
{
    private static readonly Lazy<UserSettings> instance = new(() => new UserSettings());

    public static UserSettings Instance => instance.Value;

    public static string Username { get; set; }
    public static string Role { get; set; }
    public static string CashB { get; set; }
    public static string Lang { get; set; }
    public static string PrintLang { get; set; }
    public static int ClerkID { get; set; }
    public static int Price_cat { get; set; }
    public static int Defypd { get; set; }
    public static bool IsAdmin { get; set; }
    public static bool Discount { get; set; }
    public static bool Freedrinks { get; set; }
    public static bool Stats { get; set; }
   

    // Add other user settings properties here

    private UserSettings()
    {
    }
}
