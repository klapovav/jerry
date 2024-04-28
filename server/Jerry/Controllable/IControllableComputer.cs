using Jerry.Coordinates;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;

namespace Jerry.Controllable;

/// <summary>
/// Interface for computers capable of receiving user input.
/// </summary>
public interface IControllableComputer : IControllable, IComputer
{

}