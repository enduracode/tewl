using JetBrains.Annotations;

namespace Tewl.Tools;

/// <summary>
/// Extension methods and other static methods pertaining to things of type object.
/// </summary>
[ PublicAPI ]
public static class ObjectTools {
	/// <summary>
	/// Returns o.ToString() unless o is null. In this case, returns either null (if nullToEmptyString is false) or the empty
	/// string (if nullToEmptyString is true).
	/// </summary>
	public static string? ObjectToString( this object? o, bool nullToEmptyString ) => o is not null ? o.ToString() : nullToEmptyString ? string.Empty : null;
}