﻿using System.Runtime.ExceptionServices;
using System.Threading;
using JetBrains.Annotations;

namespace Tewl.Tools;

/// <summary>
/// Static methods pertaining to exception handling.
/// </summary>
[ PublicAPI ]
public static class ExceptionHandlingTools {
	/// <summary>
	/// Retries the given action until it executes without exception or maxAttempts is reached. You can specify different
	/// maxAttempts or retry intervals - the default is 30 tries with a 2-second wait in between each try.
	/// If every attempt fails, a new application exception will be thrown with the given message. The original exception will
	/// be the inner exception.
	/// </summary>
	public static void Retry( Action action, string failureMessage, int maxAttempts = 30, int retryIntervalMs = 2000 ) {
		for( var i = 0;; i += 1 )
			try {
				action();
				break;
			}
			catch( Exception e ) {
				if( i < maxAttempts )
					Thread.Sleep( retryIntervalMs );
				else
					throw new ApplicationException( failureMessage, e );
			}
	}

	/// <summary>
	/// Sequentially calls each of the specified methods, continuing even if exceptions are thrown. When finished, throws the first exception if there was one.
	/// </summary>
	public static void CallEveryMethod( params Action[] methods ) {
		ExceptionDispatchInfo? exception = null;
		foreach( var method in methods )
			try {
				method();
			}
			catch( Exception e ) {
				exception ??= ExceptionDispatchInfo.Capture( e );
			}
		exception?.Throw();
	}
}