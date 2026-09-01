namespace System.Runtime.CompilerServices;

/// <summary>
/// Compiler-required marker that enables init-only setters on frameworks (such as net48) that do not ship
/// the type. The compiler looks for this type by name; an internal definition satisfies it.
/// </summary>
internal static class IsExternalInit { }
