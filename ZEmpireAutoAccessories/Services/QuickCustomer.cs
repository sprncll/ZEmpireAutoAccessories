using Microsoft.EntityFrameworkCore;
using ZEmpireAutoAccessories.Data;
using ZEmpireAutoAccessories.Models;

namespace ZEmpireAutoAccessories.Services
{
    /// <summary>
    /// Adding a customer without leaving the screen that needs one.
    ///
    /// A walk-in buying a bottle of wax, or being quoted for tint, is still a
    /// customer - and sending the person at the counter off to the Customers
    /// module to make one loses whatever they had half-typed. The panel that
    /// does it sits on the sale and on the quotation, and this is the part
    /// they share: the same fields, the same refusals, the same record.
    ///
    /// It only ever ADDS. A number already on file is refused and its holder
    /// named rather than quietly resolved to them - picking a customer the
    /// typist did not choose is how a sale ends up against the wrong person,
    /// and the holder's name is what tells them who to pick instead.
    ///
    /// Not a controller action of its own, because the permission differs by
    /// where it is used: making a customer is part of making a sale, so the
    /// Sales module's own permission is the right one to ask for there, and
    /// the Quotations module's is the right one on a quotation. Each
    /// controller keeps its own action; this keeps them from disagreeing
    /// about what a customer is.
    /// </summary>
    public static class QuickCustomer
    {
        /// <summary>
        /// What the panel gets back: a customer, or a reason it is not one.
        /// </summary>
        public sealed record Result(int? Id, string? Name, string? Error)
        {
            public static Result Refused(string why) => new(null, null, why);
            public static Result Added(Customer customer) =>
                new(customer.CustomerID, customer.FullName, null);
        }

        public static async Task<Result> Add(
            ApplicationDbContext context, string? lastName, string? firstName, string? contactNumber)
        {
            // Two boxes, one column - the same split the Customers module
            // uses, so a walk-in added here is not a different shape of record
            // from one added there.
            if (string.IsNullOrWhiteSpace(firstName))
                return Result.Refused("Enter the customer's first name.");

            if (string.IsNullOrWhiteSpace(lastName))
                return Result.Refused("Enter the customer's last name.");

            var number = PhoneNumber.ToPhilippineMobile(contactNumber);
            if (number == null)
                return Result.Refused(
                    "Enter a Philippine mobile number: +63 then 10 digits starting with 9.");

            // Both shapes are looked for: a row entered before the shop moved
            // to +63 still holds 09121231231, which is the same number.
            var dialled = PhoneNumber.ToDialledForm(number);

            var holder = await context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ContactNumber == number || c.ContactNumber == dialled);

            if (holder != null)
                return Result.Refused(
                    $"That number is already on file for \"{holder.FullName}\". "
                    + "Close this and pick them from the Customer list.");

            var customer = new Customer
            {
                FullName = PersonName.Combine(firstName, lastName),
                ContactNumber = number,
                CreatedAt = DateTime.UtcNow
            };

            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            return Result.Added(customer);
        }
    }
}
