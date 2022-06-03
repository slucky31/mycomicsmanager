using System;
using AspNetCore.Identity.MongoDbCore.Models;
using MongoDbGenericRepository.Attributes;
namespace MyComicsManagerWeb.Models;

[CollectionName("Users")]
public class ApplicationUser : MongoIdentityUser<Guid>
{
}