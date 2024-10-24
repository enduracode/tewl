﻿using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Polly;
using StackExchange.Profiling;
using Tewl.IO;

namespace Tewl.Tools;

/// <summary>
/// Static methods pertaining to HttpClient.
/// </summary>
[ PublicAPI ]
public static class HttpClientTools {
	private class WriterContent( Action<Stream> bodyWriter ): HttpContent {
		protected override Task SerializeToStreamAsync( Stream stream, TransportContext? context ) {
			bodyWriter( stream );
			return Task.CompletedTask;
		}

		protected override Task SerializeToStreamAsync( Stream stream, TransportContext? context, CancellationToken cancellationToken ) =>
			SerializeToStreamAsync( stream, context );

		protected override void SerializeToStream( Stream stream, TransportContext? context, CancellationToken cancellationToken ) {
			bodyWriter( stream );
		}

		protected override bool TryComputeLength( out long length ) {
			length = 0;
			return false;
		}
	}

	/// <summary>
	/// Makes a GET request for a text-based resource and returns its representation, retrying several times with exponential back-off in the event of network
	/// problems or transient failures on the server. Use only from a background process that can tolerate a long delay.
	/// </summary>
	public static string? GetTextWithRetry( this HttpClient client, string url, bool returnNullIfNotFound = false, string additionalHandledMessage = "" ) =>
		ExecuteRequestWithRetry(
			true,
			async () => {
				using var response = await client.GetAsync( url, HttpCompletionOption.ResponseHeadersRead );
				if( returnNullIfNotFound && response.StatusCode == HttpStatusCode.NotFound )
					return null;
				response.EnsureSuccessStatusCode();
				return await response.Content.ReadAsStringAsync();
			},
			additionalHandledMessage: additionalHandledMessage );

	/// <summary>
	/// Makes a GET request for a resource and writes its representation to a file at the specified path, retrying several times with exponential back-off in the
	/// event of network problems or transient failures on the server. Use only from a background process that can tolerate a long delay. Overwrites the
	/// destination file if it already exists.
	/// </summary>
	public static void DownloadFileWithRetry( this HttpClient client, string url, string destinationPath, string additionalHandledMessage = "" ) =>
		ExecuteRequestWithRetry(
			true,
			async () => {
				using var response = await client.GetAsync( url, HttpCompletionOption.ResponseHeadersRead );
				response.EnsureSuccessStatusCode();

				IoMethods.DeleteFile( destinationPath );
				await using var fileStream = IoMethods.GetFileStreamForWrite( destinationPath );
				await response.Content.CopyToAsync( fileStream );
			},
			additionalHandledMessage: additionalHandledMessage );

	/// <summary>
	/// Executes a method that makes a request using <see cref="HttpClient"/>, retrying several times with exponential back-off in the event of network problems
	/// or transient failures on the server. Use only from a background process that can tolerate a long delay.
	/// </summary>
	public static void ExecuteRequestWithRetry(
		bool requestIsIdempotent, Func<Task> method, string additionalHandledMessage = "", Action? persistentFailureHandler = null ) {
		var policyBuilder = Policy.HandleInner<HttpRequestException>(
			e => e.InnerException is SocketException { SocketErrorCode: SocketError.HostNotFound or SocketError.NoData } );

		if( requestIsIdempotent ) {
			policyBuilder.OrInner<TaskCanceledException>() // timeout
				.OrInner<HttpRequestException>( e => e.InnerException is SocketException { SocketErrorCode: SocketError.ConnectionRefused } )
				.OrInner<HttpRequestException>( e => e.StatusCode is HttpStatusCode.InternalServerError )
				.OrInner<HttpRequestException>( e => e.StatusCode is HttpStatusCode.BadGateway );

			if( additionalHandledMessage.Length > 0 )
				policyBuilder = policyBuilder.OrInner<HttpRequestException>( e => e.Message.Contains( additionalHandledMessage ) );
		}

		var result = MiniProfiler.Current.Inline(
			() => policyBuilder.WaitAndRetry( 7, attemptNumber => TimeSpan.FromSeconds( Math.Pow( 2, attemptNumber ) ) )
				.ExecuteAndCapture(
					() => Policy.HandleInner<HttpRequestException>( e => e.StatusCode is HttpStatusCode.ServiceUnavailable )
						.WaitAndRetry( 11, attemptNumber => TimeSpan.FromSeconds( Math.Pow( 2, attemptNumber ) ) )
						.Execute( () => Task.Run( method ).Wait() ) ),
			"TEWL - Execute HTTP request with retry" );

		if( result.Outcome == OutcomeType.Successful )
			return;

		if( persistentFailureHandler is not null && result.ExceptionType == ExceptionType.HandledByThisPolicy )
			persistentFailureHandler();
		else
			throw result.FinalException;
	}

	/// <summary>
	/// Executes a method that makes a request using <see cref="HttpClient"/>, retrying several times with exponential back-off in the event of network problems
	/// or transient failures on the server. Use only from a background process that can tolerate a long delay.
	/// </summary>
	public static T ExecuteRequestWithRetry<T>(
		bool requestIsIdempotent, Func<Task<T>> method, string additionalHandledMessage = "", Action? persistentFailureHandler = null ) {
		T? result = default;
		ExecuteRequestWithRetry(
			requestIsIdempotent,
			async () => { result = await method(); },
			additionalHandledMessage: additionalHandledMessage,
			persistentFailureHandler: persistentFailureHandler );
		return result!;
	}

	public static HttpContent GetRequestContentFromWriter( Action<Stream> bodyWriter ) => new WriterContent( bodyWriter );
}