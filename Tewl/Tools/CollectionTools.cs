using JetBrains.Annotations;

namespace Tewl.Tools;

/// <summary>
/// Tools for collections.
/// </summary>
[ PublicAPI ]
public static class CollectionTools {
	/// <summary>
	/// Sorts the list alphabetically (ascending) based on the ToString value of each element.
	/// </summary>
	public static void SortAlphabetically<T>( this List<T> list ) => list.Sort( ( one, two ) => one.ToString().CompareTo( two.ToString() ) );

	/// <summary>
	/// Adds default(T) to the given list until the desired length is reached.
	/// </summary>
	public static List<T> Pad<T>( this IEnumerable<T> enumeration, int length ) => enumeration.Pad( length, () => default );

	/// <summary>
	/// Adds the given placeholder item to the given list until the desired length is reached.
	/// </summary>
	public static List<T> Pad<T>( this IEnumerable<T> enumeration, int length, Func<T> getNewPlaceholderItem ) {
		var list = new List<T>( enumeration );
		while( list.Count < length )
			list.Add( getNewPlaceholderItem() );
		return list;
	}

	/// <summary>
	/// Transforms an IEnumerable into an IEnumerable of Tuple of two items while maintaining the order of the IEnumerable.
	/// </summary>
	public static IEnumerable<Tuple<A, B>> ToTupleEnumeration<A, B, T>( this IEnumerable<T> enumerable, Func<T, A> item1Selector, Func<T, B> item2Selector ) =>
		enumerable.Select( e => Tuple.Create( item1Selector( e ), item2Selector( e ) ) );

	/// <summary>
	/// Gets the values that appear more than once in this sequence.
	/// </summary>
	public static IEnumerable<T> GetDuplicates<T>( this IEnumerable<T> items ) => items.GroupBy( i => i ).Where( i => i.Count() > 1 ).Select( i => i.Key );

	/// <summary>
	/// Convenience method to allow concatenating individual elements.
	/// </summary>
	public static IEnumerable<T> ConcatItems<T>( this IEnumerable<T> items, params T[] ts ) => items.Concat( ts );

	/// <summary>
	/// Convenience method to allow concatenating individual elements to an existing array.
	/// </summary>
	public static T[] ConcatArray<T>( this IEnumerable<T> items, params T[] ts ) => items.Concat( ts ).ToArray();

	/// <summary>
	/// Creates a collection from this sequence.
	/// </summary>
	public static IReadOnlyCollection<T> Materialize<T>( this IEnumerable<T> items ) => items.ToArray();

	/// <summary>
	/// Creates a list from this sequence, enabling elements to be accessed by index.
	/// </summary>
	public static IReadOnlyList<T> MaterializeAsList<T>( this IEnumerable<T> items ) => items.ToList();

	/// <summary>
	/// Creates a collection containing only this item.
	/// </summary>
	public static IReadOnlyCollection<T> ToCollection<T>( this T item ) => [ item ];

	/// <summary>
	/// Returns an enumerable of functions that return the given items.
	/// </summary>
	public static IEnumerable<Func<T>> ToFunctions<T>( this IEnumerable<T> items ) => items.Select<T, Func<T>>( i => () => i );
}