﻿
using Ardalis.GuardClauses;
using Base.Integration.Tests;
using Domain.Dto;
using Domain.Libraries;

namespace Persistence.Integration.Tests;


public class UnitOfWorkTests : BaseIntegrationTest
{

    public UnitOfWorkTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Savechanges_Create()
    {
        // Arrange
        var libName = "Create_" + Guid.NewGuid().ToString();
        var lib = LibraryDto.Create(Library.Create(libName));        
        Context.Libraries.Add(lib);

        // Act
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Assert
        var list = Context.Libraries.Where(l => l.Name == libName).ToList();
        list.Should().HaveCount(1);
        var savedLib = list.First();
        Guard.Against.Null(savedLib);
        savedLib.CreatedOnUtc.Should().NotBe(null);
    }

    [Fact]
    public async Task Savechanges_Modify()
    {
        // Arrange
        var libName = "Modify_" + Guid.NewGuid().ToString();
        var lib = LibraryDto.Create(Library.Create(libName));
        lib.CreatedOnUtc.Should().NotBe(null);
        Context.Libraries.Add(lib);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);
        libName += "_modified";
        lib.Update(libName);
        Context.Libraries.Update(lib);

        // Act
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Assert
        var list = Context.Libraries.Where(l => l.Name == libName).ToList();
        list.Should().HaveCount(1);
        var savedLib = list.First();        
        Guard.Against.Null(savedLib);
        savedLib.CreatedOnUtc.Should().NotBe(null);
        savedLib.ModifiedOnUtc.Should().NotBe(null);
        Guard.Against.Null(savedLib.ModifiedOnUtc);
        savedLib.CreatedOnUtc.Should().BeBefore(savedLib.ModifiedOnUtc.Value);
    }


}