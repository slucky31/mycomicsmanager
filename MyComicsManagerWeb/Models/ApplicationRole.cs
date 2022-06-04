using System;
using AspNetCore.Identity.MongoDbCore.Models;
using MongoDbGenericRepository.Attributes;

namespace MyComicsManagerWeb.Models;

[CollectionName("Roles")]
public class ApplicationRole : MongoIdentityRole<Guid>
{
}