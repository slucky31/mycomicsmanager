using System.ComponentModel.DataAnnotations;

namespace MyComicsManagerWeb.Models;

public class Role
{
    [Required]
    public string Name { get; set; }
}