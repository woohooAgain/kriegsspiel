using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;

namespace Kriegsspiel.Console.Commands;

public sealed class OrderCommand
{
    public Order? Build(GameState state) => new OrderBuilder().Build(state);
}
