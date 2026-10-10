
// Copyright (C) 2017 Luca Piccioni
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Text.RegularExpressions;
using System.Xml;
using BindingsGen.GLSpecs;

// ReSharper disable InheritdocConsiderUsage

namespace BindingsGen
{
	/// <summary>
	/// OpenGL registry documentation handler.
	/// </summary>
	public abstract class RegistryDocumentationHandler : IDisposable
	{
		#region Metadata

		/// <summary>
		/// API which this documentation belongs to.
		/// </summary>
		public string Api;

		#endregion

		#region Abstract Interface

		/// <summary>
		/// The list of commands documented by this instance.
		/// </summary>
		public abstract ICollection<string> Commands { get; }

		/// <summary>
		/// The list of enumerations documented by this instance.
		/// </summary>
		public abstract ICollection<string> Enums { get; }

		/// <summary>
		/// Loads XML documentation for processing.
		/// </summary>
		public void Load()
		{
			if (XmlPath != null)
				Load(XmlPath);
		}

		/// <summary>
		/// 
		/// </summary>
		protected string XmlPath;

		/// <summary>
		/// Loads XML documentation for processing.
		/// </summary>
		/// <param name="xmlPath">
		/// A <see cref="string"/> taht specifies the path to the XML documentation file.
		/// </param>
		public abstract void Load(string xmlPath);

		/// <summary>
		/// Query the documentation.
		/// </summary>
		public abstract void Query();

		/// <summary>
		/// Get the API enumeration summary.
		/// </summary>
		/// <param name="ctx">
		/// The <see cref="RegistryContext"/> holding information about the generated class.
		/// </param>
		/// <param name="enumerant">
		/// The relative <see cref="Enumerant"/>.
		/// </param>
		/// <returns>
		/// It returns the summary relative to <paramref name="enumerant"/>.
		/// </returns>
		public abstract string QueryEnumSummary(RegistryContext ctx, Enumerant enumerant);

		/// <summary>
		/// Get the API command summary.
		/// </summary>
		/// <param name="ctx">
		/// The <see cref="RegistryContext"/> holding information about the generated class.
		/// </param>
		/// <param name="command">
		/// The relative <see cref="Command"/>.
		/// </param>
		/// <returns>
		/// It returns the summary relative to the command <paramref name="command"/>.
		/// </returns>
		public abstract string QueryCommandSummary(RegistryContext ctx, Command command);

		/// <summary>
		/// Get the API command parameter summary.
		/// </summary>
		/// <param name="ctx">
		/// The <see cref="RegistryContext"/> holding information about the generated class.
		/// </param>
		/// <param name="command">
		/// The relative <see cref="Command"/>.
		/// </param>
		/// <param name="commandParameter">
		/// The relative <see cref="CommandParameter"/>
		/// </param>
		/// <returns>
		/// It returns the summary relative to the command <paramref name="commandParameter"/>.
		/// </returns>
		public abstract string QueryCommandParamSummary(RegistryContext ctx, Command command, CommandParameter commandParameter);

		/// <summary>
		/// Get the API command remarks.
		/// </summary>
		/// <param name="ctx">
		/// The <see cref="RegistryContext"/> holding information about the generated class.
		/// </param>
		/// <param name="command">
		/// The relative <see cref="Command"/>.
		/// </param>
		/// <returns>
		/// It returns a <see cref="IEnumerable{string}"/> that specifies the command remarks paragraphs.
		/// </returns>
		public abstract IEnumerable<string> QueryCommandRemarks(RegistryContext ctx, Command command);

		/// <summary>
		/// Get the API command associated "gets".
		/// </summary>
		/// <param name="ctx">
		/// The <see cref="RegistryContext"/> holding information about the generated class.
		/// </param>
		/// <param name="command">
		/// The relative <see cref="Command"/>.
		/// </param>
		/// <returns>
		/// It returns a <see cref="IEnumerable{string}"/> that specifies the command "get" paragraphs.
		/// </returns>
		public abstract IEnumerable<string> QueryCommandGets(RegistryContext ctx, Command command);

		/// <summary>
		/// Get the API command associated errors.
		/// </summary>
		/// <param name="ctx">
		/// The <see cref="RegistryContext"/> holding information about the generated class.
		/// </param>
		/// <param name="command">
		/// The relative <see cref="Command"/>.
		/// </param>
		/// <returns>
		/// It returns a <see cref="IEnumerable{string}"/> that specifies the command "get" paragraphs.
		/// </returns>
		public abstract IEnumerable<string> QueryCommandErrors(RegistryContext ctx, Command command);

		/// <summary>
		/// Get the API command "see also" references.
		/// </summary>
		/// <param name="ctx">
		/// The <see cref="RegistryContext"/> holding information about the generated class.
		/// </param>
		/// <param name="command">
		/// The relative <see cref="Command"/>.
		/// </param>
		/// <returns>
		/// It returns a <see cref="IEnumerable{string}"/> that specifies the command "see also" references.
		/// </returns>
		public abstract IEnumerable<string> QueryCommandSeeAlso(RegistryContext ctx, Command command);

		#endregion

		#region XML Utilities

		/// <summary>
		/// Code documentation trimming.
		/// </summary>
		/// <param name="documentation">
		/// A <see cref="string"/> that specifies the documentation to be trimmed.
		/// </param>
		/// <returns>
		/// It returns <paramref name="documentation"/>, after having removed all new lines characters and reduced contiguous
		/// white spaces.
		/// </returns>
		protected static string TrimXmlDocumentation(string documentation)
		{
			// Remove carriage returns
			documentation = Regex.Replace(documentation, @"(\r|\n)", string.Empty);
			// Trim white spaces
			documentation = Regex.Replace(documentation, @"(\t| )", " ");
			documentation = documentation.Trim();
			// Replace multi white spaces with a single one
			documentation = Regex.Replace(documentation, " +", " ");

			return documentation;
		}

		#endregion

		#region IDisposable Implementation

		/// <summary>
		/// Dispose resources associated with this instance.
		/// </summary>
		public abstract void Dispose();

		#endregion
	}
}
