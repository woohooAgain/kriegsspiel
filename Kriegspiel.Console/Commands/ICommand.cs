namespace Kriegsspiel.Console.Commands;

using Kriegsspiel.Core.Domain;
public interface ICommand { void Execute(GameState state); }