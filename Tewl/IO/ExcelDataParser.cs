﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using JetBrains.Annotations;
using Tewl.InputValidation;

namespace Tewl.IO {

	/// <summary>
	/// Parses Excel files.
	/// </summary>
	[ PublicAPI ]
	public class ExcelDataParser {
		private readonly Stream fileStream;

		/// <summary>
		/// The number of rows in the file, not including the header rows that were skipped with headerRowsToSkip or hasHeaderRows
		/// = true.
		/// This is the number of rows in the file that were parsed.
		/// This properly only has meaning after ParseAndProcessAllLines has been called.
		/// </summary>
		public int NonHeaderRows { get; private set; }

		/// <summary>
		/// The number of rows in the file with at least one non-blank field.
		/// This properly only has meaning after ParseAndProcessAllLines has been called.
		/// This is the number of rows in that file that were processed (the lineHandler callback was performed).
		/// </summary>
		public int RowsContainingData { get; private set; }

		/// <summary>
		/// The number of rows in the file that were processed without encountering any validation errors.
		/// This properly only has meaning after ParseAndProcessAllLines has been called.
		/// </summary>
		public int RowsWithoutValidationErrors { get; private set; }

		/// <summary>
		/// The number of rows in the file that did encounter validation errors when processed.
		/// This properly only has meaning after ParseAndProcessAllLines has been called.
		/// </summary>
		public int RowsWithValidationErrors => RowsContainingData - RowsWithoutValidationErrors;

		/// <summary>
		/// Creates a <see cref="ExcelDataParser"/>.
		/// </summary>
		public ExcelDataParser( Stream fileStream ) => this.fileStream = fileStream;

		// GMS NOTE: Not sure if we should take stream in constructor or in this method. Not sure stream openness requirements we should have, or who should clean it up.
		// Change comments to reflect parse and process all lines vs process all lines. 

		/// <summary>
		/// Specify a worksheetName or leave as the default value of null to process the first worksheet in the workbook.
		/// GMS NOTE: Get comment from other parser. Move LineProcessingMethod if this works.
		/// </summary>
		public void ProcessAllLines( TabularDataParser.LineProcessingMethod lineHandler, ICollection<ValidationError> validationErrors, string worksheetName = null ) {
			var workbook = new XLWorkbook( fileStream );
			IXLWorksheet worksheet;
			if( worksheetName != null )
				worksheet = workbook.Worksheet( worksheetName );
			else
				worksheet = workbook.Worksheets.First();

			var rows = worksheet.RangeUsed().RowsUsed().ToList();

			var headerRow = rows.First();
			var columnHeadersToIndexes = headerRow.CellsUsed().ToDictionary(
				c => c.Value.ToString().ToLower(),
				c => c.WorksheetColumn().ColumnNumber() - 1 /*Column numbers are 1-based, indexes are 0-based.*/ );

			foreach( var row in rows.Skip( 1 ) ) {
				var parsedLine = new ParsedLine( row.CellsUsed().Select( c => c.Value.ToString() ).ToList() );
				parsedLine.ColumnHeadersToIndexes = columnHeadersToIndexes;
				parsedLine.LineNumber = row.RowNumber();

				if( parsedLine.ContainsData ) {
					RowsContainingData++;
					var validator = new Validator();
					lineHandler( validator, parsedLine );
					if( validator.ErrorsOccurred ) {
						if( validationErrors != null ) {
							foreach( var error in validator.Errors )
								validationErrors.Add( new ValidationError( "Line " + parsedLine.LineNumber, error.UnusableValueReturned, error.Message ) );
						}
					}
					else
						RowsWithoutValidationErrors++;
				}
			}
		}
	}
}