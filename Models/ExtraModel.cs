using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OlymPOS.Models;

public class Extra
{
    public int ExtraId { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public bool IsSelected { get; set; }
    public int OrderIDSub { get; set; }
    public int quantity { get; set; }
}

public class Option
{
    public long OptionId { get; set; }
    public string Description { get; set; }
    public bool IsSelected { get; set; }
}
public class Course
{
    public long CourseId { get; set; }
    public string Description { get; set; }
    public bool IsSelected { get; set; }

}