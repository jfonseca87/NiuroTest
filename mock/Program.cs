using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Almacenamiento en memoria (mock)
var customers = new Dictionary<string, CustomerData>(StringComparer.OrdinalIgnoreCase);

app.MapGet("/", () => "Niuro Mock External Service");

// POST /api/customers - Crear customer
app.MapPost("/api/customers", (CustomerPayload payload) =>
{
    if (string.IsNullOrWhiteSpace(payload.Customer?.Ssn))
    {
        return Results.BadRequest(new { error = "SSN is required" });
    }

    var key = payload.Customer.Ssn;
    if (customers.ContainsKey(key))
    {
        return Results.Conflict(new { error = "Customer already exists" });
    }

    customers[key] = new CustomerData
    {
        Ssn = payload.Customer.Ssn,
        FirstName = payload.Customer.FirstName,
        LastName = payload.Customer.LastName,
        Address = payload.Customer.Address,
        CompanyName = payload.Customer.CompanyName,
        Application = payload.Application
    };

    Console.WriteLine($"[Mock] Created customer: {key}");
    return Results.Ok(new { message = "Customer created", ssn = key });
});

// PUT /api/customers/{ssn} - Actualizar customer
app.MapPut("/api/customers/{ssn}", (string ssn, CustomerPayload payload) =>
{
    if (!customers.TryGetValue(ssn, out var existing))
    {
        // Si no existe, lo creamos (comportamiento de "upsert" simple)
        customers[ssn] = new CustomerData
        {
            Ssn = ssn,
            FirstName = payload.Customer?.FirstName ?? "",
            LastName = payload.Customer?.LastName ?? "",
            Address = payload.Customer?.Address ?? new AddressData(),
            CompanyName = payload.Customer?.CompanyName ?? "",
            Application = payload.Application
        };
        Console.WriteLine($"[Mock] Upserted customer (PUT): {ssn}");
        return Results.Ok(new { message = "Customer upserted", ssn });
    }

    // Actualizar campos existentes
    existing.FirstName = payload.Customer?.FirstName ?? existing.FirstName;
    existing.LastName = payload.Customer?.LastName ?? existing.LastName;
    existing.Address = payload.Customer?.Address ?? existing.Address;
    existing.CompanyName = payload.Customer?.CompanyName ?? existing.CompanyName;
    existing.Application = payload.Application;

    Console.WriteLine($"[Mock] Updated customer: {ssn}");
    return Results.Ok(new { message = "Customer updated", ssn });
});

// GET /api/customers - Para debug/demo (UC-15)
app.MapGet("/api/customers", () => customers.Values);

app.Run();

// Types para el mock
public class CustomerPayload
{
    public CustomerData? Customer { get; set; }
    public ApplicationData? Application { get; set; }
}

public class CustomerData
{
    public string Ssn { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public AddressData Address { get; set; } = new();
    public string CompanyName { get; set; } = "";
    public ApplicationData? Application { get; set; }
}

public class AddressData
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string ZipCode { get; set; } = "";
}

public class ApplicationData
{
    public string? Id { get; set; }
    public decimal RequestedAmount { get; set; }
}
